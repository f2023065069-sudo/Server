// Services/BillingService.cs
using CafeTime.Server.Common.DOTs;
using CafeTime.Server.Common.Models;
using MySql.Data.MySqlClient;
using System.Text.Json;

namespace CafeTime.Server.Services
{
    public static class BillingService
    {
        public class BillRequest
        {
            public int OrderId { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal Discount { get; set; }
        }

        // Services/BillingService.cs - Update GenerateBill method
        public static Response GenerateBill(BillRequest billRequest)
        {
            using var connection = DatabaseService.GetConnection();
            connection.Open();
            var transaction = connection.BeginTransaction();

            try
            {
                // First, get order total from database
                string orderQuery = @"SELECT 
                            SUM(oi.Quantity * oi.Price) as OrderTotal,
                            o.Status
                            FROM Orders o
                            JOIN OrderItems oi ON o.OrderId = oi.OrderId
                            WHERE o.OrderId = @OrderId
                            GROUP BY o.OrderId";

                using var orderCmd = new MySqlCommand(orderQuery, connection, transaction);
                orderCmd.Parameters.AddWithValue("@OrderId", billRequest.OrderId);

                using var reader = orderCmd.ExecuteReader();

                if (!reader.Read())
                {
                    transaction.Rollback();
                    return new Response { Success = false, Message = "Order not found" };
                }

                decimal orderTotal = reader.GetDecimal("OrderTotal");
                string orderStatus = reader.GetString("Status");
                reader.Close();

                // Check if order is ready for billing
                if (orderStatus != "Ready" && orderStatus != "Completed")
                {
                    transaction.Rollback();
                    return new Response
                    {
                        Success = false,
                        Message = $"Order status is '{orderStatus}'. Order must be 'Ready' for billing."
                    };
                }

                // Use calculated total from order, not from request
                decimal totalAmount = orderTotal;
                decimal discount = billRequest.Discount;

                // Calculate tax (10% of total after discount)
                decimal taxableAmount = totalAmount - discount;
                decimal tax = taxableAmount * 0.10m;
                decimal finalAmount = taxableAmount + tax;

                // Check if bill already exists
                string checkBillQuery = "SELECT COUNT(*) FROM Bills WHERE OrderId = @OrderId";
                using var checkCmd = new MySqlCommand(checkBillQuery, connection, transaction);
                checkCmd.Parameters.AddWithValue("@OrderId", billRequest.OrderId);
                int billCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (billCount > 0)
                {
                    transaction.Rollback();
                    return new Response { Success = false, Message = "Bill already exists for this order" };
                }

                // Insert bill
                string billQuery = @"INSERT INTO Bills (OrderId, TotalAmount, Tax, Discount, FinalAmount) 
                           VALUES (@OrderId, @TotalAmount, @Tax, @Discount, @FinalAmount);
                           SELECT LAST_INSERT_ID();";

                using var billCmd = new MySqlCommand(billQuery, connection, transaction);
                billCmd.Parameters.AddWithValue("@OrderId", billRequest.OrderId);
                billCmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
                billCmd.Parameters.AddWithValue("@Tax", tax);
                billCmd.Parameters.AddWithValue("@Discount", discount);
                billCmd.Parameters.AddWithValue("@FinalAmount", finalAmount);

                int billId = Convert.ToInt32(billCmd.ExecuteScalar());

                // Update order status to 'Completed'
                string updateOrder = "UPDATE Orders SET Status = 'Completed' WHERE OrderId = @OrderId";
                using var updateCmd = new MySqlCommand(updateOrder, connection, transaction);
                updateCmd.Parameters.AddWithValue("@OrderId", billRequest.OrderId);
                updateCmd.ExecuteNonQuery();

                // Get bill details
                var billDetails = new
                {
                    BillId = billId,
                    OrderId = billRequest.OrderId,
                    OrderTotal = totalAmount,
                    Discount = discount,
                    Tax = tax,
                    FinalAmount = finalAmount,
                    BillDate = DateTime.Now
                };

                transaction.Commit();

                return new Response
                {
                    Success = true,
                    Message = $"Bill #{billId} generated successfully\n" +
                             $"Order Total: ${totalAmount:F2}\n" +
                             $"Discount: ${discount:F2}\n" +
                             $"Tax (10%): ${tax:F2}\n" +
                             $"Final Amount: ${finalAmount:F2}",
                    Data = JsonSerializer.Serialize(billDetails)
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return new Response { Success = false, Message = $"Error generating bill: {ex.Message}" };
            }
        }

        public static Response MakePayment(Payment payment)
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                string query = @"INSERT INTO Payments (BillId, PaymentMethod, AmountPaid, TransactionId) 
                               VALUES (@BillId, @PaymentMethod, @AmountPaid, @TransactionId)";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@BillId", payment.BillId);
                cmd.Parameters.AddWithValue("@PaymentMethod", payment.PaymentMethod);
                cmd.Parameters.AddWithValue("@AmountPaid", payment.AmountPaid);
                cmd.Parameters.AddWithValue("@TransactionId",
                    string.IsNullOrEmpty(payment.TransactionId) ?
                    $"TRX{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}" :
                    payment.TransactionId);

