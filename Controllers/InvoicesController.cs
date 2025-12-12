using System.Security.Claims;
using HRPackage.Models;
using HRPackage.Models.ViewModels;
using HRPackage.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRPackage.Controllers
{
    [Authorize]
    public class InvoicesController : Controller
    {
        private readonly IInvoicesRepository _invoicesRepository;
        private readonly IPurchaseOrdersRepository _purchaseOrdersRepository;
        private readonly ICustomersRepository _customersRepository;
        private readonly HRPackage.Services.IPdfService _pdfService;
        private readonly Microsoft.Extensions.Options.IOptions<CompanySettings> _companySettings;

        public InvoicesController(
            IInvoicesRepository invoicesRepository,
            IPurchaseOrdersRepository purchaseOrdersRepository,
            ICustomersRepository customersRepository,
            HRPackage.Services.IPdfService pdfService,
            Microsoft.Extensions.Options.IOptions<CompanySettings> companySettings)
        {
            _invoicesRepository = invoicesRepository;
            _purchaseOrdersRepository = purchaseOrdersRepository;
            _customersRepository = customersRepository;
            _pdfService = pdfService;
            _companySettings = companySettings;
        }

        public async Task<IActionResult> Index()
        {
            var invoices = await _invoicesRepository.GetAllAsync();
            return View(invoices);
        }

        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _invoicesRepository.GetByIdWithItemsAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            // Load customer if exists
            if (invoice.CustomerId.HasValue)
            {
                invoice.Customer = await _customersRepository.GetByIdAsync(invoice.CustomerId.Value);
            }

            return View(invoice);
        }

        public async Task<IActionResult> Create(int? poId = null)
        {
            var model = new InvoiceViewModel
            {
                InvoiceNumber = await _invoicesRepository.GenerateNextInvoiceNumberAsync(),
                InvoiceDate = DateTime.Today,
                PurchaseOrders = await _purchaseOrdersRepository.GetDropdownListAsync()
            };

            if (poId.HasValue)
            {
                model.PoId = poId.Value;
                await LoadPoDetailsAsync(model, poId.Value);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InvoiceViewModel model)
        {
            // Filter items with quantity > 0
            model.Items = model.Items?.Where(i => i.ThisInvoiceQuantity > 0).ToList() 
                          ?? new List<InvoiceItemViewModel>();

            if (model.Items.Count == 0)
            {
                ModelState.AddModelError("", "At least one item with quantity must be specified.");
            }

            // Validate quantities don't exceed pending
            foreach (var item in model.Items)
            {
                if (item.ThisInvoiceQuantity > item.PendingQuantity)
                {
                    ModelState.AddModelError("", $"Quantity for '{item.ItemDescription}' exceeds pending quantity.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.PurchaseOrders = await _purchaseOrdersRepository.GetDropdownListAsync();
                if (model.PoId > 0)
                {
                    await LoadPoDetailsAsync(model, model.PoId);
                }
                return View(model);
            }

            if (await _invoicesRepository.InvoiceNumberExistsAsync(model.InvoiceNumber))
            {
                ModelState.AddModelError("InvoiceNumber", "This Invoice Number already exists.");
                model.PurchaseOrders = await _purchaseOrdersRepository.GetDropdownListAsync();
                return View(model);
            }

            var invoice = new Invoice
            {
                PoId = model.PoId,
                InvoiceNumber = model.InvoiceNumber,
                InvoiceDate = model.InvoiceDate,
                ShippingAddress = model.UseDifferentShippingAddress ? model.ShippingAddress : null,
                IsPaid = model.IsPaid,
                TotalAmount = model.Items.Sum(i => i.ThisInvoiceQuantity * i.UnitPrice)
            };

            // Calculate taxes based on PO rates
            var po = await _purchaseOrdersRepository.GetByIdAsync(model.PoId);
            if (po != null)
            {
                invoice.CgstPercent = po.CgstPercent;
                invoice.SgstPercent = po.SgstPercent;
                invoice.IgstPercent = po.IgstPercent;
                
                // Calculate tax amount on the INVOICED Total
                invoice.TaxAmount = invoice.TotalAmount * (po.CgstPercent + po.SgstPercent + po.IgstPercent) / 100;
                invoice.GrandTotal = invoice.TotalAmount + invoice.TaxAmount;
            }

            var items = model.Items.Select(i => new InvoiceItem
            {
                PoItemId = i.PoItemId,
                HsnCode = i.HsnCode,
                Quantity = i.ThisInvoiceQuantity,
                UnitPrice = i.UnitPrice,
                LineAmount = i.ThisInvoiceQuantity * i.UnitPrice
            }).ToList();

            await _invoicesRepository.CreateWithItemsAsync(invoice, items);
            
            // Update PO completion status
            await _purchaseOrdersRepository.UpdateCompletionStatusAsync(model.PoId);

            TempData["Success"] = "Invoice created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var invoice = await _invoicesRepository.GetByIdWithItemsAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            var model = new InvoiceViewModel
            {
                InvoiceId = invoice.InvoiceId,
                PoId = invoice.PoId,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                TotalAmount = invoice.TotalAmount,
                IsPaid = invoice.IsPaid,
                UseDifferentShippingAddress = !string.IsNullOrEmpty(invoice.ShippingAddress),
                ShippingAddress = invoice.ShippingAddress,
                InternalPoCode = invoice.InternalPoCode,
                CustomerName = invoice.CustomerName,
                // Add tax info for display/binding
                CgstPercent = invoice.CgstPercent,
                SgstPercent = invoice.SgstPercent,
                IgstPercent = invoice.IgstPercent,
                TaxAmount = invoice.TaxAmount,
                GrandTotal = invoice.GrandTotal,
                PurchaseOrders = await _purchaseOrdersRepository.GetDropdownListAsync(),
                // Map Items
                Items = invoice.Items?.Select(i => new InvoiceItemViewModel
                {
                    InvoiceItemId = i.InvoiceItemId,
                    PoItemId = i.PoItemId,
                    ItemDescription = i.ItemDescription,
                    ThisInvoiceQuantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineAmount = i.LineAmount,
                    HsnCode = i.HsnCode // Mapped from repository join
                }).ToList() ?? new List<InvoiceItemViewModel>()
            };
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InvoiceViewModel model)
        {
            if (id != model.InvoiceId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                model.PurchaseOrders = await _purchaseOrdersRepository.GetDropdownListAsync();
                return View(model);
            }

            if (await _invoicesRepository.InvoiceNumberExistsAsync(model.InvoiceNumber, model.InvoiceId))
            {
                ModelState.AddModelError("InvoiceNumber", "This Invoice Number already exists.");
                model.PurchaseOrders = await _purchaseOrdersRepository.GetDropdownListAsync();
                return View(model);
            }

            var invoice = new Invoice
            {
                InvoiceId = model.InvoiceId,
                PoId = model.PoId,
                InvoiceNumber = model.InvoiceNumber,
                InvoiceDate = model.InvoiceDate,
                TotalAmount = model.TotalAmount,
                ShippingAddress = model.UseDifferentShippingAddress ? model.ShippingAddress : null,
                IsPaid = model.IsPaid
            };

            // Recalculate taxes based on PO rates (to be safe or allow override if we supported it)
            var poForEdit = await _purchaseOrdersRepository.GetByIdAsync(model.PoId);
            if (poForEdit != null)
            {
                invoice.CgstPercent = poForEdit.CgstPercent;
                invoice.SgstPercent = poForEdit.SgstPercent;
                invoice.IgstPercent = poForEdit.IgstPercent;
                invoice.TaxAmount = invoice.TotalAmount * (poForEdit.CgstPercent + poForEdit.SgstPercent + poForEdit.IgstPercent) / 100;
                invoice.GrandTotal = invoice.TotalAmount + invoice.TaxAmount;
            }

            await _invoicesRepository.UpdateAsync(invoice);
            TempData["Success"] = "Invoice updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var invoice = await _invoicesRepository.GetByIdAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }
            return View(invoice);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var invoice = await _invoicesRepository.GetByIdAsync(id);
            if (invoice != null)
            {
                await _invoicesRepository.SoftDeleteAsync(id);
                // Re-check PO completion status
                await _purchaseOrdersRepository.UpdateCompletionStatusAsync(invoice.PoId);
            }
            TempData["Success"] = "Invoice deleted successfully.";
            return RedirectToAction(nameof(Index));
        }



        // API endpoint for loading PO items
        public async Task<IActionResult> DownloadPdf(int id)
        {
            var invoice = await _invoicesRepository.GetByIdAsync(id);
            if (invoice == null) return NotFound();

            if (invoice.PurchaseOrder == null && invoice.PoId != 0)
            {
                 var po = await _purchaseOrdersRepository.GetByIdAsync(invoice.PoId);
                 if (po != null) {
                    invoice.PurchaseOrder = po;
                    invoice.PoNumber = po.PoNumber;
                 }
            }

            // Ensure items are loaded
            if (invoice.Items == null || !invoice.Items.Any())
            {
                invoice.Items = await _invoicesRepository.GetInvoiceItemsAsync(id);
            }

            var pdfBytes = _pdfService.GenerateInvoicePdf(invoice, _companySettings.Value);
            return File(pdfBytes, "application/pdf", $"Invoice_{invoice.InvoiceNumber}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> GetPoItems(int poId)
        {
            var po = await _purchaseOrdersRepository.GetByIdAsync(poId);
            if (po == null)
            {
                return NotFound();
            }

            var items = await _invoicesRepository.GetPoItemsForInvoiceAsync(poId);
            
            Customer? customer = null;
            if (po.CustomerId.HasValue)
            {
                customer = await _customersRepository.GetByIdAsync(po.CustomerId.Value);
            }

            return Json(new { 
                internalPoCode = po.InternalPoCode,
                poNumber = po.PoNumber,
                customerName = customer?.CustomerName ?? po.SupplierName,
                customerAddress = customer?.FullAddress ?? "",
                customerGstNumber = customer?.GstNumber ?? "",
                // Return tax rates from PO
                cgstPercent = po.CgstPercent,
                sgstPercent = po.SgstPercent,
                igstPercent = po.IgstPercent,
                items = items.Select(i => new {
                    poItemId = i.PoItemId,
                    itemDescription = i.ItemDescription,
                    hsnCode = i.HsnCode,
                    orderedQuantity = i.OrderedQuantity,
                    previouslyInvoiced = i.PreviouslyInvoiced,
                    pendingQuantity = i.PendingQuantity,
                    unitPrice = i.UnitPrice
                })
            });
        }

        private async Task LoadPoDetailsAsync(InvoiceViewModel model, int poId)
        {
            var po = await _purchaseOrdersRepository.GetByIdAsync(poId);
            if (po != null)
            {
                model.InternalPoCode = po.InternalPoCode;
                model.PoNumber = po.PoNumber;
                
                if (po.CustomerId.HasValue)
                {
                    var customer = await _customersRepository.GetByIdAsync(po.CustomerId.Value);
                    if (customer != null)
                    {
                        model.CustomerId = customer.CustomerId;
                        model.CustomerName = customer.CustomerName;
                        model.CustomerAddress = customer.FullAddress;
                        model.CustomerGstNumber = customer.GstNumber;
                    }
                }

                model.Items = await _invoicesRepository.GetPoItemsForInvoiceAsync(poId);
            }
        }
    }
}
