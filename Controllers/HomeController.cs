using System.Diagnostics;
using HRPackage.Models;
using HRPackage.Models.ViewModels;
using HRPackage.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRPackage.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IPurchaseOrdersRepository _purchaseOrdersRepository;
        private readonly IInvoicesRepository _invoicesRepository;

        public HomeController(IPurchaseOrdersRepository purchaseOrdersRepository, IInvoicesRepository invoicesRepository)
        {
            _purchaseOrdersRepository = purchaseOrdersRepository;
            _invoicesRepository = invoicesRepository;
        }

        public async Task<IActionResult> Index()
        {
            var stats = await _purchaseOrdersRepository.GetDashboardStatsAsync();
            var unpaidInvoices = await _invoicesRepository.GetUnpaidCountAsync();
            var recentPOs = await _purchaseOrdersRepository.GetRecentAsync(5);

            var model = new DashboardViewModel
            {
                TotalPOs = stats.TotalPOs,
                CompletedPOs = stats.CompletedPOs,
                TotalPoAmount = stats.TotalAmount,
                UnpaidInvoices = unpaidInvoices,
                RecentPOs = recentPOs.ToList()
            };

            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
