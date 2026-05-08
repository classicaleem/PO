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
        private readonly ICustomersRepository _customersRepository;
        private readonly IQuotationsRepository _quotationsRepository;

        public HomeController(
            IPurchaseOrdersRepository purchaseOrdersRepository,
            IInvoicesRepository invoicesRepository,
            ICustomersRepository customersRepository,
            IQuotationsRepository quotationsRepository)
        {
            _purchaseOrdersRepository = purchaseOrdersRepository;
            _invoicesRepository = invoicesRepository;
            _customersRepository = customersRepository;
            _quotationsRepository = quotationsRepository;
        }

        public async Task<IActionResult> Index()
        {
            var statsTask = _purchaseOrdersRepository.GetDashboardStatsAsync();
            var pendingPoCountTask = _purchaseOrdersRepository.GetPendingCountAsync();
            var recentPOsTask = _purchaseOrdersRepository.GetRecentAsync(5);
            var monthlyPoTrendTask = _purchaseOrdersRepository.GetMonthlyPoTrendAsync(6);
            var pendingQtyPOsTask = _purchaseOrdersRepository.GetPendingQuantityPOsAsync(5);
            var topCustomersTask = _purchaseOrdersRepository.GetTopCustomersAsync(5);

            var unpaidCountTask = _invoicesRepository.GetUnpaidCountAsync();
            var unpaidAmountTask = _invoicesRepository.GetUnpaidAmountAsync();
            var paidAmountTask = _invoicesRepository.GetPaidAmountAsync();
            var totalInvAmountTask = _invoicesRepository.GetTotalInvoiceAmountAsync();
            var invoiceCountTask = _invoicesRepository.GetCountAsync();
            var recentInvoicesTask = _invoicesRepository.GetRecentAsync(5);
            var unpaidListTask = _invoicesRepository.GetUnpaidListAsync();
            var monthlyInvTrendTask = _invoicesRepository.GetMonthlyInvoiceTrendAsync(6);

            var activeCustomersTask = _customersRepository.GetActiveCountAsync();
            var quotationCountTask = _quotationsRepository.GetCountAsync();

            await Task.WhenAll(
                statsTask, pendingPoCountTask, recentPOsTask, monthlyPoTrendTask,
                pendingQtyPOsTask, topCustomersTask,
                unpaidCountTask, unpaidAmountTask, paidAmountTask, totalInvAmountTask,
                invoiceCountTask, recentInvoicesTask, unpaidListTask, monthlyInvTrendTask,
                activeCustomersTask, quotationCountTask
            );

            var stats = await statsTask;
            var monthlyPoTrend = await monthlyPoTrendTask;
            var monthlyInvTrend = await monthlyInvTrendTask;

            var monthlyTrend = MergeMonthlyTrends(monthlyPoTrend, monthlyInvTrend);

            var model = new DashboardViewModel
            {
                TotalPOs = stats.TotalPOs,
                CompletedPOs = stats.CompletedPOs,
                PendingPOs = await pendingPoCountTask,
                TotalPoAmount = stats.TotalAmount,

                TotalInvoices = await invoiceCountTask,
                UnpaidInvoices = await unpaidCountTask,
                UnpaidAmount = await unpaidAmountTask,
                PaidAmount = await paidAmountTask,
                TotalInvoiceAmount = await totalInvAmountTask,

                ActiveCustomers = await activeCustomersTask,
                TotalQuotations = await quotationCountTask,

                MonthlyTrend = monthlyTrend,
                TopCustomers = await topCustomersTask,

                RecentPOs = (await recentPOsTask).ToList(),
                RecentInvoices = (await recentInvoicesTask).ToList(),
                UnpaidInvoicesList = (await unpaidListTask).ToList(),
                PendingQuantityPOs = (await pendingQtyPOsTask).ToList()
            };

            return View(model);
        }

        private List<MonthlyTrendItem> MergeMonthlyTrends(
            List<MonthlyTrendItem> poTrend, List<MonthlyTrendItem> invTrend)
        {
            var merged = new Dictionary<(int Year, int Month), MonthlyTrendItem>();

            foreach (var po in poTrend)
            {
                var key = (po.Year, po.Month);
                if (!merged.ContainsKey(key))
                    merged[key] = new MonthlyTrendItem { Year = po.Year, Month = po.Month };
                merged[key].PoCount = po.PoCount;
                merged[key].PoAmount = po.PoAmount;
            }

            foreach (var inv in invTrend)
            {
                var key = (inv.Year, inv.Month);
                if (!merged.ContainsKey(key))
                    merged[key] = new MonthlyTrendItem { Year = inv.Year, Month = inv.Month };
                merged[key].InvoiceCount = inv.InvoiceCount;
                merged[key].InvoiceAmount = inv.InvoiceAmount;
            }

            return merged.OrderBy(x => x.Key.Year).ThenBy(x => x.Key.Month)
                         .Select(x => x.Value).ToList();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
