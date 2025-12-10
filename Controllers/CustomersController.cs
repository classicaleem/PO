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
    public class CustomersController : Controller
    {
        private readonly ICustomersRepository _customersRepository;

        public CustomersController(ICustomersRepository customersRepository)
        {
            _customersRepository = customersRepository;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var customers = await _customersRepository.GetAllAsync();
            return View(customers);
        }

        public async Task<IActionResult> Details(int id)
        {
            var customer = await _customersRepository.GetByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            var model = new CustomerViewModel
            {
                IsActive = true,
                States = await GetStateSelectListAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CustomerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.States = await GetStateSelectListAsync();
                return View(model);
            }

            if (await _customersRepository.CustomerCodeExistsAsync(model.CustomerCode))
            {
                ModelState.AddModelError("CustomerCode", "This Customer Code already exists.");
                model.States = await GetStateSelectListAsync();
                return View(model);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var customer = new Customer
            {
                CustomerCode = model.CustomerCode,
                CustomerName = model.CustomerName,
                AddressLine1 = model.AddressLine1,
                AddressLine2 = model.AddressLine2,
                City = model.City,
                State = model.State,
                StateCode = model.StateCode,
                Pincode = model.Pincode,
                ContactNumber = model.ContactNumber,
                EmailId = model.EmailId,
                GstNumber = model.GstNumber,
                DefaultCgstPercent = model.DefaultCgstPercent,
                DefaultSgstPercent = model.DefaultSgstPercent,
                DefaultIgstPercent = model.DefaultIgstPercent,
                IsActive = model.IsActive,
                CreatedByUserId = userId
            };

            await _customersRepository.CreateAsync(customer);
            TempData["Success"] = "Customer created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _customersRepository.GetByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            var model = new CustomerViewModel
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                AddressLine1 = customer.AddressLine1,
                AddressLine2 = customer.AddressLine2,
                City = customer.City,
                State = customer.State,
                StateCode = customer.StateCode,
                Pincode = customer.Pincode,
                ContactNumber = customer.ContactNumber,
                EmailId = customer.EmailId,
                GstNumber = customer.GstNumber,
                DefaultCgstPercent = customer.DefaultCgstPercent,
                DefaultSgstPercent = customer.DefaultSgstPercent,
                DefaultIgstPercent = customer.DefaultIgstPercent,
                IsActive = customer.IsActive,
                States = await GetStateSelectListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, CustomerViewModel model)
        {
            if (id != model.CustomerId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                model.States = await GetStateSelectListAsync();
                return View(model);
            }

            if (await _customersRepository.CustomerCodeExistsAsync(model.CustomerCode, model.CustomerId))
            {
                ModelState.AddModelError("CustomerCode", "This Customer Code already exists.");
                model.States = await GetStateSelectListAsync();
                return View(model);
            }

            var customer = new Customer
            {
                CustomerId = model.CustomerId,
                CustomerCode = model.CustomerCode,
                CustomerName = model.CustomerName,
                AddressLine1 = model.AddressLine1,
                AddressLine2 = model.AddressLine2,
                City = model.City,
                State = model.State,
                StateCode = model.StateCode,
                Pincode = model.Pincode,
                ContactNumber = model.ContactNumber,
                EmailId = model.EmailId,
                GstNumber = model.GstNumber,
                DefaultCgstPercent = model.DefaultCgstPercent,
                DefaultSgstPercent = model.DefaultSgstPercent,
                DefaultIgstPercent = model.DefaultIgstPercent,
                IsActive = model.IsActive
            };

            await _customersRepository.UpdateAsync(customer);
            TempData["Success"] = "Customer updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _customersRepository.GetByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _customersRepository.SoftDeleteAsync(id);
            TempData["Success"] = "Customer deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // API endpoint for getting tax defaults based on state
        [HttpGet]
        public IActionResult GetTaxDefaults(string state)
        {
            decimal cgst = 0, sgst = 0, igst = 0;
            
            if (state?.Trim().Equals("Tamil Nadu", StringComparison.OrdinalIgnoreCase) == true)
            {
                cgst = 9;
                sgst = 9;
                igst = 0;
            }
            else if (!string.IsNullOrEmpty(state))
            {
                cgst = 0;
                sgst = 0;
                igst = 18;
            }

            return Json(new { cgst, sgst, igst });
        }

        // API endpoint for getting customer details
        [HttpGet]
        public async Task<IActionResult> GetCustomerDetails(int id)
        {
            var customer = await _customersRepository.GetByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            return Json(new { 
                customerName = customer.CustomerName,
                address = customer.FullAddress,
                gstNumber = customer.GstNumber,
                state = customer.State, // Adding State for Tax Calculation
                cgst = customer.DefaultCgstPercent,
                sgst = customer.DefaultSgstPercent,
                igst = customer.DefaultIgstPercent
            });
        }

        private async Task<List<SelectListItem>> GetStateSelectListAsync()
        {
            var states = await _customersRepository.GetAllStatesAsync();
            return states.Select(s => new SelectListItem
            {
                Value = s.StateName,
                Text = $"{s.StateName} ({s.StateCode})"
            }).ToList();
        }
    }
}
