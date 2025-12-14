using Dapper;
using HRPackage.Models;
using HRPackage.Services;
using System.Data;

namespace HRPackage.Repositories
{
    public interface IDeliveryChallansRepository
    {
        Task<IEnumerable<DeliveryChallan>> GetAllAsync();
        Task<DeliveryChallan?> GetByIdAsync(int id);
        Task<int> CreateAsync(DeliveryChallan dc, List<DeliveryChallanItem> items);
        Task<string> GenerateNextDcNumberAsync();
        Task<bool> SoftDeleteAsync(int id);
        Task<IEnumerable<DeliveryChallan>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate);
    }

    public class DeliveryChallansRepository : IDeliveryChallansRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public DeliveryChallansRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IEnumerable<DeliveryChallan>> GetAllAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT dc.*, c.CustomerName 
                        FROM DeliveryChallans dc
                        LEFT JOIN Customers c ON dc.CustomerId = c.CustomerId
                        WHERE dc.IsDeleted = 0
                        ORDER BY dc.DcDate DESC";
            return await connection.QueryAsync<DeliveryChallan, Customer, DeliveryChallan>(
                sql, 
                (dc, c) => { dc.Customer = c; return dc; },
                splitOn: "CustomerName");
        }


        
        // Correct implementation of GetById
        public async Task<DeliveryChallan?> GetByIdAsync(int id) {
             using var connection = _connectionFactory.CreateConnection();
             var sqlHeader = @"SELECT dc.*, c.CustomerId, c.CustomerName, c.AddressLine1, c.AddressLine2, c.City, c.State, c.GstNumber
                               FROM DeliveryChallans dc
                               LEFT JOIN Customers c ON dc.CustomerId = c.CustomerId
                               WHERE dc.DcId = @id AND dc.IsDeleted = 0";
             
             var dc = await connection.QueryAsync<DeliveryChallan, Customer, DeliveryChallan>(
                 sqlHeader,
                 (challan, customer) => { challan.Customer = customer; return challan; },
                 new { id },
                 splitOn: "CustomerId"
             );
             
             var result = dc.FirstOrDefault();
             if (result != null) {
                 var sqlItems = "SELECT * FROM DeliveryChallanItems WHERE DcId = @id AND IsDeleted = 0 ORDER BY SlNo";
                 result.Items = (await connection.QueryAsync<DeliveryChallanItem>(sqlItems, new { id })).ToList();
             }
             return result;
        }

        public async Task<int> CreateAsync(DeliveryChallan dc, List<DeliveryChallanItem> items)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var sqlDc = @"INSERT INTO DeliveryChallans (DcNumber, DcDate, CustomerId, TargetCompany, VehicleNo, CreatedByUserId, IsDeleted)
                              VALUES (@DcNumber, @DcDate, @CustomerId, @TargetCompany, @VehicleNo, @CreatedByUserId, 0);
                              SELECT CAST(SCOPE_IDENTITY() as int)";
                var dcId = await connection.QuerySingleAsync<int>(sqlDc, dc, transaction);

                var sqlItem = @"INSERT INTO DeliveryChallanItems (DcId, SlNo, Description, Quantity, Unit, Remarks, IsDeleted)
                                VALUES (@DcId, @SlNo, @Description, @Quantity, @Unit, @Remarks, 0)";
                
                int slNo = 1;
                foreach(var item in items)
                {
                    item.DcId = dcId;
                    item.SlNo = slNo++;
                    await connection.ExecuteAsync(sqlItem, item, transaction);
                }

                transaction.Commit();
                return dcId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<string> GenerateNextDcNumberAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            // Format: "SIM/DC/122/2526" - let's assume SIM/DC/{SEQUENCE}/{YEAR}
            // For simplicity, I'll stick to a sequence like SIM/DC/0001
            var sql = "SELECT COUNT(*) FROM DeliveryChallans";
            var count = await connection.ExecuteScalarAsync<int>(sql);
            return $"SIM/DC/{count + 1:D4}";
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = "UPDATE DeliveryChallans SET IsDeleted = 1 WHERE DcId = @id";
            return await connection.ExecuteAsync(sql, new { id }) > 0;
        }

        public async Task<IEnumerable<DeliveryChallan>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT dc.*, c.CustomerName 
                        FROM DeliveryChallans dc
                        LEFT JOIN Customers c ON dc.CustomerId = c.CustomerId
                        WHERE dc.IsDeleted = 0
                        AND dc.DcDate >= @fromDate AND dc.DcDate <= @toDate
                        ORDER BY dc.DcDate DESC";
            return await connection.QueryAsync<DeliveryChallan, Customer, DeliveryChallan>(
                sql, 
                (dc, c) => { dc.Customer = c; return dc; },
                new { fromDate, toDate },
                splitOn: "CustomerName");
        }
    }
}
