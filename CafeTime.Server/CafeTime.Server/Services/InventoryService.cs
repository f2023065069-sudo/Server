using CafeTime.Server.Common.DOTs;
using CafeTime.Server.Common.Models;
using MySql.Data.MySqlClient;
using System.Text.Json;

namespace CafeTime.Server.Services
{
    public static class InventoryService
    {
        public static Response AddInventoryItem(InventoryItem item)
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                string query = @"INSERT INTO Inventory (ItemName, Quantity, LowStockLevel) 
                               VALUES (@ItemName, @Quantity, @LowStockLevel)";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@ItemName", item.ItemName);
                cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                cmd.Parameters.AddWithValue("@LowStockLevel", item.LowStockLevel);

                int rows = cmd.ExecuteNonQuery();

                return new Response
                {
                    Success = rows > 0,
                    Message = rows > 0 ? "Inventory item added successfully" : "Failed to add item"
                };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public static Response GetInventoryItems()
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                string query = "SELECT * FROM Inventory ORDER BY ItemName";
                using var cmd = new MySqlCommand(query, connection);
                using var reader = cmd.ExecuteReader();

                var items = new List<InventoryItem>();
                while (reader.Read())
                {
                    items.Add(new InventoryItem
                    {
                        InventoryId = reader.GetInt32("InventoryId"),
                        ItemName = reader.GetString("ItemName"),
                        Quantity = reader.GetInt32("Quantity"),
                        LowStockLevel = reader.GetInt32("LowStockLevel")
                    });
                }

                return new Response
                {
                    Success = true,
                    Message = "Inventory retrieved successfully",
                    Data = JsonSerializer.Serialize(items)
                };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public static Response UpdateInventoryItem(InventoryItem item)
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                string query = @"UPDATE Inventory SET Quantity = @Quantity 
                               WHERE InventoryId = @InventoryId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                cmd.Parameters.AddWithValue("@InventoryId", item.InventoryId);

                int rows = cmd.ExecuteNonQuery();

                return new Response
                {
                    Success = rows > 0,
                    Message = rows > 0 ? "Inventory updated successfully" : "Item not found"
                };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public static Response DeleteInventoryItem(int inventoryId)
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                string query = "DELETE FROM Inventory WHERE InventoryId = @InventoryId";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@InventoryId", inventoryId);

                int rows = cmd.ExecuteNonQuery();

                return new Response
                {
                    Success = rows > 0,
                    Message = rows > 0 ? "Inventory item deleted" : "Item not found"
                };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = $"Error: {ex.Message}" };
            }
        }
    }
}