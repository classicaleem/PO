using HRPackage.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRPackage.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly IReportRepository _reportRepository;

        public ReportsController(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public async Task<IActionResult> Index()
        {
            var report = await _reportRepository.GetPoReportAsync();
            return View(report);
        }

        public async Task<IActionResult> PendingQuantity()
        {
            var report = await _reportRepository.GetPendingQuantityReportAsync();
            return View(report);
        }
    }
}
