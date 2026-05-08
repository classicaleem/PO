using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPO.Services;

namespace SmartPO.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public AdminController(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IActionResult DataMaintenance()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TruncateAllTables()
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                connection.Open();

                // Delete in FK-safe order: children before parents.
                // Tables preserved: Customers, IndianStates, Users
                var statements = new[]
                {
                    // Level 3 – deepest children
                    "DELETE FROM InvoiceItems",
                    "DELETE FROM DeliveryChallanItems",
                    "DELETE FROM QuotationItems",

                    // Level 2 – mid-level children
                    "DELETE FROM PurchaseOrderItems",
                    "DELETE FROM Invoices",
                    "DELETE FROM DeliveryChallans",
                    "DELETE FROM Quotations",

                    // Level 1 – root parents (except preserved tables)
                    "DELETE FROM PurchaseOrders",

                    // Reset identity seeds so IDs start from 1 again
                    "DBCC CHECKIDENT ('InvoiceItems',       RESEED, 0)",
                    "DBCC CHECKIDENT ('DeliveryChallanItems', RESEED, 0)",
                    "DBCC CHECKIDENT ('QuotationItems',     RESEED, 0)",
                    "DBCC CHECKIDENT ('PurchaseOrderItems', RESEED, 0)",
                    "DBCC CHECKIDENT ('Invoices',           RESEED, 0)",
                    "DBCC CHECKIDENT ('DeliveryChallans',   RESEED, 0)",
                    "DBCC CHECKIDENT ('Quotations',         RESEED, 0)",
                    "DBCC CHECKIDENT ('PurchaseOrders',     RESEED, 0)",
                };

                foreach (var sql in statements)
                {
                    await connection.ExecuteAsync(sql);
                }

                TempData["Success"] = "All transactional data has been cleared successfully. Customers, Users and Indian States are preserved.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction(nameof(DataMaintenance));
        }
    }
}
