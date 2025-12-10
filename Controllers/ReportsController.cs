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
            var data = await _reportRepository.GetSalesReportAsync();
            return View(data);
        }


    }
}
