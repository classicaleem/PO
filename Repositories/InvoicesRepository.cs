using Dapper;
using HRPackage.Models;
using HRPackage.Models.ViewModels;
using HRPackage.Services;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRPackage.Repositories
{
    public interface IInvoicesRepository
    {
        Task<IEnumerable<Invoice>> GetAllAsync();
        Task<IEnumerable<Invoice>> GetByPoIdAsync(int poId);
        Task<Invoice?> GetByIdAsync(int invoiceId);
        Task<Invoice?> GetByIdWithItemsAsync(int invoiceId);
        Task<int> CreateWithItemsAsync(Invoice invoice, List<InvoiceItem> items);
        Task<bool> UpdateAsync(Invoice invoice);
        Task<bool> SoftDeleteAsync(int invoiceId);
        Task<int> GetUnpaidCountAsync();
        Task<bool> InvoiceNumberExistsAsync(string invoiceNumber, int? excludeInvoiceId = null);
        Task<List<InvoiceItemViewModel>> GetPoItemsForInvoiceAsync(int poId);
        Task<List<InvoiceItem>> GetInvoiceItemsAsync(int invoiceId);
        Task<string> GenerateNextInvoiceNumberAsync();
    }

    public class InvoicesRepository : IInvoicesRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public InvoicesRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<Invoice>> GetAllAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT i.InvoiceId, i.PoId, i.InvoiceNumber, i.InvoiceDate, i.TotalAmount, 
                               i.CgstPercent, i.SgstPercent, i.IgstPercent, i.TaxAmount, i.RoundOff, i.GrandTotal,
                               i.ShippingAddress, i.IsPaid, i.IsDeleted,
                               p.PoNumber, p.InternalPoCode, c.CustomerName, c.CustomerId,
                               (SELECT ISNULL(SUM(Quantity), 0) FROM InvoiceItems WHERE InvoiceId = i.InvoiceId) as TotalQuantity
                        FROM Invoices i
                        INNER JOIN PurchaseOrders p ON i.PoId = p.PoId
                        LEFT JOIN Customers c ON p.CustomerId = c.CustomerId
                        WHERE i.IsDeleted = 0 AND p.IsDeleted = 0
                        ORDER BY i.InvoiceDate DESC";
            return await connection.QueryAsync<Invoice>(sql);
        }

        public async Task<IEnumerable<Invoice>> GetByPoIdAsync(int poId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT InvoiceId, PoId, InvoiceNumber, InvoiceDate, TotalAmount, 
                               CgstPercent, SgstPercent, IgstPercent, TaxAmount, RoundOff, GrandTotal,
                               ShippingAddress, IsPaid, IsDeleted
                        FROM Invoices
                        WHERE PoId = @poId AND IsDeleted = 0
                        ORDER BY InvoiceDate DESC";
            return await connection.QueryAsync<Invoice>(sql, new { poId });
        }

        public async Task<Invoice?> GetByIdAsync(int invoiceId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT i.InvoiceId, i.PoId, i.InvoiceNumber, i.InvoiceDate, i.TotalAmount, 
                               i.CgstPercent, i.SgstPercent, i.IgstPercent, i.TaxAmount, i.RoundOff, i.GrandTotal,
                               i.ShippingAddress, i.IsPaid, i.IsDeleted,
                               p.PoNumber, p.InternalPoCode, c.CustomerName, c.CustomerId
                        FROM Invoices i
                        INNER JOIN PurchaseOrders p ON i.PoId = p.PoId
                        LEFT JOIN Customers c ON p.CustomerId = c.CustomerId
                        WHERE i.InvoiceId = @invoiceId AND i.IsDeleted = 0";
            return await connection.QuerySingleOrDefaultAsync<Invoice>(sql, new { invoiceId });
        }

        public async Task<Invoice?> GetByIdWithItemsAsync(int invoiceId)
        {
            using var connection = _connectionFactory.CreateConnection();
            
            var sql = @"SELECT i.InvoiceId, i.PoId, i.InvoiceNumber, i.InvoiceDate, i.TotalAmount, 
                               i.CgstPercent, i.SgstPercent, i.IgstPercent, i.TaxAmount, i.RoundOff, i.GrandTotal,
                               i.ShippingAddress, i.IsPaid, i.IsDeleted,
                               p.PoNumber, p.InternalPoCode, c.CustomerName, c.CustomerId
                        FROM Invoices i
                        INNER JOIN PurchaseOrders p ON i.PoId = p.PoId
                        LEFT JOIN Customers c ON p.CustomerId = c.CustomerId
                        WHERE i.InvoiceId = @invoiceId AND i.IsDeleted = 0;

                        SELECT ii.InvoiceItemId, ii.InvoiceId, ii.PoItemId, ii.Quantity, ii.UnitPrice, ii.LineAmount,
                               poi.ItemDescription, poi.HsnCode, poi.Quantity as OrderedQuantity
                        FROM InvoiceItems ii
                        INNER JOIN PurchaseOrderItems poi ON ii.PoItemId = poi.PoItemId
                        WHERE ii.InvoiceId = @invoiceId";

            using var multi = await connection.QueryMultipleAsync(sql, new { invoiceId });
            var invoice = await multi.ReadSingleOrDefaultAsync<Invoice>();
            if (invoice != null)
            {
                invoice.Items = (await multi.ReadAsync<InvoiceItem>()).ToList();
                invoice.TotalQuantity = invoice.Items.Sum(i => i.Quantity);
            }
            return invoice;
        }

        public async Task<int> CreateWithItemsAsync(Invoice invoice, List<InvoiceItem> items)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            
            try
            {
                // Calculate total amount
                invoice.TotalAmount = items.Sum(i => i.LineAmount);
                
                // Calculate tax
                // (Assuming TaxAmount is already calculated or we recalculate it here for safety? 
                // The current code passes TaxAmount from controller. Let's assume controller does math 
                // OR we trust the incoming model. Let's keep existing pattern but add rounding.)
                
                // Auto-Round Logic
                // If GrandTotal comes in, we might overwrite it OR we calculate fresh.
                // Safest to calculate fresh from TotalAmount + Taxes so we know it's consistent.
                
                decimal subTotal = invoice.TotalAmount;
                decimal tax = (subTotal * invoice.CgstPercent / 100) + 
                              (subTotal * invoice.SgstPercent / 100) + 
                              (subTotal * invoice.IgstPercent / 100);
                
                invoice.TaxAmount = tax;
                decimal totalWithTax = subTotal + tax;
                decimal roundedTotal = Math.Round(totalWithTax, 0, MidpointRounding.AwayFromZero); // Standard rounding: .5 goes up
                
                invoice.RoundOff = roundedTotal - totalWithTax;
                invoice.GrandTotal = roundedTotal;

                // Insert invoice header
                var invoiceSql = @"INSERT INTO Invoices (PoId, InvoiceNumber, InvoiceDate, TotalAmount, 
                                               CgstPercent, SgstPercent, IgstPercent, TaxAmount, RoundOff, GrandTotal,
                                               ShippingAddress, IsPaid, IsDeleted)
                                   VALUES (@PoId, @InvoiceNumber, @InvoiceDate, @TotalAmount, 
                                           @CgstPercent, @SgstPercent, @IgstPercent, @TaxAmount, @RoundOff, @GrandTotal,
                                           @ShippingAddress, @IsPaid, 0);
                                   SELECT CAST(SCOPE_IDENTITY() as int)";
                var invoiceId = await connection.QuerySingleAsync<int>(invoiceSql, invoice, transaction);

                // Insert items
                var itemSql = @"INSERT INTO InvoiceItems (InvoiceId, PoItemId, Quantity, UnitPrice, LineAmount)
                                VALUES (@InvoiceId, @PoItemId, @Quantity, @UnitPrice, @LineAmount)";
                
                foreach (var item in items.Where(i => i.Quantity > 0))
                {
                    item.InvoiceId = invoiceId;
                    item.LineAmount = item.Quantity * item.UnitPrice;
                    await connection.ExecuteAsync(itemSql, item, transaction);
                }

                transaction.Commit();
                return invoiceId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> UpdateAsync(Invoice invoice)
        {
            // Auto-Round Logic for Update
            // We need to re-calculate because percentages or amount might have changed.
            // But here we don't have Items list to verify TotalAmount. 
            // We assume invoice.TotalAmount is correct from the form/controller.
            
            decimal subTotal = invoice.TotalAmount;
            decimal tax = (subTotal * invoice.CgstPercent / 100) + 
                          (subTotal * invoice.SgstPercent / 100) + 
                          (subTotal * invoice.IgstPercent / 100);
                          
            invoice.TaxAmount = tax;
            decimal totalWithTax = subTotal + tax;
            decimal roundedTotal = Math.Round(totalWithTax, 0, MidpointRounding.AwayFromZero);
            
            invoice.RoundOff = roundedTotal - totalWithTax;
            invoice.GrandTotal = roundedTotal;

            using var connection = _connectionFactory.CreateConnection();
            var sql = @"UPDATE Invoices 
                        SET PoId = @PoId, InvoiceNumber = @InvoiceNumber, InvoiceDate = @InvoiceDate, 
                            TotalAmount = @TotalAmount, CgstPercent = @CgstPercent, SgstPercent = @SgstPercent, 
                            IgstPercent = @IgstPercent, TaxAmount = @TaxAmount, RoundOff = @RoundOff, GrandTotal = @GrandTotal,
                            ShippingAddress = @ShippingAddress, IsPaid = @IsPaid
                        WHERE InvoiceId = @InvoiceId AND IsDeleted = 0";
            var rowsAffected = await connection.ExecuteAsync(sql, invoice);
            return rowsAffected > 0;
        }

        public async Task<bool> SoftDeleteAsync(int invoiceId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = "UPDATE Invoices SET IsDeleted = 1 WHERE InvoiceId = @invoiceId";
            var rowsAffected = await connection.ExecuteAsync(sql, new { invoiceId });
            return rowsAffected > 0;
        }

        public async Task<int> GetUnpaidCountAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT COUNT(*) FROM Invoices i
                        INNER JOIN PurchaseOrders p ON i.PoId = p.PoId
                        WHERE i.IsDeleted = 0 AND p.IsDeleted = 0 AND i.IsPaid = 0";
            return await connection.QuerySingleAsync<int>(sql);
        }

        public async Task<bool> InvoiceNumberExistsAsync(string invoiceNumber, int? excludeInvoiceId = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT COUNT(*) FROM Invoices 
                        WHERE InvoiceNumber = @invoiceNumber AND IsDeleted = 0 
                        AND (@excludeInvoiceId IS NULL OR InvoiceId != @excludeInvoiceId)";
            var count = await connection.QuerySingleAsync<int>(sql, new { invoiceNumber, excludeInvoiceId });
            return count > 0;
        }

        public async Task<List<InvoiceItemViewModel>> GetPoItemsForInvoiceAsync(int poId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT poi.PoItemId, poi.ItemDescription, poi.HsnCode, poi.Quantity as OrderedQuantity, 
                               poi.UnitPrice,
                               ISNULL((
                                   SELECT SUM(ii.Quantity) 
                                   FROM InvoiceItems ii 
                                   JOIN Invoices i ON ii.InvoiceId = i.InvoiceId 
                                   WHERE ii.PoItemId = poi.PoItemId AND i.IsDeleted = 0
                               ), 0) as PreviouslyInvoiced
                        FROM PurchaseOrderItems poi
                        WHERE poi.PoId = @poId AND poi.IsDeleted = 0
                        ORDER BY poi.LineNumber";

            var items = await connection.QueryAsync<dynamic>(sql, new { poId });

            return items.Select(i => new InvoiceItemViewModel
            {
                PoItemId = (int)i.PoItemId,
                ItemDescription = (string)i.ItemDescription,
                HsnCode = (string)i.HsnCode,
                OrderedQuantity = (int)i.OrderedQuantity,
                UnitPrice = (decimal)i.UnitPrice,
                PreviouslyInvoiced = (int)i.PreviouslyInvoiced,
                PendingQuantity = (int)i.OrderedQuantity - (int)i.PreviouslyInvoiced,
                ThisInvoiceQuantity = 0,
                LineAmount = 0
            }).ToList();
        }
        public async Task<string> GenerateNextInvoiceNumberAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT ISNULL(MAX(CAST(RIGHT(InvoiceNumber, 4) AS INT)), 0) + 1 
                        FROM Invoices 
                        WHERE InvoiceNumber LIKE 'SIMINV%'";
            var nextNum = await connection.QuerySingleAsync<int>(sql);
            return $"SIMINV{nextNum:D4}";
        }

        public async Task<List<InvoiceItem>> GetInvoiceItemsAsync(int invoiceId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT ii.InvoiceItemId, ii.InvoiceId, ii.PoItemId, ii.Quantity, ii.UnitPrice, ii.LineAmount,
                               poi.ItemDescription, poi.HsnCode
                        FROM InvoiceItems ii
                        INNER JOIN PurchaseOrderItems poi ON ii.PoItemId = poi.PoItemId
                        WHERE ii.InvoiceId = @invoiceId";
            var items = await connection.QueryAsync<InvoiceItem>(sql, new { invoiceId });
            return items.ToList();
        }
    }
}
