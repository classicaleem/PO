using Dapper;
using SmartPO.Models;
using SmartPO.Services;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartPO.Repositories
{
    public interface ICustomersRepository
    {
        Task<IEnumerable<Customer>> GetAllAsync();
        Task<IEnumerable<Customer>> GetActiveAsync();
        Task<Customer?> GetByIdAsync(int customerId);
        Task<int> CreateAsync(Customer customer);
        Task<bool> UpdateAsync(Customer customer);
        Task<bool> SoftDeleteAsync(int customerId);
        Task<bool> CustomerCodeExistsAsync(string customerCode, int? excludeCustomerId = null);
        Task<string> GetNextCustomerCodeAsync();
        Task<List<SelectListItem>> GetDropdownListAsync();
        Task<List<IndianState>> GetAllStatesAsync();
        Task<int> GetActiveCountAsync();
    }

    public class CustomersRepository : ICustomersRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public CustomersRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT CustomerId, CustomerCode, CustomerName, CustomerAlias,
                               AddressLine1, AddressLine2, City, State, StateCode, Pincode,
                               ContactNumber, EmailId, GstNumber,
                               DefaultCgstPercent, DefaultSgstPercent, DefaultIgstPercent,
                               IsActive, CreatedDate, CreatedByUserId
                        FROM Customers
                        WHERE IsActive = 1
                        ORDER BY CustomerName";
            return await connection.QueryAsync<Customer>(sql);
        }

        public async Task<IEnumerable<Customer>> GetActiveAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT CustomerId, CustomerCode, CustomerName, CustomerAlias,
                               AddressLine1, AddressLine2, City, State, StateCode, Pincode,
                               ContactNumber, EmailId, GstNumber,
                               DefaultCgstPercent, DefaultSgstPercent, DefaultIgstPercent,
                               IsActive, CreatedDate, CreatedByUserId
                        FROM Customers
                        WHERE IsActive = 1
                        ORDER BY CustomerName";
            return await connection.QueryAsync<Customer>(sql);
        }

        public async Task<Customer?> GetByIdAsync(int customerId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT CustomerId, CustomerCode, CustomerName, CustomerAlias,
                               AddressLine1, AddressLine2, City, State, StateCode, Pincode,
                               ContactNumber, EmailId, GstNumber,
                               DefaultCgstPercent, DefaultSgstPercent, DefaultIgstPercent,
                               IsActive, CreatedDate, CreatedByUserId
                        FROM Customers
                        WHERE CustomerId = @customerId";
            return await connection.QuerySingleOrDefaultAsync<Customer>(sql, new { customerId });
        }

        public async Task<int> CreateAsync(Customer customer)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"INSERT INTO Customers (CustomerCode, CustomerName, CustomerAlias,
                               AddressLine1, AddressLine2, City, State, StateCode, Pincode,
                               ContactNumber, EmailId, GstNumber,
                               DefaultCgstPercent, DefaultSgstPercent, DefaultIgstPercent,
                               IsActive, CreatedDate, CreatedByUserId)
                        VALUES (@CustomerCode, @CustomerName, @CustomerAlias,
                               @AddressLine1, @AddressLine2, @City, @State, @StateCode, @Pincode,
                               @ContactNumber, @EmailId, @GstNumber,
                               @DefaultCgstPercent, @DefaultSgstPercent, @DefaultIgstPercent,
                               @IsActive, GETDATE(), @CreatedByUserId);
                        SELECT CAST(SCOPE_IDENTITY() as int)";
            return await connection.QuerySingleAsync<int>(sql, customer);
        }

        public async Task<bool> UpdateAsync(Customer customer)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"UPDATE Customers
                        SET CustomerCode = @CustomerCode, CustomerName = @CustomerName,
                            CustomerAlias = @CustomerAlias,
                            AddressLine1 = @AddressLine1, AddressLine2 = @AddressLine2,
                            City = @City, State = @State, StateCode = @StateCode, Pincode = @Pincode,
                            ContactNumber = @ContactNumber, EmailId = @EmailId, GstNumber = @GstNumber,
                            DefaultCgstPercent = @DefaultCgstPercent, DefaultSgstPercent = @DefaultSgstPercent,
                            DefaultIgstPercent = @DefaultIgstPercent, IsActive = @IsActive
                        WHERE CustomerId = @CustomerId";
            var rowsAffected = await connection.ExecuteAsync(sql, customer);
            return rowsAffected > 0;
        }

        public async Task<bool> SoftDeleteAsync(int customerId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = "UPDATE Customers SET IsActive = 0 WHERE CustomerId = @customerId";
            var rowsAffected = await connection.ExecuteAsync(sql, new { customerId });
            return rowsAffected > 0;
        }

        public async Task<bool> CustomerCodeExistsAsync(string customerCode, int? excludeCustomerId = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT COUNT(*) FROM Customers 
                        WHERE CustomerCode = @customerCode 
                        AND (@excludeCustomerId IS NULL OR CustomerId != @excludeCustomerId)";
            var count = await connection.QuerySingleAsync<int>(sql, new { customerCode, excludeCustomerId });
            return count > 0;
        }

        public async Task<string> GetNextCustomerCodeAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            // Use MAX CustomerId so deletes don't cause duplicate codes
            var sql = @"SELECT 'CUST' + RIGHT('0000' + CAST(ISNULL(MAX(CustomerId), 0) + 1 AS VARCHAR(10)), 4)
                        FROM Customers";
            return await connection.QuerySingleAsync<string>(sql);
        }

        public async Task<List<SelectListItem>> GetDropdownListAsync()
        {
            var customers = await GetActiveAsync();
            return customers.Select(c => new SelectListItem
            {
                Value = c.CustomerId.ToString(),
                Text = string.IsNullOrWhiteSpace(c.CustomerAlias)
                    ? $"{c.CustomerCode} - {c.CustomerName}"
                    : $"{c.CustomerCode} - {c.CustomerName} ({c.CustomerAlias})"
            }).ToList();
        }

        public async Task<List<IndianState>> GetAllStatesAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT StateId, StateName, StateCode FROM IndianStates ORDER BY StateName";
            var states = await connection.QueryAsync<IndianState>(sql);
            return states.ToList();
        }

        public async Task<int> GetActiveCountAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = "SELECT COUNT(*) FROM Customers WHERE IsActive = 1";
            return await connection.QuerySingleAsync<int>(sql);
        }
    }
}
