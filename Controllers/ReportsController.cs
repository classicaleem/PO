using HRPackage.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRPackage.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly IReportRepository _reportRepository;

        public ReportsController(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SalesReport()
        {
            var data = await _reportRepository.GetSalesReportAsync();
            return View(data);
        }

        public async Task<IActionResult> PendingQuantity()
        {
            var data = await _reportRepository.GetPendingQuantityReportAsync();
            return View(data);
        }

        public async Task<IActionResult> PoSummary()
        {
            var data = await _reportRepository.GetPoSummaryReportAsync();
            return View(data);
        }
    }
}
