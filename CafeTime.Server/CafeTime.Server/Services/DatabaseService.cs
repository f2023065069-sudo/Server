// Services/DatabaseService.cs
using MySql.Data.MySqlClient;
using System.Data;

namespace CafeTime.Server.Services
{
    public static class DatabaseService
    {
        public static void Initialize()
        {
            try
            {
                using var connection = GetConnection();
                connection.Open();
                Console.WriteLine("Database connected successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database connection failed: {ex.Message}");
            }
        }

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(Config.ConnectionString);
        }
    }
}