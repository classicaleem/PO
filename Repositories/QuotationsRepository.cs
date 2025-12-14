using Dapper;
using HRPackage.Models;
using HRPackage.Services;
using System.Data;

namespace HRPackage.Repositories
{
    public interface IQuotationsRepository
    {
        Task<IEnumerable<Quotation>> GetAllAsync();
        Task<Quotation?> GetByIdAsync(int id);
        Task<int> CreateAsync(Quotation quotation, List<QuotationItem> items);
        Task<string> GenerateNextQuotationNoAsync();
        Task<bool> SoftDeleteAsync(int id);
        Task<IEnumerable<Quotation>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate);
    }

    public class QuotationsRepository : IQuotationsRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public QuotationsRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<Quotation>> GetAllAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT q.*, c.CustomerName 
                        FROM Quotations q
                        LEFT JOIN Customers c ON q.CustomerId = c.CustomerId
                        WHERE q.IsDeleted = 0
                        ORDER BY q.Date DESC";
            return await connection.QueryAsync<Quotation, Customer, Quotation>(
                sql, 
                (q, c) => { q.Customer = c; return q; },
                splitOn: "CustomerName");
        }

        public async Task<Quotation?> GetByIdAsync(int id)
        {
             using var connection = _connectionFactory.CreateConnection();
             var sqlHeader = @"SELECT q.*, c.*
                               FROM Quotations q
                               LEFT JOIN Customers c ON q.CustomerId = c.CustomerId
                               WHERE q.QuotationId = @id AND q.IsDeleted = 0";
             
             var q = await connection.QueryAsync<Quotation, Customer, Quotation>(
                 sqlHeader,
                 (quotation, customer) => { quotation.Customer = customer; return quotation; },
                 new { id },
                 splitOn: "CustomerId"
             );
             
             var result = q.FirstOrDefault();
             if (result != null) {
                 var sqlItems = "SELECT * FROM QuotationItems WHERE QuotationId = @id AND IsDeleted = 0 ORDER BY SlNo";
                 result.Items = (await connection.QueryAsync<QuotationItem>(sqlItems, new { id })).ToList();
             }
             return result;
        }

        public async Task<int> CreateAsync(Quotation quotation, List<QuotationItem> items)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var sqlQ = @"INSERT INTO Quotations (QuotationNo, Date, ValidUntil, CustomerId, CreatedByUserId, IsDeleted)
                             VALUES (@QuotationNo, @Date, @ValidUntil, @CustomerId, @CreatedByUserId, 0);
                             SELECT CAST(SCOPE_IDENTITY() as int)";
                var qId = await connection.QuerySingleAsync<int>(sqlQ, quotation, transaction);

                var sqlItem = @"INSERT INTO QuotationItems (QuotationId, SlNo, Description, Quantity, UnitPrice, TotalAmount, IsDeleted)
                                VALUES (@QuotationId, @SlNo, @Description, @Quantity, @UnitPrice, @TotalAmount, 0)";
                
                int slNo = 1;
                foreach(var item in items)
                {
                    item.QuotationId = qId;
                    item.SlNo = slNo++;
                    item.TotalAmount = item.Quantity * item.UnitPrice;
                    await connection.ExecuteAsync(sqlItem, item, transaction);
                }

                transaction.Commit();
                return qId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<string> GenerateNextQuotationNoAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            // Format: SIM/QN/185/2526
            var sql = "SELECT COUNT(*) FROM Quotations";
            var count = await connection.ExecuteScalarAsync<int>(sql);
            return $"SIM/QN/{count + 1:D3}/2526"; // Hardcoded year for similarity to image
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = "UPDATE Quotations SET IsDeleted = 1 WHERE QuotationId = @id";
            return await connection.ExecuteAsync(sql, new { id }) > 0;
        }

        public async Task<IEnumerable<Quotation>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT q.*, c.CustomerName 
                        FROM Quotations q
                        LEFT JOIN Customers c ON q.CustomerId = c.CustomerId
                        WHERE q.IsDeleted = 0 
                        AND q.Date >= @fromDate AND q.Date <= @toDate
                        ORDER BY q.Date DESC";
            return await connection.QueryAsync<Quotation, Customer, Quotation>(
                sql, 
                (q, c) => { q.Customer = c; return q; },
                new { fromDate, toDate },
                splitOn: "CustomerName");
        }
    }
}
