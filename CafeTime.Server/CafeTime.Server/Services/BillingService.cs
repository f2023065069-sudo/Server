// Services/BillingService.cs
using CafeTime.Server.Common.DOTs;
using CafeTime.Server.Services.PaymentStrategy;
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

        // =====================================================
        // GENERATE BILL
        // =====================================================
        public static Response GenerateBill(BillRequest billRequest)
        {
            using var connection = DatabaseService.GetConnection();
            connection.Open();
            var transaction = connection.BeginTransaction();

            try
            {
                string orderQuery = @"SELECT 
                        SUM(oi.Quantity * oi.Price) AS OrderTotal,
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

                if (orderStatus != "Ready" && orderStatus != "Completed")
                {
                    transaction.Rollback();
                    return new Response
                    {
                        Success = false,
                        Message = $"Order status is '{orderStatus}'. Order must be Ready."
                    };
                }

                decimal discount = billRequest.Discount;
                decimal taxableAmount = orderTotal - discount;
                decimal tax = taxableAmount * 0.10m;
                decimal finalAmount = taxableAmount + tax;

                string checkBillQuery = "SELECT COUNT(*) FROM Bills WHERE OrderId = @OrderId";
                using var checkCmd = new MySqlCommand(checkBillQuery, connection, transaction);
                checkCmd.Parameters.AddWithValue("@OrderId", billRequest.OrderId);

                if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                {
                    transaction.Rollback();
                    return new Response { Success = false, Message = "Bill already exists" };
                }

                string billQuery = @"INSERT INTO Bills 
                        (OrderId, TotalAmount, Tax, Discount, FinalAmount)
                        VALUES 
                        (@OrderId, @TotalAmount, @Tax, @Discount, @FinalAmount);
                        SELECT LAST_INSERT_ID();";

                using var billCmd = new MySqlCommand(billQuery, connection, transaction);
                billCmd.Parameters.AddWithValue("@OrderId", billRequest.OrderId);
                billCmd.Parameters.AddWithValue("@TotalAmount", orderTotal);
                billCmd.Parameters.AddWithValue("@Tax", tax);
                billCmd.Parameters.AddWithValue("@Discount", discount);
                billCmd.Parameters.AddWithValue("@FinalAmount", finalAmount);

                int billId = Convert.ToInt32(billCmd.ExecuteScalar());

                string updateOrder = "UPDATE Orders SET Status='Completed' WHERE OrderId=@OrderId";
                using var updateCmd = new MySqlCommand(updateOrder, connection, transaction);
                updateCmd.Parameters.AddWithValue("@OrderId", billRequest.OrderId);
                updateCmd.ExecuteNonQuery();

                transaction.Commit();

                return new Response
                {
                    Success = true,
                    Message = $"Bill #{billId} generated successfully",
                    Data = JsonSerializer.Serialize(new
                    {
                        BillId = billId,
                        OrderTotal = orderTotal,
                        Discount = discount,
                        Tax = tax,
                        FinalAmount = finalAmount
                    })
                };
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return new Response
                {
                    Success = false,
                    Message = $"Error generating bill: {ex.Message}"
                };
            }
        }

        // =====================================================
        // MAKE PAYMENT (STRATEGY PATTERN)
        // =====================================================
        public static Response MakePayment(Payment payment)
        {
            try
            {
                if (payment.AmountPaid <= 0)
                {
                    return new Response
                    {
                        Success = false,
                        Message = "Payment amount must be greater than zero"
                    };
                }

                // ✅ STRATEGY PATTERN
                PaymentContext context = new PaymentContext();
                context.SetStrategy(payment.PaymentMethod);
                context.Execute(payment);

                using var connection = DatabaseService.GetConnection();
                connection.Open();

                string query = @"INSERT INTO Payments
                        (BillId, PaymentMethod, AmountPaid, TransactionId)
                        VALUES
                        (@BillId, @PaymentMethod, @AmountPaid, @TransactionId)";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@BillId", payment.BillId);
                cmd.Parameters.AddWithValue("@PaymentMethod", payment.PaymentMethod);
                cmd.Parameters.AddWithValue("@AmountPaid", payment.AmountPaid);
                cmd.Parameters.AddWithValue("@TransactionId", payment.TransactionId);

                cmd.ExecuteNonQuery();

                return new Response
                {
                    Success = true,
                    Message = $"Payment successful via {payment.PaymentMethod}"
                };
            }
            catch (Exception ex)
            {
                return new Response
                {
                    Success = false,
                    Message = $"Payment failed: {ex.Message}"
                };
            }
        }

        // =====================================================
        // BILL DETAILS
        // =====================================================
        public static Response GetBillDetails(int billId)
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                string query = @"SELECT 
                        b.BillId, b.OrderId, b.TotalAmount, b.Tax,
                        b.Discount, b.FinalAmount, b.BillDate,
                        o.OrderType, o.OrderDate,
                        u.Username AS EmployeeName,
                        (SELECT COALESCE(SUM(AmountPaid),0) FROM Payments WHERE BillId=b.BillId) AS AmountPaidSoFar,
                        (b.FinalAmount - 
                         (SELECT COALESCE(SUM(AmountPaid),0) FROM Payments WHERE BillId=b.BillId)) AS BalanceDue
                        FROM Bills b
                        JOIN Orders o ON b.OrderId=o.OrderId
                        JOIN Users u ON o.EmployeeId=u.UserId
                        WHERE b.BillId=@BillId";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@BillId", billId);

                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                    return new Response { Success = false, Message = "Bill not found" };

                return new Response
                {
                    Success = true,
                    Message = "Bill details retrieved",
                    Data = JsonSerializer.Serialize(new
                    {
                        BillId = reader.GetInt32("BillId"),
                        FinalAmount = reader.GetDecimal("FinalAmount"),
                        Paid = reader.GetDecimal("AmountPaidSoFar"),
                        Balance = reader.GetDecimal("BalanceDue")
                    })
                };
            }
            catch (Exception ex)
            {
                return new Response
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // =====================================================
        // PAYMENT HISTORY
        // =====================================================
        public static Response GetPaymentHistory(int billId)
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                string query = @"SELECT PaymentMethod, AmountPaid, PaymentDate, TransactionId
                                 FROM Payments
                                 WHERE BillId=@BillId
                                 ORDER BY PaymentDate DESC";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@BillId", billId);

                using var reader = cmd.ExecuteReader();
                var list = new List<object>();

                while (reader.Read())
                {
                    list.Add(new
                    {
                        Method = reader.GetString("PaymentMethod"),
                        Amount = reader.GetDecimal("AmountPaid"),
                        Date = reader.GetDateTime("PaymentDate"),
                        Transaction = reader.GetString("TransactionId")
                    });
                }

                return new Response
                {
                    Success = true,
                    Message = $"{list.Count} payment(s) found",
                    Data = JsonSerializer.Serialize(list)
                };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = ex.Message };
            }
        }
    }
}
