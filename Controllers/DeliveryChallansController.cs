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
    public class DeliveryChallansController : Controller
    {
        private readonly IDeliveryChallansRepository _dcRepository;
        private readonly ICustomersRepository _customersRepository;
        private readonly HRPackage.Services.IPdfService _pdfService;
        private readonly Microsoft.Extensions.Options.IOptions<CompanySettings> _companySettings;

        public DeliveryChallansController(
            IDeliveryChallansRepository dcRepository, 
            ICustomersRepository customersRepository,
            HRPackage.Services.IPdfService pdfService,
            Microsoft.Extensions.Options.IOptions<CompanySettings> companySettings)
        {
            _dcRepository = dcRepository;
            _customersRepository = customersRepository;
            _pdfService = pdfService;
            _companySettings = companySettings;
        }

        public async Task<IActionResult> Index()
        {
            var dcs = await _dcRepository.GetAllAsync();
            return View(dcs);
        }

        public async Task<IActionResult> Create()
        {
            var model = new DeliveryChallanViewModel
            {
                DcNumber = await _dcRepository.GenerateNextDcNumberAsync(),
                DcDate = DateTime.Today,
                Customers = await GetCustomerListAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DeliveryChallanViewModel model)
        {
            if (model.Items == null || !model.Items.Any(i => !string.IsNullOrWhiteSpace(i.Description)))
            {
                ModelState.AddModelError("", "At least one item description is required.");
            }

            if (!ModelState.IsValid)
            {
                model.Customers = await GetCustomerListAsync();
                return View(model);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var dc = new DeliveryChallan
            {
                DcNumber = model.DcNumber,
                DcDate = model.DcDate,
                CustomerId = model.CustomerId,
                TargetCompany = model.TargetCompany,
                VehicleNo = model.VehicleNo,
                CreatedByUserId = userId
            };

            var items = model.Items
                .Where(i => !string.IsNullOrWhiteSpace(i.Description))
                .Select(i => new DeliveryChallanItem
                {
                    Description = i.Description,
                    Quantity = i.Quantity,
                    Unit = i.Unit ?? "NO",
                    Remarks = i.Remarks
                }).ToList();

            await _dcRepository.CreateAsync(dc, items);
            TempData["Success"] = "Delivery Challan created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DownloadPdf(int id)
        {
            var dc = await _dcRepository.GetByIdAsync(id);
            if (dc == null) return NotFound();

            var pdfBytes = _pdfService.GenerateDeliveryChallanPdf(dc, _companySettings.Value);
            return File(pdfBytes, "application/pdf", $"DC_{dc.DcNumber.Replace("/", "_")}.pdf");
        }


        
        public async Task<IActionResult> Delete(int id)
        {
            await _dcRepository.SoftDeleteAsync(id);
             TempData["Success"] = "DC Deleted.";
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
