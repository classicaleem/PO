using HRPackage.Models.ViewModels;
using Dapper;
using HRPackage.Services;
using System.Data;

namespace HRPackage.Repositories
{
    public interface IReportRepository
    {
        Task<(int TotalOrders, int CompletedOrders, decimal TotalRevenue, int PendingOrders)> GetDashboardStatsAsync();
         Task<PendingQuantityReportViewModel> GetPendingQuantityReportAsync();
         Task<IEnumerable<PurchaseOrderViewModel>> GetPoSummaryReportAsync();
         Task<IEnumerable<SalesReportViewModel>> GetSalesReportAsync();
         Task<IEnumerable<SalesReportViewModel>> GetSalesReportAsync(DateTime fromDate, DateTime toDate);
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
             // Handle nulls in dynamic result safely
             int total = result.TotalOrders != null ? (int)result.TotalOrders : 0;
             int completed = result.CompletedOrders != null ? (int)result.CompletedOrders : 0;
             decimal revenue = result.TotalRevenue != null ? (decimal)result.TotalRevenue : 0;
             int pending = result.PendingOrders != null ? (int)result.PendingOrders : 0;
             
             return (total, completed, revenue, pending);
        }

        public async Task<PendingQuantityReportViewModel> GetPendingQuantityReportAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT 
                            p.PoId, p.InternalPoCode, p.PoNumber, p.PoDate, c.CustomerName,
                            poi.PoItemId, poi.LineNumber, poi.ItemDescription, poi.Quantity as OrderedQty,
                            ISNULL(SUM(ii.Quantity), 0) as InvoicedQty,
                            CASE WHEN (poi.Quantity - ISNULL(SUM(ii.Quantity), 0)) > 0 THEN 1 ELSE 0 END as HasPending
                        FROM PurchaseOrders p
                        INNER JOIN PurchaseOrderItems poi ON p.PoId = poi.PoId
                        LEFT JOIN Customers c ON p.CustomerId = c.CustomerId
                        LEFT JOIN InvoiceItems ii ON poi.PoItemId = ii.PoItemId
                        LEFT JOIN Invoices i ON ii.InvoiceId = i.InvoiceId AND i.IsDeleted = 0
                        WHERE p.IsDeleted = 0 AND poi.IsDeleted = 0
                        GROUP BY p.PoId, p.InternalPoCode, p.PoNumber, p.PoDate, c.CustomerName,
                                 poi.PoItemId, poi.LineNumber, poi.ItemDescription, poi.Quantity
                        ORDER BY p.PoDate DESC, p.PoId, poi.LineNumber";
             
             var rawData = await connection.QueryAsync<dynamic>(sql);

             var viewModel = new PendingQuantityReportViewModel();
             
             // Group by PO
             var grouped = rawData.GroupBy(r => (int)r.PoId);

             foreach (var group in grouped)
             {
                 var first = group.First();
                 var summary = new PendingQuantityPoSummary
                 {
                     PoId = first.PoId,
                     InternalPoCode = first.InternalPoCode,
                     PoNumber = first.PoNumber,
                     PoDate = first.PoDate,
                     CustomerName = first.CustomerName,
                     ItemDetails = new List<PendingQuantityItemDetail>()
                 };

                 foreach (var item in group)
                 {
                     summary.ItemDetails.Add(new PendingQuantityItemDetail
                     {
                         PoItemId = item.PoItemId,
                         LineNumber = item.LineNumber,
                         ItemDescription = item.ItemDescription,
                         OrderedQuantity = (int)item.OrderedQty,
                         InvoicedQuantity = (int)item.InvoicedQty
                     });
                 }

                 // Calculate aggregations
                 summary.TotalOrderedQuantity = summary.ItemDetails.Sum(i => i.OrderedQuantity);
                 summary.TotalInvoicedQuantity = summary.ItemDetails.Sum(i => i.InvoicedQuantity);

                 // Only add if there is any pending OR if we want to show all. 
                 // Usually pending report shows items with Issues. 
                 // But let's show all for completeness as per view logic which filters/highlights
                 viewModel.PoSummaries.Add(summary);
             }

             return viewModel;
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
            
