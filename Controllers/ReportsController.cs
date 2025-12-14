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



        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SalesReport()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesReportJson(DateTime? fromDate, DateTime? toDate)
        {
            var end = toDate ?? DateTime.Today;
            var start = fromDate ?? DateTime.Today.AddDays(-30); // Default 30 days for report
            var endOfDay = end.Date.AddDays(1).AddTicks(-1);

            var data = await _reportRepository.GetSalesReportAsync(start, endOfDay);
            return Json(data);
        }


    }
}
