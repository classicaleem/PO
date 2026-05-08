using System.Diagnostics;
using SmartPO.Models;
using SmartPO.Models.ViewModels;
using SmartPO.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartPO.Controllers
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
            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

            var poStats = await _purchaseOrdersRepository.GetDashboardStatsAsync();
            var invoiceStats = await _invoicesRepository.GetDashboardStatsAsync();
            var recentPOs = await _purchaseOrdersRepository.GetRecentAsync(5);
            var recentInvoices = await _invoicesRepository.GetRecentAsync(5);
            var monthlyPoStats = await _purchaseOrdersRepository.GetMonthlyStatsAsync(monthStart, monthEnd);
            var monthlyInvStats = await _invoicesRepository.GetMonthlyStatsAsync(monthStart, monthEnd);
            var topCustomers = await _purchaseOrdersRepository.GetTopCustomersAsync(5);

            var model = new DashboardViewModel
            {
                TotalPOs = poStats.TotalPOs,
                CompletedPOs = poStats.CompletedPOs,
                PendingPOs = poStats.TotalPOs - poStats.CompletedPOs,
                TotalPoAmount = poStats.TotalAmount,

                TotalInvoices = invoiceStats.TotalInvoices,
                PaidInvoices = invoiceStats.PaidInvoices,
                UnpaidInvoices = invoiceStats.UnpaidInvoices,
                TotalInvoiceAmount = invoiceStats.TotalAmount,
                UnpaidInvoiceAmount = invoiceStats.UnpaidAmount,

                ThisMonthPOs = monthlyPoStats.PoCount,
                ThisMonthInvoices = monthlyInvStats.InvoiceCount,
                ThisMonthPoAmount = monthlyPoStats.PoAmount,
                ThisMonthInvoiceAmount = monthlyInvStats.InvoiceAmount,

                RecentPOs = recentPOs.ToList(),
                RecentInvoices = recentInvoices.ToList(),
                TopCustomers = topCustomers
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
