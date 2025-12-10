using HRPackage.Models.ViewModels;
using Dapper;
using HRPackage.Services;
using System.Data;

namespace HRPackage.Repositories
{
    public interface IReportRepository
    {
        Task<(int TotalOrders, int CompletedOrders, decimal TotalRevenue, int PendingOrders)> GetDashboardStatsAsync();
         Task<IEnumerable<PurchaseOrderViewModel>> GetPendingQuantityReportAsync();
         Task<IEnumerable<PurchaseOrderViewModel>> GetPoSummaryReportAsync();
         Task<IEnumerable<SalesReportViewModel>> GetSalesReportAsync();
    }

    public class ReportRepository : IReportRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ReportRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<(int TotalOrders, int CompletedOrders, decimal TotalRevenue, int PendingOrders)> GetDashboardStatsAsync()
        {
             using var connection = _connectionFactory.CreateConnection();
             var sql = @"SELECT 
                             COUNT(*) as TotalOrders,
                             SUM(CASE WHEN IsCompleted = 1 THEN 1 ELSE 0 END) as CompletedOrders,
                             ISNULL(SUM(PoAmount), 0) as TotalRevenue,
                             SUM(CASE WHEN IsCompleted = 0 THEN 1 ELSE 0 END) as PendingOrders
                         FROM PurchaseOrders
                         WHERE IsDeleted = 0";
             var result = await connection.QuerySingleAsync<dynamic>(sql);
             return ((int)result.TotalOrders, (int)result.CompletedOrders, (decimal)result.TotalRevenue, (int)result.PendingOrders);
        }

        public async Task<IEnumerable<PurchaseOrderViewModel>> GetPendingQuantityReportAsync()
        {
            // Existing implementation... placeholder for brevity if not requested to change
            // But since I'm overwriting the file, I must provide full content or use replace.
            // I will use FULL implementation for safety designated in previous turns.
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT p.PoNumber, p.PoDate, c.CustomerName, 
                               poi.ItemDescription, poi.Quantity as OrderedQuantity,
                               ISNULL(SUM(ii.Quantity), 0) as InvoicedQuantity,
                               (poi.Quantity - ISNULL(SUM(ii.Quantity), 0)) as PendingQuantity
                        FROM PurchaseOrderItems poi
                        JOIN PurchaseOrders p ON poi.PoId = p.PoId
                        LEFT JOIN Customers c ON p.CustomerId = c.CustomerId
                        LEFT JOIN InvoiceItems ii ON poi.PoItemId = ii.PoItemId
                        LEFT JOIN Invoices i ON ii.InvoiceId = i.InvoiceId AND i.IsDeleted = 0
                        WHERE p.IsDeleted = 0 AND poi.IsDeleted = 0
                        GROUP BY p.PoNumber, p.PoDate, c.CustomerName, poi.ItemDescription, poi.Quantity
                        HAVING (poi.Quantity - ISNULL(SUM(ii.Quantity), 0)) > 0
                        ORDER BY p.PoDate";
             
             // Mapping to PO VM mostly for display
             var result = await connection.QueryAsync<dynamic>(sql);
             return result.Select(r => new PurchaseOrderViewModel {
                 PoNumber = r.PoNumber,
                 CustomerName = r.CustomerName,
                 PoDate = r.PoDate,
                 Items = new List<PurchaseOrderItemViewModel> {
                     new PurchaseOrderItemViewModel {
                         ItemDescription = r.ItemDescription,
                         Quantity = (int)r.OrderedQuantity,
                         PendingQuantity = (int)r.PendingQuantity
                         // Invoiced Qty not in VM but calc
                     }
                 }
             });
        }

        public async Task<IEnumerable<PurchaseOrderViewModel>> GetPoSummaryReportAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT p.PoNumber, p.PoDate, c.CustomerName, p.GrandTotal
                        FROM PurchaseOrders p
                        LEFT JOIN Customers c ON p.CustomerId = c.CustomerId
                        WHERE p.IsDeleted = 0
                        ORDER BY p.PoDate DESC";
            var result = await connection.QueryAsync<PurchaseOrderViewModel>(sql);
            return result;
        }

        public async Task<IEnumerable<SalesReportViewModel>> GetSalesReportAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            // Complex join for Sales Report
            // Invoices -> InvoiceItems -> PO Items (for description/qty) -> PO (for internal code) -> Customer
            // Note: Invoice amount is total, but items have individual amounts. Report seems to list Invoices.
            // If multiple items, we might sum or list 1st.
            // The image shows 1 row per Invoice usually, but Qty is there.
            // Let's assume 1 row per Invoice for simplicity or group.
            
            var sql = @"SELECT 
                            i.InvoiceNumber as InvoiceNo, 
                            i.InvoiceDate, 
                            c.CustomerName,
                            c.State,
                            c.GstNumber as CustomerGstNo,
                            p.InternalPoCode as ProjectName,
                            p.PoNumber as PoNo,
                            SUM(it.Quantity) as Qty,
                            i.TotalAmount as Amount,
                            (i.TotalAmount * i.CgstPercent / 100) as Cgst,
                            (i.TotalAmount * i.SgstPercent / 100) as Sgst,
                            (i.TotalAmount * i.IgstPercent / 100) as Igst,
                            i.GrandTotal as TotalAmount
                        FROM Invoices i
                        LEFT JOIN PurchaseOrders p ON i.PoId = p.PoId
                        LEFT JOIN Customers c ON p.CustomerId = c.CustomerId
                        LEFT JOIN InvoiceItems it ON i.InvoiceId = it.InvoiceId
                        WHERE i.IsDeleted = 0
                        GROUP BY i.InvoiceNumber, i.InvoiceDate, c.CustomerName, c.State, c.GstNumber, 
                                 p.InternalPoCode, p.PoNumber, i.TotalAmount, i.CgstPercent, i.SgstPercent, i.IgstPercent, i.GrandTotal
                        ORDER BY i.InvoiceDate DESC";
            
            var data = await connection.QueryAsync<SalesReportViewModel>(sql);
            int sl = 1;
            foreach(var d in data) { d.SlNo = sl++; }
            return data;
        }
    }
}
