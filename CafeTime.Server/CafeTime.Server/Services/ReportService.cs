// Services/ReportService.cs
using CafeTime.Server.Common.DOTs;
using MySql.Data.MySqlClient;
using System.Text;

namespace CafeTime.Server.Services
{
    public static class ReportService
    {
        public static Response GetDailySalesReport()
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                string query = @"SELECT 
                                DATE(BillDate) as SaleDate,
                                COUNT(*) as TotalBills,
                                SUM(FinalAmount) as TotalSales,
                                AVG(FinalAmount) as AverageBill
                                FROM Bills
                                WHERE DATE(BillDate) = CURDATE()
                                GROUP BY DATE(BillDate)";

                using var cmd = new MySqlCommand(query, connection);
                using var reader = cmd.ExecuteReader();

                var report = new StringBuilder();
                report.AppendLine("=== DAILY SALES REPORT ===");
                report.AppendLine($"Date: {DateTime.Today:yyyy-MM-dd}");
                report.AppendLine();

                if (reader.Read())
                {
                    report.AppendLine($"Total Bills: {reader["TotalBills"]}");
                    report.AppendLine($"Total Sales: ${reader["TotalSales"]:F2}");
                    report.AppendLine($"Average Bill: ${reader["AverageBill"]:F2}");
                }
                else
                {
                    report.AppendLine("No sales for today.");
                }

                return new Response
                {
                    Success = true,
                    Message = "Daily sales report generated",
                    Data = report.ToString()
                };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public static Response GetInventoryReport()
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                string query = @"SELECT 
                                ItemName,
                                Quantity,
                                LowStockLevel,
                                CASE WHEN Quantity <= LowStockLevel THEN 'LOW STOCK' 
                                     ELSE 'IN STOCK' END as Status
                                FROM Inventory
                                ORDER BY ItemName";

                using var cmd = new MySqlCommand(query, connection);
                using var reader = cmd.ExecuteReader();

                var report = new StringBuilder();
                report.AppendLine("=== INVENTORY REPORT ===");
                report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
                report.AppendLine();
                report.AppendLine("Item Name | Quantity | Low Level | Status");
                report.AppendLine(new string('-', 50));

                while (reader.Read())
                {
                    report.AppendLine($"{reader["ItemName"],-20} | {reader["Quantity"],8} | {reader["LowStockLevel"],10} | {reader["Status"]}");
                }

                return new Response
                {
                    Success = true,
                    Message = "Inventory report generated",
                    Data = report.ToString()
                };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public static Response GetLowStockReport()
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                string query = @"SELECT * FROM Inventory 
                                WHERE Quantity <= LowStockLevel
                                ORDER BY ItemName";

                using var cmd = new MySqlCommand(query, connection);
                using var reader = cmd.ExecuteReader();

                var report = new StringBuilder();
                report.AppendLine("=== LOW STOCK REPORT ===");
                report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
                report.AppendLine();

                if (!reader.HasRows)
                {
                    report.AppendLine("All items are sufficiently stocked.");
                }
                else
                {
                    report.AppendLine("ID | Item Name | Quantity | Low Level");
                    report.AppendLine(new string('-', 50));

                    while (reader.Read())
                    {
                        report.AppendLine($"{reader["InventoryId"],3} | {reader["ItemName"],-20} | {reader["Quantity"],8} | {reader["LowStockLevel"],10}");
                    }
                }

                return new Response
                {
                    Success = true,
                    Message = "Low stock report generated",
                    Data = report.ToString()
                };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = $"Error: {ex.Message}" };
            }
        }
    }
}