            var sql = @"
            ;WITH PoStats AS (
                SELECT 
                    p.PoId,
                    (SELECT ISNULL(SUM(Quantity), 0) FROM PurchaseOrderItems WHERE PoId = p.PoId AND IsDeleted = 0) -
                    (SELECT ISNULL(SUM(ii.Quantity), 0) FROM InvoiceItems ii 
                     JOIN Invoices inv ON ii.InvoiceId = inv.InvoiceId 
                     WHERE inv.PoId = p.PoId AND inv.IsDeleted = 0) as PendingQty
                FROM PurchaseOrders p
                WHERE p.IsDeleted = 0
            )
            SELECT 
                i.InvoiceNumber as InvoiceNo, 
                i.InvoiceDate, 
                c.CustomerName,
                c.State,
                c.GstNumber as CustomerGstNo,
                p.InternalPoCode as ProjectName,
                p.PoNumber as PoNo,
                ISNULL(SUM(it.Quantity), 0) as Qty,
                ISNULL(i.TotalAmount, 0) as Amount,
                ISNULL((i.TotalAmount * i.CgstPercent / 100), 0) as Cgst,
                ISNULL((i.TotalAmount * i.SgstPercent / 100), 0) as Sgst,
                ISNULL((i.TotalAmount * i.IgstPercent / 100), 0) as Igst,
                ISNULL(i.GrandTotal, 0) as TotalAmount,
                ISNULL(i.TaxAmount, 0) as TaxAmount,
                i.IsPaid,
                i.IsDeleted,
                p.PoId,
                ISNULL(stats.PendingQty, 0) as PendingQty
            FROM Invoices i
            LEFT JOIN PurchaseOrders p ON i.PoId = p.PoId
            LEFT JOIN PoStats stats ON p.PoId = stats.PoId
            LEFT JOIN Customers c ON p.CustomerId = c.CustomerId
            LEFT JOIN InvoiceItems it ON i.InvoiceId = it.InvoiceId
            GROUP BY i.InvoiceNumber, i.InvoiceDate, c.CustomerName, c.State, c.GstNumber, 
                     p.InternalPoCode, p.PoNumber, i.TotalAmount, i.CgstPercent, i.SgstPercent, i.IgstPercent, 
                     i.GrandTotal, i.TaxAmount, i.IsPaid, i.IsDeleted, p.PoId, stats.PendingQty
            ORDER BY i.InvoiceDate DESC";
            
            var result = await connection.QueryAsync<dynamic>(sql);

