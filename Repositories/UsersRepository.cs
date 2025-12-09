using Dapper;
using HRPackage.Models;
using HRPackage.Services;

namespace HRPackage.Repositories
{
    public interface IUsersRepository
    {
        Task<User?> ValidateUserAsync(string username, string password);
        Task<User?> GetByIdAsync(int userId);
    }

    public class UsersRepository : IUsersRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public UsersRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<User?> ValidateUserAsync(string username, string password)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"SELECT UserId, Username, Password, Role, IsActive 
                        FROM Users 
                        WHERE Username = @username AND Password = @password AND IsActive = 1";
            return await connection.QuerySingleOrDefaultAsync<User>(sql, new { username, password });
        }

        public async Task<User?> GetByIdAsync(int userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = "SELECT UserId, Username, Password, Role, IsActive FROM Users WHERE UserId = @userId";
            return await connection.QuerySingleOrDefaultAsync<User>(sql, new { userId });
        }
    }
}
