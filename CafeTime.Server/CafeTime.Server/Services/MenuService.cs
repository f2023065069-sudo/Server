using CafeTime.Server.Common.DOTs;
using CafeTime.Server.Common.Models;
using MySql.Data.MySqlClient;
using System.Text.Json;

namespace CafeTime.Server.Services
{
    public static class MenuService
    {
        public static Response AddMenuItem(MenuItem menuItem)
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                string query = @"INSERT INTO Menu (ItemName, Price, Category, IsAvailable) 
                               VALUES (@ItemName, @Price, @Category, @IsAvailable)";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@ItemName", menuItem.ItemName);
                cmd.Parameters.AddWithValue("@Price", menuItem.Price);
                cmd.Parameters.AddWithValue("@Category", menuItem.Category);
                cmd.Parameters.AddWithValue("@IsAvailable", menuItem.IsAvailable);

                int rows = cmd.ExecuteNonQuery();

                return new Response
                {
                    Success = rows > 0,
                    Message = rows > 0 ? "Menu item added successfully" : "Failed to add menu item"
                };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public static Response GetMenuItems()
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                string query = "SELECT * FROM Menu WHERE IsAvailable = TRUE";
                using var cmd = new MySqlCommand(query, connection);
                using var reader = cmd.ExecuteReader();

                var menuItems = new List<MenuItem>();
                while (reader.Read())
                {
                    menuItems.Add(new MenuItem
                    {
                        MenuId = reader.GetInt32("MenuId"),
                        ItemName = reader.GetString("ItemName"),
                        Price = reader.GetDecimal("Price"),
                        Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? "" : reader.GetString("Category"),
                        IsAvailable = reader.GetBoolean("IsAvailable")
                    });
                }

                return new Response
                {
                    Success = true,
                    Message = "Menu retrieved successfully",
                    Data = JsonSerializer.Serialize(menuItems)
                };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public static Response UpdateMenuItem(MenuItem menuItem)
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                string query = @"UPDATE Menu SET ItemName = @ItemName, Price = @Price, 
                               Category = @Category WHERE MenuId = @MenuId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@ItemName", menuItem.ItemName);
                cmd.Parameters.AddWithValue("@Price", menuItem.Price);
                cmd.Parameters.AddWithValue("@Category", menuItem.Category);
                cmd.Parameters.AddWithValue("@MenuId", menuItem.MenuId);

                int rows = cmd.ExecuteNonQuery();

                return new Response
                {
                    Success = rows > 0,
                    Message = rows > 0 ? "Menu item updated successfully" : "Menu item not found"
                };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public static Response DeleteMenuItem(int menuId)
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                string query = "UPDATE Menu SET IsAvailable = FALSE WHERE MenuId = @MenuId";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@MenuId", menuId);

                int rows = cmd.ExecuteNonQuery();

                return new Response
                {
                    Success = rows > 0,
                    Message = rows > 0 ? "Menu item deleted successfully" : "Menu item not found"
                };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = $"Error: {ex.Message}" };
            }
        }
    }
}