            return result.Select(d => 
            {
                decimal amount = d.Amount != null ? (decimal)d.Amount : 0;
                decimal grandTotal = d.TotalAmount != null ? (decimal)d.TotalAmount : 0;
                
                // Safety fix for null tax components
                decimal cgst = d.Cgst != null ? (decimal)d.Cgst : 0;
                decimal sgst = d.Sgst != null ? (decimal)d.Sgst : 0;
                decimal igst = d.Igst != null ? (decimal)d.Igst : 0;
                
                decimal tax = d.TaxAmount != null ? (decimal)d.TaxAmount : (cgst + sgst + igst);
                var roundOff = grandTotal - (amount + tax);

                int pending = d.PendingQty != null ? (int)d.PendingQty : 0;

                return new SalesReportViewModel 
                {
                    SlNo = 0, // Assigned later
                    InvoiceNo = d.InvoiceNo,
                    InvoiceDate = d.InvoiceDate,
                    CustomerName = d.CustomerName,
                    State = d.State,
                    CustomerGstNo = d.CustomerGstNo,
                    ProjectName = d.ProjectName,
                    PoNo = d.PoNo,
                    Qty = d.Qty != null ? (int)d.Qty : 0,
                    Amount = amount,
                    Cgst = cgst,
                    Sgst = sgst,
                    Igst = igst,
                    RoundOff = roundOff,
                    TotalAmount = grandTotal,
                    PendingQty = pending < 0 ? 0 : pending,
                    IsPaid = d.IsPaid,
                    PaymentStatus = d.IsDeleted ? "cancel" : (d.IsPaid ? "received" : "pending"),
                    GstStatus = d.IsDeleted ? "cancel" : "Approve"
                };
            }).Select((x, index) => { x.SlNo = index + 1; return x; }).ToList();
        }

        public async Task<IEnumerable<SalesReportViewModel>> GetSalesReportAsync(DateTime fromDate, DateTime toDate)
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var sql = @"
            ;WITH PoStats AS (
                SELECT 
                    p.PoId,
                    (SELECT ISNULL(SUM(Quantity), 0) FROM PurchaseOrderItems WHERE PoId = p.PoId AND IsDeleted = 0) -
                    (SELECT ISNULL(SUM(ii.Quantity), 0) FROM InvoiceItems ii 
                     JOIN Invoices inv ON ii.InvoiceId = inv.InvoiceId 
                     WHERE inv.PoId = p.PoId AND inv.IsDeleted = 0) as PendingQty
                FROM PurchaseOrders p
                WHERE p.IsDeleted = 0
            )
            SELECT 
                i.InvoiceNumber as InvoiceNo, 
                i.InvoiceDate, 
                c.CustomerName,
                c.State,
                c.GstNumber as CustomerGstNo,
                p.InternalPoCode as ProjectName,
                p.PoNumber as PoNo,
                -- Aggregate HSN Codes
                -- Aggregate HSN Codes
                -- Aggregate HSN Codes
                STUFF((SELECT DISTINCT ', ' + COALESCE(HsnCode, '') FROM InvoiceItems WHERE InvoiceId = i.InvoiceId FOR XML PATH('')), 1, 2, '') as HsnCode,
                ISNULL(SUM(it.Quantity), 0) as Qty,
                ISNULL(i.TotalAmount, 0) as Amount,
                ISNULL((i.TotalAmount * i.CgstPercent / 100), 0) as Cgst,
                ISNULL((i.TotalAmount * i.SgstPercent / 100), 0) as Sgst,
                ISNULL((i.TotalAmount * i.IgstPercent / 100), 0) as Igst,
                -- Select Percentages
                ISNULL(i.CgstPercent, 0) as CgstPercent,
                ISNULL(i.SgstPercent, 0) as SgstPercent,
                ISNULL(i.IgstPercent, 0) as IgstPercent,
                ISNULL(i.GrandTotal, 0) as TotalAmount,
                ISNULL(i.TaxAmount, 0) as TaxAmount,
                i.IsPaid,
                i.IsDeleted,
                p.PoId,
                ISNULL(stats.PendingQty, 0) as PendingQty
            FROM Invoices i
            LEFT JOIN PurchaseOrders p ON i.PoId = p.PoId
            LEFT JOIN PoStats stats ON p.PoId = stats.PoId
            LEFT JOIN Customers c ON p.CustomerId = c.CustomerId
            LEFT JOIN InvoiceItems it ON i.InvoiceId = it.InvoiceId
            WHERE i.InvoiceDate >= @fromDate AND i.InvoiceDate <= @toDate
            GROUP BY i.InvoiceId, i.InvoiceNumber, i.InvoiceDate, c.CustomerName, c.State, c.GstNumber, 
                     p.InternalPoCode, p.PoNumber, i.TotalAmount, i.CgstPercent, i.SgstPercent, i.IgstPercent, 
                     i.GrandTotal, i.TaxAmount, i.IsPaid, i.IsDeleted, p.PoId, stats.PendingQty
            ORDER BY i.InvoiceDate DESC";
            
            var result = await connection.QueryAsync<dynamic>(sql, new { fromDate, toDate });

            return result.Select(d => 
            {
                decimal amount = d.Amount != null ? (decimal)d.Amount : 0;
                decimal grandTotal = d.TotalAmount != null ? (decimal)d.TotalAmount : 0;
                
                decimal cgst = d.Cgst != null ? (decimal)d.Cgst : 0;
                decimal sgst = d.Sgst != null ? (decimal)d.Sgst : 0;
                decimal igst = d.Igst != null ? (decimal)d.Igst : 0;
                
                decimal tax = d.TaxAmount != null ? (decimal)d.TaxAmount : (cgst + sgst + igst);
                var roundOff = grandTotal - (amount + tax);

                int pending = d.PendingQty != null ? (int)d.PendingQty : 0;

                return new SalesReportViewModel 
                {
                    SlNo = 0,
                    InvoiceNo = d.InvoiceNo,
                    InvoiceDate = d.InvoiceDate,
                    CustomerName = d.CustomerName,
                    State = d.State,
                    CustomerGstNo = d.CustomerGstNo,
                    ProjectName = d.ProjectName,
                    PoNo = d.PoNo,
                    HsnCode = d.HsnCode,
                    Qty = d.Qty != null ? (int)d.Qty : 0,
                    Amount = amount,
                    Cgst = cgst,
                    Sgst = sgst,
                    Igst = igst,
                    CgstPercent = d.CgstPercent != null ? (decimal)d.CgstPercent : 0,
                    SgstPercent = d.SgstPercent != null ? (decimal)d.SgstPercent : 0,
                    IgstPercent = d.IgstPercent != null ? (decimal)d.IgstPercent : 0,
                    RoundOff = roundOff,
                    TotalAmount = grandTotal,
                    PendingQty = pending < 0 ? 0 : pending,
                    IsPaid = d.IsPaid,
                    PaymentStatus = d.IsDeleted ? "cancel" : (d.IsPaid ? "received" : "pending"),
                    GstStatus = d.IsDeleted ? "cancel" : "Approve"
                };
            }).Select((x, index) => { x.SlNo = index + 1; return x; }).ToList();
        }
    }
}
