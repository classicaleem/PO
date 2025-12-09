using Dapper;
using HRPackage.Models;
using HRPackage.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace HRPackage.Repositories
{
    public interface IPurchaseOrdersRepository
    {
        Task<IEnumerable<PurchaseOrder>> GetAllAsync();
        Task<PurchaseOrder?> GetByIdAsync(int poId);
        Task<PurchaseOrder?> GetByIdWithItemsAsync(int poId);
        Task<PurchaseOrder?> GetByIdWithInvoicesAsync(int poId);
        Task<int> CreateWithItemsAsync(PurchaseOrder po, List<PurchaseOrderItem> items);
        Task<bool> UpdateWithItemsAsync(PurchaseOrder po, List<PurchaseOrderItem> items);
        Task<bool> SoftDeleteAsync(int poId);
        Task<(int TotalPOs, int CompletedPOs, decimal TotalAmount)> GetDashboardStatsAsync();
        Task<IEnumerable<PurchaseOrder>> GetRecentAsync(int count);
        Task<bool> PoNumberExistsAsync(string poNumber, int? excludePoId = null);
        Task<string> GenerateNextInternalPoCodeAsync();
        Task<List<SelectListItem>> GetDropdownListAsync();
        Task<List<PurchaseOrderItem>> GetItemsWithPendingAsync(int poId);
        Task<bool> UpdateCompletionStatusAsync(int poId);
    }

    public class PurchaseOrdersRepository : IPurchaseOrdersRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public PurchaseOrdersRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<PurchaseOrder>> GetAllAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT p.PoId, p.PoNumber, p.InternalPoCode, p.CustomerId, p.SupplierName, 
                               p.PoAmount, p.PoDate, p.StartDate, p.EndDate, p.CreatedDate, 
                               p.CreatedByUserId, p.IsCompleted, p.IsDeleted, 
                               u.Username as CreatedByUsername,
                               c.CustomerName
                        FROM PurchaseOrders p
                        LEFT JOIN Users u ON p.CreatedByUserId = u.UserId
                        LEFT JOIN Customers c ON p.CustomerId = c.CustomerId
                        WHERE p.IsDeleted = 0
                        ORDER BY p.CreatedDate DESC";
            return await connection.QueryAsync<PurchaseOrder>(sql);
        }

        public async Task<PurchaseOrder?> GetByIdAsync(int poId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT p.PoId, p.PoNumber, p.InternalPoCode, p.CustomerId, p.SupplierName, 
                               p.PoAmount, p.PoDate, p.StartDate, p.EndDate, p.CreatedDate, 
                               p.CreatedByUserId, p.IsCompleted, p.IsDeleted, 
                               u.Username as CreatedByUsername,
                               c.CustomerName
                        FROM PurchaseOrders p
                        LEFT JOIN Users u ON p.CreatedByUserId = u.UserId
                        LEFT JOIN Customers c ON p.CustomerId = c.CustomerId
                        WHERE p.PoId = @poId AND p.IsDeleted = 0";
            return await connection.QuerySingleOrDefaultAsync<PurchaseOrder>(sql, new { poId });
        }

        public async Task<PurchaseOrder?> GetByIdWithItemsAsync(int poId)
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var sql = @"SELECT p.PoId, p.PoNumber, p.InternalPoCode, p.CustomerId, p.SupplierName, 
                               p.PoAmount, p.PoDate, p.StartDate, p.EndDate, p.CreatedDate, 
                               p.CreatedByUserId, p.IsCompleted, p.IsDeleted, 
                               u.Username as CreatedByUsername,
                               c.CustomerName
                        FROM PurchaseOrders p
                        LEFT JOIN Users u ON p.CreatedByUserId = u.UserId
                        LEFT JOIN Customers c ON p.CustomerId = c.CustomerId
                        WHERE p.PoId = @poId AND p.IsDeleted = 0;

                        SELECT PoItemId, PoId, LineNumber, ItemDescription, Quantity, UnitPrice, LineTotal, IsDeleted
                        FROM PurchaseOrderItems
                        WHERE PoId = @poId AND IsDeleted = 0
                        ORDER BY LineNumber";

            using var multi = await connection.QueryMultipleAsync(sql, new { poId });
            var po = await multi.ReadSingleOrDefaultAsync<PurchaseOrder>();
            if (po != null)
            {
                po.Items = (await multi.ReadAsync<PurchaseOrderItem>()).ToList();
            }
            return po;
        }

        public async Task<PurchaseOrder?> GetByIdWithInvoicesAsync(int poId)
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var sql = @"SELECT p.PoId, p.PoNumber, p.InternalPoCode, p.CustomerId, p.SupplierName, 
                               p.PoAmount, p.PoDate, p.StartDate, p.EndDate, p.CreatedDate, 
                               p.CreatedByUserId, p.IsCompleted, p.IsDeleted, 
                               u.Username as CreatedByUsername,
                               c.CustomerName
                        FROM PurchaseOrders p
                        LEFT JOIN Users u ON p.CreatedByUserId = u.UserId
                        LEFT JOIN Customers c ON p.CustomerId = c.CustomerId
                        WHERE p.PoId = @poId AND p.IsDeleted = 0;

                        SELECT PoItemId, PoId, LineNumber, ItemDescription, Quantity, UnitPrice, LineTotal, IsDeleted
                        FROM PurchaseOrderItems
                        WHERE PoId = @poId AND IsDeleted = 0
                        ORDER BY LineNumber;
                        
                        SELECT InvoiceId, PoId, InvoiceNumber, InvoiceDate, TotalAmount, IsPaid, IsDeleted
                        FROM Invoices
                        WHERE PoId = @poId AND IsDeleted = 0
                        ORDER BY InvoiceDate DESC";

            using var multi = await connection.QueryMultipleAsync(sql, new { poId });
            var po = await multi.ReadSingleOrDefaultAsync<PurchaseOrder>();
            if (po != null)
            {
                po.Items = (await multi.ReadAsync<PurchaseOrderItem>()).ToList();
                po.Invoices = (await multi.ReadAsync<Invoice>()).ToList();
            }
            return po;
        }

        public async Task<int> CreateWithItemsAsync(PurchaseOrder po, List<PurchaseOrderItem> items)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            
            try
            {
                // Insert PO header
                var poSql = @"INSERT INTO PurchaseOrders (PoNumber, InternalPoCode, CustomerId, SupplierName, 
                                   PoAmount, PoDate, StartDate, EndDate, CreatedDate, CreatedByUserId, IsCompleted, IsDeleted)
                              VALUES (@PoNumber, @InternalPoCode, @CustomerId, @SupplierName, 
                                   @PoAmount, @PoDate, @StartDate, @EndDate, GETDATE(), @CreatedByUserId, 0, 0);
                              SELECT CAST(SCOPE_IDENTITY() as int)";
                var poId = await connection.QuerySingleAsync<int>(poSql, po, transaction);

                // Insert items
                var itemSql = @"INSERT INTO PurchaseOrderItems (PoId, LineNumber, ItemDescription, Quantity, UnitPrice, LineTotal, IsDeleted)
                                VALUES (@PoId, @LineNumber, @ItemDescription, @Quantity, @UnitPrice, @LineTotal, 0)";
                
                int lineNum = 1;
                foreach (var item in items)
                {
                    item.PoId = poId;
                    item.LineNumber = lineNum++;
                    item.LineTotal = item.Quantity * item.UnitPrice;
                    await connection.ExecuteAsync(itemSql, item, transaction);
                }

                transaction.Commit();
                return poId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> UpdateWithItemsAsync(PurchaseOrder po, List<PurchaseOrderItem> items)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            
            try
            {
                // Update PO header
                var poSql = @"UPDATE PurchaseOrders 
                              SET PoNumber = @PoNumber, CustomerId = @CustomerId, SupplierName = @SupplierName, 
                                  PoAmount = @PoAmount, PoDate = @PoDate, StartDate = @StartDate, EndDate = @EndDate,
                                  IsCompleted = @IsCompleted
                              WHERE PoId = @PoId AND IsDeleted = 0";
                await connection.ExecuteAsync(poSql, po, transaction);

                // Soft delete existing items
                var deleteSql = "UPDATE PurchaseOrderItems SET IsDeleted = 1 WHERE PoId = @PoId";
                await connection.ExecuteAsync(deleteSql, new { po.PoId }, transaction);

                // Insert new items
                var itemSql = @"INSERT INTO PurchaseOrderItems (PoId, LineNumber, ItemDescription, Quantity, UnitPrice, LineTotal, IsDeleted)
                                VALUES (@PoId, @LineNumber, @ItemDescription, @Quantity, @UnitPrice, @LineTotal, 0)";
                
                int lineNum = 1;
                foreach (var item in items)
                {
                    item.PoId = po.PoId;
                    item.LineNumber = lineNum++;
                    item.LineTotal = item.Quantity * item.UnitPrice;
                    await connection.ExecuteAsync(itemSql, item, transaction);
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> SoftDeleteAsync(int poId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"UPDATE PurchaseOrders SET IsDeleted = 1 WHERE PoId = @poId;
                        UPDATE PurchaseOrderItems SET IsDeleted = 1 WHERE PoId = @poId";
            var rowsAffected = await connection.ExecuteAsync(sql, new { poId });
            return rowsAffected > 0;
        }

        public async Task<(int TotalPOs, int CompletedPOs, decimal TotalAmount)> GetDashboardStatsAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT 
                            COUNT(*) as TotalPOs,
                            SUM(CASE WHEN IsCompleted = 1 THEN 1 ELSE 0 END) as CompletedPOs,
                            ISNULL(SUM(PoAmount), 0) as TotalAmount
                        FROM PurchaseOrders 
                        WHERE IsDeleted = 0";
            var result = await connection.QuerySingleAsync<dynamic>(sql);
            return ((int)result.TotalPOs, (int)result.CompletedPOs, (decimal)result.TotalAmount);
        }

        public async Task<IEnumerable<PurchaseOrder>> GetRecentAsync(int count)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT TOP (@count) p.PoId, p.PoNumber, p.InternalPoCode, p.CustomerId, p.SupplierName, 
                               p.PoAmount, p.PoDate, p.CreatedDate, p.CreatedByUserId, p.IsCompleted, p.IsDeleted, 
                               u.Username as CreatedByUsername, c.CustomerName
                        FROM PurchaseOrders p
                        LEFT JOIN Users u ON p.CreatedByUserId = u.UserId
                        LEFT JOIN Customers c ON p.CustomerId = c.CustomerId
                        WHERE p.IsDeleted = 0
                        ORDER BY p.CreatedDate DESC";
            return await connection.QueryAsync<PurchaseOrder>(sql, new { count });
        }

        public async Task<bool> PoNumberExistsAsync(string poNumber, int? excludePoId = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT COUNT(*) FROM PurchaseOrders 
                        WHERE PoNumber = @poNumber AND IsDeleted = 0 
                        AND (@excludePoId IS NULL OR PoId != @excludePoId)";
            var count = await connection.QuerySingleAsync<int>(sql, new { poNumber, excludePoId });
            return count > 0;
        }

        public async Task<string> GenerateNextInternalPoCodeAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT ISNULL(MAX(CAST(RIGHT(InternalPoCode, 4) AS INT)), 0) + 1 
                        FROM PurchaseOrders 
                        WHERE InternalPoCode LIKE 'SIMPO%'";
            var nextNum = await connection.QuerySingleAsync<int>(sql);
            return $"SIMPO{nextNum:D4}";
        }

        public async Task<List<SelectListItem>> GetDropdownListAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT p.PoId, p.InternalPoCode, p.PoNumber, c.CustomerName
                        FROM PurchaseOrders p
                        LEFT JOIN Customers c ON p.CustomerId = c.CustomerId
                        WHERE p.IsDeleted = 0 AND p.IsCompleted = 0
                        ORDER BY p.InternalPoCode DESC";
            var pos = await connection.QueryAsync<dynamic>(sql);
            return pos.Select(p => new SelectListItem
            {
                Value = ((int)p.PoId).ToString(),
                Text = $"{p.InternalPoCode} - {p.PoNumber} ({p.CustomerName ?? "No Customer"})"
            }).ToList();
        }

        public async Task<List<PurchaseOrderItem>> GetItemsWithPendingAsync(int poId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT poi.PoItemId, poi.PoId, poi.LineNumber, poi.ItemDescription, 
                               poi.Quantity, poi.UnitPrice, poi.LineTotal, poi.IsDeleted,
                               ISNULL(SUM(ii.Quantity), 0) as InvoicedQuantity
                        FROM PurchaseOrderItems poi
                        LEFT JOIN InvoiceItems ii ON poi.PoItemId = ii.PoItemId
                        LEFT JOIN Invoices i ON ii.InvoiceId = i.InvoiceId AND i.IsDeleted = 0
                        WHERE poi.PoId = @poId AND poi.IsDeleted = 0
                        GROUP BY poi.PoItemId, poi.PoId, poi.LineNumber, poi.ItemDescription, 
                                 poi.Quantity, poi.UnitPrice, poi.LineTotal, poi.IsDeleted
                        ORDER BY poi.LineNumber";
            var items = await connection.QueryAsync<PurchaseOrderItem>(sql, new { poId });
            return items.ToList();
        }

        public async Task<bool> UpdateCompletionStatusAsync(int poId)
        {
            using var connection = _connectionFactory.CreateConnection();
            
            // Check if all items are fully invoiced
            var sql = @"SELECT CASE 
                            WHEN NOT EXISTS (
                                SELECT 1 FROM PurchaseOrderItems poi
                                WHERE poi.PoId = @poId AND poi.IsDeleted = 0
                                AND poi.Quantity > ISNULL((
                                    SELECT SUM(ii.Quantity) 
                                    FROM InvoiceItems ii 
                                    JOIN Invoices i ON ii.InvoiceId = i.InvoiceId 
                                    WHERE ii.PoItemId = poi.PoItemId AND i.IsDeleted = 0
                                ), 0)
                            ) THEN 1 ELSE 0 
                        END";
            var isComplete = await connection.QuerySingleAsync<bool>(sql, new { poId });
            
            var updateSql = "UPDATE PurchaseOrders SET IsCompleted = @isComplete WHERE PoId = @poId";
            await connection.ExecuteAsync(updateSql, new { poId, isComplete });
            
            return isComplete;
        }
    }
}