                int rows = cmd.ExecuteNonQuery();

                return new Response
                {
                    Success = rows > 0,
                    Message = rows > 0 ? "Payment recorded successfully" : "Payment failed"
                };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = $"Error: {ex.Message}" };
            }
        }
        // In Services/BillingService.cs - Add this method
        public static Response GetBillDetails(int billId)
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                string query = @"SELECT 
                        b.BillId,
                        b.OrderId,
                        b.TotalAmount,
                        b.Tax,
                        b.Discount,
                        b.FinalAmount,
                        b.BillDate,
                        o.OrderType,
                        o.OrderDate,
                        u.Username as EmployeeName,
                        (SELECT COALESCE(SUM(AmountPaid), 0) FROM Payments WHERE BillId = b.BillId) as AmountPaidSoFar,
                        (b.FinalAmount - (SELECT COALESCE(SUM(AmountPaid), 0) FROM Payments WHERE BillId = b.BillId)) as BalanceDue
                        FROM Bills b
                        JOIN Orders o ON b.OrderId = o.OrderId
                        JOIN Users u ON o.EmployeeId = u.UserId
                        WHERE b.BillId = @BillId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@BillId", billId);

                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    var billDetails = new
                    {
                        BillId = reader.GetInt32("BillId"),
                        OrderId = reader.GetInt32("OrderId"),
                        TotalAmount = reader.GetDecimal("TotalAmount"),
                        Tax = reader.GetDecimal("Tax"),
                        Discount = reader.GetDecimal("Discount"),
                        FinalAmount = reader.GetDecimal("FinalAmount"),
                        BillDate = reader.GetDateTime("BillDate"),
                        OrderType = reader.GetString("OrderType"),
                        OrderDate = reader.GetDateTime("OrderDate"),
                        EmployeeName = reader.GetString("EmployeeName"),
                        AmountPaidSoFar = reader.GetDecimal("AmountPaidSoFar"),
                        BalanceDue = reader.GetDecimal("BalanceDue")
                    };

                    return new Response
                    {
                        Success = true,
                        Message = "Bill details retrieved successfully",
                        Data = JsonSerializer.Serialize(billDetails)
                    };
                }
                else
                {
                    return new Response
                    {
                        Success = false,
                        Message = $"Bill #{billId} not found"
                    };
                }
            }
            catch (Exception ex)
            {
                return new Response
                {
                    Success = false,
                    Message = $"Error retrieving bill details: {ex.Message}"
                };
            }
        }
        // In BillingService.cs - Add this method
        public static Response GetPaymentHistory(int billId)
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                string query = @"SELECT 
                        PaymentId,
                        PaymentMethod,
                        AmountPaid,
                        PaymentDate,
                        TransactionId
                        FROM Payments
                        WHERE BillId = @BillId
                        ORDER BY PaymentDate DESC";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@BillId", billId);

                using var reader = cmd.ExecuteReader();

                var payments = new List<object>();
                while (reader.Read())
                {
                    payments.Add(new
                    {
                        PaymentId = reader.GetInt32("PaymentId"),
                        PaymentMethod = reader.GetString("PaymentMethod"),
                        AmountPaid = reader.GetDecimal("AmountPaid"),
                        PaymentDate = reader.GetDateTime("PaymentDate"),
                        TransactionId = reader.GetString("TransactionId")
                    });
                }

                return new Response
                {
                    Success = true,
                    Message = payments.Count > 0 ? $"{payments.Count} payment(s) found" : "No payments found",
                    Data = JsonSerializer.Serialize(payments)
                };
            }
            catch (Exception ex)
            {
                return new Response
                {
                    Success = false,
                    Message = $"Error retrieving payment history: {ex.Message}"
                };
            }
        }


    }
}