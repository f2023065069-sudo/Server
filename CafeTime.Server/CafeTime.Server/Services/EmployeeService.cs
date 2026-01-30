// Services/EmployeeService.cs
using CafeTime.Server.Common.DOTs;
using CafeTime.Server.Common.Models;
using MySql.Data.MySqlClient;
using System.Text.Json;

namespace CafeTime.Server.Services
{
    public static class EmployeeService
    {
        // Add Employee (User with Employee role)
        public static Response AddEmployee(User employee)
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                // Check if username already exists
                string checkQuery = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
                using var checkCmd = new MySqlCommand(checkQuery, connection);
                checkCmd.Parameters.AddWithValue("@Username", employee.Username);
                int userCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (userCount > 0)
                {
                    return new Response { Success = false, Message = "Username already exists" };
                }

                // Insert user as employee
                string query = @"INSERT INTO Users (Username, Password, Role, IsActive) 
                               VALUES (@Username, @Password, @Role, @IsActive);
                               SELECT LAST_INSERT_ID();";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Username", employee.Username);
                cmd.Parameters.AddWithValue("@Password", employee.Password); // In real app, hash this!
                cmd.Parameters.AddWithValue("@Role", "Employee");
                cmd.Parameters.AddWithValue("@IsActive", employee.IsActive);

                int userId = Convert.ToInt32(cmd.ExecuteScalar());

                return new Response
                {
                    Success = true,
                    Message = "Employee added successfully",
                    Data = userId
                };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Get All Employees
        public static Response GetEmployees()
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                string query = @"SELECT 
                                UserId, 
                                Username, 
                                Role, 
                                IsActive,
                                CreatedDate
                                FROM Users 
                                WHERE Role = 'Employee'
                                ORDER BY Username";

                using var cmd = new MySqlCommand(query, connection);
                using var reader = cmd.ExecuteReader();

                var employees = new List<User>();
                while (reader.Read())
                {
                    employees.Add(new User
                    {
                        UserId = reader.GetInt32("UserId"),
                        Username = reader.GetString("Username"),
                        Role = reader.GetString("Role"),
                        IsActive = reader.GetBoolean("IsActive"),
                        // You can add CreatedDate if you add it to User model
                    });
                }

                return new Response
                {
                    Success = true,
                    Message = "Employees retrieved successfully",
                    Data = JsonSerializer.Serialize(employees)
                };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Get Employee by ID
        public static Response GetEmployee(int userId)
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                string query = @"SELECT 
                                UserId, 
                                Username, 
                                Role, 
                                IsActive,
                                CreatedDate
                                FROM Users 
                                WHERE UserId = @UserId AND Role = 'Employee'";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);

                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    var employee = new User
                    {
                        UserId = reader.GetInt32("UserId"),
                        Username = reader.GetString("Username"),
                        Role = reader.GetString("Role"),
                        IsActive = reader.GetBoolean("IsActive")
                    };

                    return new Response
                    {
                        Success = true,
                        Message = "Employee found",
                        Data = JsonSerializer.Serialize(employee)
                    };
                }

                return new Response { Success = false, Message = "Employee not found" };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Update Employee
        public static Response UpdateEmployee(User employee)
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                // Check if employee exists
                string checkQuery = "SELECT COUNT(*) FROM Users WHERE UserId = @UserId AND Role = 'Employee'";
                using var checkCmd = new MySqlCommand(checkQuery, connection);
                checkCmd.Parameters.AddWithValue("@UserId", employee.UserId);
                int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (exists == 0)
                {
                    return new Response { Success = false, Message = "Employee not found" };
                }

                // Check if new username already exists (if changed)
                string usernameCheck = "SELECT Username FROM Users WHERE UserId = @UserId";
                using var usernameCmd = new MySqlCommand(usernameCheck, connection);
                usernameCmd.Parameters.AddWithValue("@UserId", employee.UserId);
                string currentUsername = usernameCmd.ExecuteScalar()?.ToString();

                if (currentUsername != employee.Username)
                {
                    string duplicateCheck = "SELECT COUNT(*) FROM Users WHERE Username = @Username AND UserId != @UserId";
                    using var duplicateCmd = new MySqlCommand(duplicateCheck, connection);
                    duplicateCmd.Parameters.AddWithValue("@Username", employee.Username);
                    duplicateCmd.Parameters.AddWithValue("@UserId", employee.UserId);
                    int duplicateCount = Convert.ToInt32(duplicateCmd.ExecuteScalar());

                    if (duplicateCount > 0)
                    {
                        return new Response { Success = false, Message = "Username already exists" };
                    }
                }

                // Update employee
                string query = @"UPDATE Users 
                               SET Username = @Username, 
                                   IsActive = @IsActive
                               WHERE UserId = @UserId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Username", employee.Username);
                cmd.Parameters.AddWithValue("@IsActive", employee.IsActive);
                cmd.Parameters.AddWithValue("@UserId", employee.UserId);

                int rows = cmd.ExecuteNonQuery();

                return new Response
                {
                    Success = rows > 0,
                    Message = rows > 0 ? "Employee updated successfully" : "Update failed"
                };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Delete/Deactivate Employee
        public static Response DeleteEmployee(int userId)
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                // Soft delete - just deactivate
                string query = "UPDATE Users SET IsActive = FALSE WHERE UserId = @UserId AND Role = 'Employee'";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);

                int rows = cmd.ExecuteNonQuery();

                return new Response
                {
                    Success = rows > 0,
                    Message = rows > 0 ? "Employee deactivated successfully" : "Employee not found"
                };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // Reset Password
        // In EmployeeService.cs - Add this method
        public static Response ResetPassword(string jsonData)
        {
            try
            {
                // Simple parsing without JsonHelper
                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData);

                int userId = Convert.ToInt32(data["UserId"]);
                string newPassword = data["NewPassword"].ToString();

                using var connection = DatabaseService.GetConnection();
                connection.Open();

                string query = "UPDATE Users SET Password = @Password WHERE UserId = @UserId AND Role = 'Employee'";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Password", newPassword);
                cmd.Parameters.AddWithValue("@UserId", userId);

                int rows = cmd.ExecuteNonQuery();

                return new Response
                {
                    Success = rows > 0,
                    Message = rows > 0 ? "Password reset successfully" : "Employee not found"
                };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = $"Error: {ex.Message}" };
            }
        }
    }
}