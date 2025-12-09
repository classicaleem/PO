using Dapper;
using HRPackage.Models;
using HRPackage.Models.ViewModels;
using HRPackage.Services;

namespace HRPackage.Repositories
{
    public interface IReportRepository
    {
        Task<PoReportViewModel> GetPoReportAsync();
        Task<PendingQuantityReportViewModel> GetPendingQuantityReportAsync();
    }

    public class ReportRepository : IReportRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ReportRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<PoReportViewModel> GetPoReportAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var sql = @"
                SELECT 
                    SUM(PoAmount) as TotalAmount,
                    COUNT(*) as TotalCount,
                    SUM(CASE WHEN IsCompleted = 1 THEN 1 ELSE 0 END) as CompletedCount,
                    SUM(CASE WHEN IsCompleted = 0 THEN 1 ELSE 0 END) as PendingCount
                FROM PurchaseOrders WHERE IsDeleted = 0;

                SELECT 
                    SUM(TotalAmount) as TotalInvoiced,
                    COUNT(*) as InvoiceCount,
                    SUM(CASE WHEN IsPaid = 1 THEN TotalAmount ELSE 0 END) as PaidAmount,
                    SUM(CASE WHEN IsPaid = 0 THEN TotalAmount ELSE 0 END) as UnpaidAmount
                FROM Invoices WHERE IsDeleted = 0";

            using var multi = await connection.QueryMultipleAsync(sql);
            var poStats = await multi.ReadSingleAsync<dynamic>();
            var invStats = await multi.ReadSingleAsync<dynamic>();

            return new PoReportViewModel
            {
                TotalPoAmount = poStats.TotalAmount ?? 0,
                TotalPoCount = poStats.TotalCount ?? 0,
                CompletedPoCount = poStats.CompletedCount ?? 0,
                PendingPoCount = poStats.PendingCount ?? 0,
                TotalInvoicedAmount = invStats.TotalInvoiced ?? 0,
                TotalInvoiceCount = invStats.InvoiceCount ?? 0,
                PaidAmount = invStats.PaidAmount ?? 0,
                UnpaidAmount = invStats.UnpaidAmount ?? 0
            };
        }

        public async Task<PendingQuantityReportViewModel> GetPendingQuantityReportAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            
            // Get PO summaries with pending quantities
            var poSql = @"SELECT p.PoId, p.InternalPoCode, p.PoNumber, c.CustomerName, p.PoDate,
                                 ISNULL(SUM(poi.Quantity), 0) as TotalOrderedQuantity,
                                 ISNULL(SUM(ISNULL((
                                     SELECT SUM(ii.Quantity) 
                                     FROM InvoiceItems ii 
                                     JOIN Invoices i ON ii.InvoiceId = i.InvoiceId 
                                     WHERE ii.PoItemId = poi.PoItemId AND i.IsDeleted = 0
                                 ), 0)), 0) as TotalInvoicedQuantity
                          FROM PurchaseOrders p
                          LEFT JOIN Customers c ON p.CustomerId = c.CustomerId
                          LEFT JOIN PurchaseOrderItems poi ON p.PoId = poi.PoId AND poi.IsDeleted = 0
                          WHERE p.IsDeleted = 0
                          GROUP BY p.PoId, p.InternalPoCode, p.PoNumber, c.CustomerName, p.PoDate
                          ORDER BY p.PoDate DESC";
            
            var poSummaries = await connection.QueryAsync<PendingQuantityPoSummary>(poSql);
            var summaryList = poSummaries.ToList();

            // Get item details for each PO
            foreach (var summary in summaryList)
            {
                var itemSql = @"SELECT poi.PoItemId, poi.LineNumber, poi.ItemDescription, 
                                       poi.Quantity as OrderedQuantity,
                                       ISNULL((
                                           SELECT SUM(ii.Quantity) 
                                           FROM InvoiceItems ii 
                                           JOIN Invoices i ON ii.InvoiceId = i.InvoiceId 
                                           WHERE ii.PoItemId = poi.PoItemId AND i.IsDeleted = 0
                                       ), 0) as InvoicedQuantity
                                FROM PurchaseOrderItems poi
                                WHERE poi.PoId = @PoId AND poi.IsDeleted = 0
                                ORDER BY poi.LineNumber";
                
                var items = await connection.QueryAsync<PendingQuantityItemDetail>(itemSql, new { summary.PoId });
                summary.ItemDetails = items.ToList();
            }

            return new PendingQuantityReportViewModel
            {
                PoSummaries = summaryList
            };
        }
    }
}
