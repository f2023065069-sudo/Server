using CafeTime.Server.Common.DOTs;
using MySql.Data.MySqlClient;
using System.Text;

namespace CafeTime.Server.Services
{
    public static class ReportService
    {
        // ================= DAILY SALES =================

        public static Response GetDailySalesReport()
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                string query = @"
                    SELECT 
                        COUNT(*) AS TotalBills,
                        SUM(FinalAmount) AS TotalSales,
                        AVG(FinalAmount) AS AverageBill
                    FROM Bills
                    WHERE DATE(BillDate) = CURDATE()";

                using var cmd = new MySqlCommand(query, connection);
                using var reader = cmd.ExecuteReader();

                var report = new StringBuilder();
                report.AppendLine("===== DAILY SALES REPORT =====");
                report.AppendLine($"Date: {DateTime.Today:yyyy-MM-dd}");
                report.AppendLine();

                if (reader.Read() && reader["TotalSales"] != DBNull.Value)
                {
                    report.AppendLine($"Total Bills : {reader["TotalBills"]}");
                    report.AppendLine($"Total Sales : {reader["TotalSales"]}");
                    report.AppendLine($"Average Bill: {reader["AverageBill"]}");
                }
                else
                {
                    report.AppendLine("No sales today.");
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
                return new Response { Success = false, Message = ex.Message };
            }
        }

        // ================= EXPENSE REPORT =================

        public static Response GetExpenseReport()
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                string query = @"SELECT SUM(Amount) FROM Expenses";

                using var cmd = new MySqlCommand(query, connection);
                var totalExpense = cmd.ExecuteScalar();

                double expense = totalExpense == DBNull.Value ? 0 : Convert.ToDouble(totalExpense);

                return new Response
                {
                    Success = true,
                    Message = "Expense report generated",
                    Data = $"Total Expenses: {expense}"
                };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = ex.Message };
            }
        }

        // ================= PROFIT / LOSS =================
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
        

        public static Response GetProfitLossReport()
        {
            try
            {
                using var connection = DatabaseService.GetConnection();
                connection.Open();

                double totalSales = 0;
                double totalExpenses = 0;

                var salesCmd = new MySqlCommand("SELECT SUM(FinalAmount) FROM Bills", connection);
                var salesResult = salesCmd.ExecuteScalar();
                if (salesResult != DBNull.Value)
                    totalSales = Convert.ToDouble(salesResult);

                var expenseCmd = new MySqlCommand("SELECT SUM(Amount) FROM Expenses", connection);
                var expenseResult = expenseCmd.ExecuteScalar();
                if (expenseResult != DBNull.Value)
                    totalExpenses = Convert.ToDouble(expenseResult);

                double profit = totalSales - totalExpenses;

                string status = profit >= 0 ? "PROFIT" : "LOSS";

                return new Response
                {
                    Success = true,
                    Message = "Profit/Loss calculated",
                    Data =
                        $"Total Sales   : {totalSales}\n" +
                        $"Total Expenses: {totalExpenses}\n" +
                        $"Result        : {status}\n" +
                        $"Amount        : {profit}"
                };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = ex.Message };
            }
        }
    }
}
