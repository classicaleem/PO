using Dapper;
using HRPackage.Models;
using HRPackage.Services;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRPackage.Repositories
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
        Task<List<SelectListItem>> GetDropdownListAsync();
        Task<List<IndianState>> GetAllStatesAsync();
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
            var sql = @"SELECT CustomerId, CustomerCode, CustomerName, AddressLine1, AddressLine2, 
                               City, State, StateCode, Pincode, ContactNumber, EmailId, GstNumber,
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
            var sql = @"SELECT CustomerId, CustomerCode, CustomerName, AddressLine1, AddressLine2, 
                               City, State, StateCode, Pincode, ContactNumber, EmailId, GstNumber,
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
            var sql = @"SELECT CustomerId, CustomerCode, CustomerName, AddressLine1, AddressLine2, 
                               City, State, StateCode, Pincode, ContactNumber, EmailId, GstNumber,
                               DefaultCgstPercent, DefaultSgstPercent, DefaultIgstPercent,
                               IsActive, CreatedDate, CreatedByUserId
                        FROM Customers
                        WHERE CustomerId = @customerId";
            return await connection.QuerySingleOrDefaultAsync<Customer>(sql, new { customerId });
        }

        public async Task<int> CreateAsync(Customer customer)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"INSERT INTO Customers (CustomerCode, CustomerName, AddressLine1, AddressLine2, 
                               City, State, StateCode, Pincode, ContactNumber, EmailId, GstNumber,
                               DefaultCgstPercent, DefaultSgstPercent, DefaultIgstPercent,
                               IsActive, CreatedDate, CreatedByUserId)
                        VALUES (@CustomerCode, @CustomerName, @AddressLine1, @AddressLine2, 
                               @City, @State, @StateCode, @Pincode, @ContactNumber, @EmailId, @GstNumber,
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

        public async Task<List<SelectListItem>> GetDropdownListAsync()
        {
            var customers = await GetActiveAsync();
            return customers.Select(c => new SelectListItem
            {
                Value = c.CustomerId.ToString(),
                Text = $"{c.CustomerCode} - {c.CustomerName}"
            }).ToList();
        }

        public async Task<List<IndianState>> GetAllStatesAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT StateId, StateName, StateCode FROM IndianStates ORDER BY StateName";
            var states = await connection.QueryAsync<IndianState>(sql);
            return states.ToList();
        }
    }
}
