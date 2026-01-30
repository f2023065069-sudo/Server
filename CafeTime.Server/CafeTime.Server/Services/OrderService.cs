// Services/OrderService.cs
using CafeTime.Server.Common.DOTs;
using CafeTime.Server.Common.Models;
using MySql.Data.MySqlClient;
using System.Text.Json;

namespace CafeTime.Server.Services
{
    public static class OrderService
    {
        // Services/OrderService.cs - Update CreateOrder method
        public static Response CreateOrder(Order order)
        {
            using var connection = DatabaseService.GetConnection();
            connection.Open();
            var transaction = connection.BeginTransaction();

            try
            {
                // Insert order
                string orderQuery = @"INSERT INTO Orders (OrderType, EmployeeId, Status) 
                            VALUES (@OrderType, @EmployeeId, @Status);
                            SELECT LAST_INSERT_ID();";

                using var orderCmd = new MySqlCommand(orderQuery, connection, transaction);
                orderCmd.Parameters.AddWithValue("@OrderType", order.OrderType);
                orderCmd.Parameters.AddWithValue("@EmployeeId", order.EmployeeId);
                orderCmd.Parameters.AddWithValue("@Status", "Pending");

                int orderId = Convert.ToInt32(orderCmd.ExecuteScalar());

                // Insert order items and calculate total
                decimal totalAmount = 0;
                foreach (var item in order.Items)
                {
                    // Get menu item price and availability
                    string priceQuery = "SELECT Price FROM Menu WHERE MenuId = @MenuId AND IsAvailable = TRUE";
                    using var priceCmd = new MySqlCommand(priceQuery, connection, transaction);
                    priceCmd.Parameters.AddWithValue("@MenuId", item.MenuId);
                    var priceResult = priceCmd.ExecuteScalar();

                    if (priceResult == null)
                    {
                        transaction.Rollback();
                        return new Response
                        {
                            Success = false,
                            Message = $"Menu item ID {item.MenuId} not found or unavailable"
                        };
                    }

                    var price = Convert.ToDecimal(priceResult);
                    item.Price = price; // Store the price in order item

                    // Insert order item
                    string itemQuery = @"INSERT INTO OrderItems (OrderId, MenuId, Quantity, Price) 
                               VALUES (@OrderId, @MenuId, @Quantity, @Price)";

                    using var itemCmd = new MySqlCommand(itemQuery, connection, transaction);
                    itemCmd.Parameters.AddWithValue("@OrderId", orderId);
                    itemCmd.Parameters.AddWithValue("@MenuId", item.MenuId);
                    itemCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                    itemCmd.Parameters.AddWithValue("@Price", price);

                    itemCmd.ExecuteNonQuery();
                    totalAmount += price * item.Quantity;
                }

                // Return order details including total amount
                var orderDetails = new
                {
                    OrderId = orderId,
                    TotalAmount = totalAmount,
                    ItemCount = order.Items.Count,
                    Status = "Pending"
                };

                transaction.Commit();

                return new Response
                {
                    Success = true,
                    Message = $"Order #{orderId} created successfully. Total: ${totalAmount:F2}",
                    Data = JsonSerializer.Serialize(orderDetails)
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return new Response { Success = false, Message = $"Order creation failed: {ex.Message}" };
            }
        }


        // In OrderService.cs - Add this method
        public static Response GetOrderTotal(int orderId)
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                string query = @"SELECT 
                        o.OrderId,
                        o.OrderType,
                        o.Status,
                        SUM(oi.Quantity * oi.Price) as TotalAmount,
                        COUNT(oi.OrderItemId) as ItemCount
                        FROM Orders o
                        JOIN OrderItems oi ON o.OrderId = oi.OrderId
                        WHERE o.OrderId = @OrderId
                        GROUP BY o.OrderId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@OrderId", orderId);

                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    var orderTotal = new
                    {
                        OrderId = reader.GetInt32("OrderId"),
                        OrderType = reader.GetString("OrderType"),
                        Status = reader.GetString("Status"),
                        TotalAmount = reader.GetDecimal("TotalAmount"),
                        ItemCount = reader.GetInt32("ItemCount")
                    };

                    return new Response
                    {
                        Success = true,
                        Message = "Order total calculated",
                        Data = JsonSerializer.Serialize(orderTotal)
                    };
                }

                return new Response { Success = false, Message = "Order not found" };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = $"Error: {ex.Message}" };
            }
        }
    }
}