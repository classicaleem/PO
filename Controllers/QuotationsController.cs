using System.Security.Claims;
using HRPackage.Models;
using HRPackage.Models.ViewModels;
using HRPackage.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRPackage.Controllers
{
    [Authorize]
    public class QuotationsController : Controller
    {
        private readonly IQuotationsRepository _quotationsRepository;
        private readonly ICustomersRepository _customersRepository;

        public QuotationsController(IQuotationsRepository quotationsRepository, ICustomersRepository customersRepository)
        {
            _quotationsRepository = quotationsRepository;
            _customersRepository = customersRepository;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _quotationsRepository.GetAllAsync();
            return View(list);
        }

        public async Task<IActionResult> Create()
        {
            var model = new QuotationViewModel
            {
                QuotationNo = await _quotationsRepository.GenerateNextQuotationNoAsync(),
                Date = DateTime.Today,
                Customers = await GetCustomerListAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QuotationViewModel model)
        {
            if (model.Items == null || !model.Items.Any(i => !string.IsNullOrWhiteSpace(i.Description)))
            {
                ModelState.AddModelError("", "At least one item required.");
            }

            if (!ModelState.IsValid)
            {
                model.Customers = await GetCustomerListAsync();
                return View(model);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var q = new Quotation
            {
                QuotationNo = model.QuotationNo,
                Date = model.Date,
                ValidUntil = model.ValidUntil,
                CustomerId = model.CustomerId,
                CreatedByUserId = userId
            };

            var items = model.Items
                .Where(i => !string.IsNullOrWhiteSpace(i.Description))
                .Select(i => new QuotationItem
                {
                    Description = i.Description,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList();

            await _quotationsRepository.CreateAsync(q, items);
            TempData["Success"] = "Quotation created.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DownloadPdf(int id)
        {
            var q = await _quotationsRepository.GetByIdAsync(id);
            if (q == null) return NotFound();

            return new Rotativa.AspNetCore.ViewAsPdf("Print", q)
            {
                FileName = $"Quotation_{q.QuotationNo}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageMargins = new Rotativa.AspNetCore.Options.Margins(10, 10, 10, 10)
            };
        }

        public async Task<IActionResult> Print(int id)
        {
            var q = await _quotationsRepository.GetByIdAsync(id);
            if (q == null) return NotFound();
            return View(q);
        }

        public async Task<IActionResult> Delete(int id)
        {
            await _quotationsRepository.SoftDeleteAsync(id);
            TempData["Success"] = "Quotation deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<SelectListItem>> GetCustomerListAsync()
        {
            var customers = await _customersRepository.GetAllAsync();
            return customers.Select(c => new SelectListItem
            {
                Value = c.CustomerId.ToString(),
                Text = c.CustomerName
            }).ToList();
        }
    }
}
