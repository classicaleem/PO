using System.Security.Claims;
using HRPackage.Models;
using HRPackage.Models.ViewModels;
using HRPackage.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRPackage.Controllers
{
    [Authorize]
    public class PurchaseOrdersController : Controller
    {
        private readonly IPurchaseOrdersRepository _purchaseOrdersRepository;
        private readonly ICustomersRepository _customersRepository;

        public PurchaseOrdersController(
            IPurchaseOrdersRepository purchaseOrdersRepository,
            ICustomersRepository customersRepository)
        {
            _purchaseOrdersRepository = purchaseOrdersRepository;
            _customersRepository = customersRepository;
        }

        public async Task<IActionResult> Index()
        {
            var purchaseOrders = await _purchaseOrdersRepository.GetAllAsync();
            return View(purchaseOrders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var purchaseOrder = await _purchaseOrdersRepository.GetByIdWithInvoicesAsync(id);
            if (purchaseOrder == null)
            {
                return NotFound();
            }

            // Load customer if exists
            if (purchaseOrder.CustomerId.HasValue)
            {
                purchaseOrder.Customer = await _customersRepository.GetByIdAsync(purchaseOrder.CustomerId.Value);
            }

            return View(purchaseOrder);
        }

        public async Task<IActionResult> Create()
        {
            var model = new PurchaseOrderViewModel
            {
                InternalPoCode = await _purchaseOrdersRepository.GenerateNextInternalPoCodeAsync(),
                PoDate = DateTime.Today,
                Customers = await _customersRepository.GetDropdownListAsync(),
                Items = new List<PurchaseOrderItemViewModel>
                {
                    new PurchaseOrderItemViewModel { LineNumber = 1 }
                }
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseOrderViewModel model)
        {
            // Remove validation for empty item rows
            model.Items = model.Items?.Where(i => !string.IsNullOrWhiteSpace(i.ItemDescription)).ToList() 
                          ?? new List<PurchaseOrderItemViewModel>();

            if (model.Items.Count == 0)
            {
                ModelState.AddModelError("", "At least one item is required.");
            }

            if (model.Items.Count > 10)
            {
                ModelState.AddModelError("", "Maximum 10 items are allowed.");
            }

            if (!ModelState.IsValid)
            {
                model.Customers = await _customersRepository.GetDropdownListAsync();
                model.InternalPoCode = await _purchaseOrdersRepository.GenerateNextInternalPoCodeAsync();
                if (model.Items.Count == 0)
                {
                    model.Items.Add(new PurchaseOrderItemViewModel { LineNumber = 1 });
                }
                return View(model);
            }

            if (await _purchaseOrdersRepository.PoNumberExistsAsync(model.PoNumber))
            {
                ModelState.AddModelError("PoNumber", "This PO Number already exists.");
                model.Customers = await _customersRepository.GetDropdownListAsync();
                return View(model);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            // Get customer name for SupplierName field (backward compat)
            var customer = await _customersRepository.GetByIdAsync(model.CustomerId);

            var purchaseOrder = new PurchaseOrder
            {
                PoNumber = model.PoNumber,
                InternalPoCode = await _purchaseOrdersRepository.GenerateNextInternalPoCodeAsync(),
                CustomerId = model.CustomerId,
                SupplierName = customer?.CustomerName ?? "",
                PoAmount = model.Items.Sum(i => i.Quantity * i.UnitPrice),
                CgstPercent = model.CgstPercent,
                SgstPercent = model.SgstPercent,
                IgstPercent = model.IgstPercent,
                // Recalculate tax amounts on server side for safety
                TaxAmount = model.Items.Sum(i => i.Quantity * i.UnitPrice) * (model.CgstPercent + model.SgstPercent + model.IgstPercent) / 100,
                GrandTotal = model.Items.Sum(i => i.Quantity * i.UnitPrice) * (1 + (model.CgstPercent + model.SgstPercent + model.IgstPercent) / 100),
                PoDate = model.PoDate,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                CreatedByUserId = userId,
                IsCompleted = false
            };

            var items = model.Items.Select(i => new PurchaseOrderItem
            {
                ItemDescription = i.ItemDescription,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.Quantity * i.UnitPrice
            }).ToList();

            await _purchaseOrdersRepository.CreateWithItemsAsync(purchaseOrder, items);
            TempData["Success"] = "Purchase Order created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var purchaseOrder = await _purchaseOrdersRepository.GetByIdWithItemsAsync(id);
            if (purchaseOrder == null)
            {
                return NotFound();
            }

            var customer = purchaseOrder.CustomerId.HasValue 
                ? await _customersRepository.GetByIdAsync(purchaseOrder.CustomerId.Value) 
                : null;

            var model = new PurchaseOrderViewModel
            {
                PoId = purchaseOrder.PoId,
                InternalPoCode = purchaseOrder.InternalPoCode,
                PoNumber = purchaseOrder.PoNumber,
                CustomerId = purchaseOrder.CustomerId ?? 0,
                SupplierName = purchaseOrder.SupplierName,
                PoAmount = purchaseOrder.PoAmount,
                CgstPercent = purchaseOrder.CgstPercent,
                SgstPercent = purchaseOrder.SgstPercent,
                IgstPercent = purchaseOrder.IgstPercent,
                TaxAmount = purchaseOrder.TaxAmount,
                GrandTotal = purchaseOrder.GrandTotal,
                PoDate = purchaseOrder.PoDate,
                StartDate = purchaseOrder.StartDate,
                EndDate = purchaseOrder.EndDate,
                IsCompleted = purchaseOrder.IsCompleted,
                Customers = await _customersRepository.GetDropdownListAsync(),
                CustomerName = customer?.CustomerName,
                CustomerAddress = customer?.FullAddress,
                CustomerGstNumber = customer?.GstNumber,
                Items = purchaseOrder.Items.Select(i => new PurchaseOrderItemViewModel
                {
                    PoItemId = i.PoItemId,
                    PoId = i.PoId,
                    LineNumber = i.LineNumber,
                    ItemDescription = i.ItemDescription,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.LineTotal
                }).ToList()
            };

            if (model.Items.Count == 0)
            {
                model.Items.Add(new PurchaseOrderItemViewModel { LineNumber = 1 });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PurchaseOrderViewModel model)
        {
            if (id != model.PoId)
            {
                return NotFound();
            }

            // Remove validation for empty item rows
            model.Items = model.Items?.Where(i => !string.IsNullOrWhiteSpace(i.ItemDescription)).ToList() 
                          ?? new List<PurchaseOrderItemViewModel>();

            if (model.Items.Count == 0)
            {
                ModelState.AddModelError("", "At least one item is required.");
            }

            if (model.Items.Count > 10)
            {
                ModelState.AddModelError("", "Maximum 10 items are allowed.");
            }

            if (!ModelState.IsValid)
            {
                model.Customers = await _customersRepository.GetDropdownListAsync();
                if (model.Items.Count == 0)
                {
                    model.Items.Add(new PurchaseOrderItemViewModel { LineNumber = 1 });
                }
                return View(model);
            }

            if (await _purchaseOrdersRepository.PoNumberExistsAsync(model.PoNumber, model.PoId))
            {
                ModelState.AddModelError("PoNumber", "This PO Number already exists.");
                model.Customers = await _customersRepository.GetDropdownListAsync();
                return View(model);
            }

            var customer = await _customersRepository.GetByIdAsync(model.CustomerId);

            var purchaseOrder = new PurchaseOrder
            {
                PoId = model.PoId,
                PoNumber = model.PoNumber,
                CustomerId = model.CustomerId,
                SupplierName = customer?.CustomerName ?? "",
                PoAmount = model.Items.Sum(i => i.Quantity * i.UnitPrice),
                CgstPercent = model.CgstPercent,
                SgstPercent = model.SgstPercent,
                IgstPercent = model.IgstPercent,
                // Recalculate tax amounts on server side for safety
                TaxAmount = model.Items.Sum(i => i.Quantity * i.UnitPrice) * (model.CgstPercent + model.SgstPercent + model.IgstPercent) / 100,
                GrandTotal = model.Items.Sum(i => i.Quantity * i.UnitPrice) * (1 + (model.CgstPercent + model.SgstPercent + model.IgstPercent) / 100),
                PoDate = model.PoDate,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                IsCompleted = model.IsCompleted
            };

            var items = model.Items.Select(i => new PurchaseOrderItem
            {
                ItemDescription = i.ItemDescription,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.Quantity * i.UnitPrice
            }).ToList();

            await _purchaseOrdersRepository.UpdateWithItemsAsync(purchaseOrder, items);
            TempData["Success"] = "Purchase Order updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var purchaseOrder = await _purchaseOrdersRepository.GetByIdWithItemsAsync(id);
            if (purchaseOrder == null)
            {
                return NotFound();
            }
            return View(purchaseOrder);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _purchaseOrdersRepository.SoftDeleteAsync(id);
            TempData["Success"] = "Purchase Order deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DownloadPdf(int id)
        {
            var purchaseOrder = await _purchaseOrdersRepository.GetByIdWithItemsAsync(id);
            if (purchaseOrder == null)
            {
                return NotFound();
            }

            if (purchaseOrder.CustomerId.HasValue)
            {
                purchaseOrder.Customer = await _customersRepository.GetByIdAsync(purchaseOrder.CustomerId.Value);
            }

            return new Rotativa.AspNetCore.ViewAsPdf("PrintPo", purchaseOrder)
            {
                FileName = $"PO_{purchaseOrder.PoNumber}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageMargins = new Rotativa.AspNetCore.Options.Margins(10, 10, 10, 10)
            };
        }

        // Print view for PDF
        public async Task<IActionResult> PrintPo(int id)
        {
            var purchaseOrder = await _purchaseOrdersRepository.GetByIdWithItemsAsync(id);
            if (purchaseOrder == null)
            {
                return NotFound();
            }

            if (purchaseOrder.CustomerId.HasValue)
            {
                purchaseOrder.Customer = await _customersRepository.GetByIdAsync(purchaseOrder.CustomerId.Value);
            }

            return View(purchaseOrder);
        }
    }
}
