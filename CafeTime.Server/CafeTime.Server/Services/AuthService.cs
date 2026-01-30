using CafeTime.Server.Common.DOTs;
using CafeTime.Server.Common.Models;
using MySql.Data.MySqlClient;
using System.Text.Json;

namespace CafeTime.Server.Services
{
    public static class AuthService
    {
        public static Response Login(LoginRequest loginRequest)
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                string query = @"SELECT UserId, Username, Role, IsActive 
                                 FROM Users 
                                 WHERE Username = @Username 
                                 AND Password = @Password 
                                 AND IsActive = TRUE";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Username", loginRequest.Username);
                cmd.Parameters.AddWithValue("@Password", loginRequest.Password);

                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    var user = new User
                    {
                        UserId = reader.GetInt32("UserId"),
                        Username = reader.GetString("Username"),
                        Role = reader.GetString("Role"),
                        IsActive = reader.GetBoolean("IsActive")
                    };

                    reader.Close();

                    // ✅ LOGIN HISTORY
                    DatabaseService.AddLoginHistory(new LoginHistory
                    {
                        Username = loginRequest.Username,
                        LoginTime = DateTime.Now
                    });

                    return new Response
                    {
                        Success = true,
                        Message = "Login successful",
                        Data = JsonSerializer.Serialize(user)
                    };
                }

                return new Response
                {
                    Success = false,
                    Message = "Invalid username or password"
                };
            }
            catch (Exception ex)
            {
                return new Response
                {
                    Success = false,
                    Message = $"Login error: {ex.Message}"
                };
            }
        }
    }
}
