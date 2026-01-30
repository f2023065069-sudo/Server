using CafeTime.Server.Common.Models;
using MySql.Data.MySqlClient;

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

        // ================= LOGIN HISTORY =================

        public static void AddLoginHistory(LoginHistory history)
        {
            using var connection = GetConnection();
            connection.Open();

            string query = @"INSERT INTO LoginHistory (Username, LoginTime)
                             VALUES (@Username, @LoginTime)";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Username", history.Username);
            cmd.Parameters.AddWithValue("@LoginTime", history.LoginTime);

            cmd.ExecuteNonQuery();
        }

        // ================= EXPENSE =================

        public static void AddExpense(Expense expense)
        {
            using var connection = GetConnection();
            connection.Open();

            string query = @"INSERT INTO Expenses (Type, Description, Amount, ExpenseDate)
                             VALUES (@Type, @Description, @Amount, @Date)";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Type", expense.Type);
            cmd.Parameters.AddWithValue("@Description", expense.Description);
            cmd.Parameters.AddWithValue("@Amount", expense.Amount);
            cmd.Parameters.AddWithValue("@Date", expense.Date);

            cmd.ExecuteNonQuery();
        }
    }
}
