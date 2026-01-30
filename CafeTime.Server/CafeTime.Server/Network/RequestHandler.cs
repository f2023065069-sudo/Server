// Network/RequestHandler.cs - ONLY FIX THE DELETE METHODS
using CafeTime.Server.Common.DOTs;
using CafeTime.Server.Common.Models;
using CafeTime.Server.Services;
using System.Text.Json;

namespace CafeTime.Server.Network
{
    public static class RequestHandler
    {
        public static Response HandleRequest(Request request)
        {
            try
            {
                return request.Action switch
                {
                    "LOGIN" => AuthService.Login(JsonSerializer.Deserialize<LoginRequest>(request.Data.ToString())),
                    "ADD_MENU" => MenuService.AddMenuItem(JsonSerializer.Deserialize<MenuItem>(request.Data.ToString())),
                    "GET_MENU" => MenuService.GetMenuItems(),
                    "UPDATE_MENU" => MenuService.UpdateMenuItem(JsonSerializer.Deserialize<MenuItem>(request.Data.ToString())),

                    // FIXED: Handle JsonElement for DELETE_MENU
                    "DELETE_MENU" => MenuService.DeleteMenuItem(GetIntFromData(request.Data)),
                    "ADD_INVENTORY" => InventoryService.AddInventoryItem(JsonSerializer.Deserialize<InventoryItem>(request.Data.ToString())),
                    "GET_INVENTORY" => InventoryService.GetInventoryItems(),
                    "UPDATE_INVENTORY" => InventoryService.UpdateInventoryItem(JsonSerializer.Deserialize<InventoryItem>(request.Data.ToString())),
                    "DELETE_INVENTORY" => InventoryService.DeleteInventoryItem(GetIntFromData(request.Data)),

                    "CREATE_ORDER" => OrderService.CreateOrder(JsonSerializer.Deserialize<Order>(request.Data.ToString())),
                    "GET_ORDER_TOTAL" => OrderService.GetOrderTotal(GetIntFromData(request.Data)),
                    "GET_BILL_DETAILS" => BillingService.GetBillDetails(GetIntFromData(request.Data)),
                    "GENERATE_BILL" => BillingService.GenerateBill(JsonSerializer.Deserialize<BillingService.BillRequest>(request.Data.ToString())),
                    "MAKE_PAYMENT" => BillingService.MakePayment(JsonSerializer.Deserialize<Payment>(request.Data.ToString())),
                    "GET_PAYMENT_HISTORY" => BillingService.GetPaymentHistory(GetIntFromData(request.Data)),

                    "DAILY_SALES" => ReportService.GetDailySalesReport(),
                    "INVENTORY_REPORT" => ReportService.GetInventoryReport(),
                    "LOW_STOCK_REPORT" => ReportService.GetLowStockReport(),

                    // In RequestHandler.cs - Add these lines to your existing switch statement
                    "ADD_EMPLOYEE" => EmployeeService.AddEmployee(JsonSerializer.Deserialize<User>(request.Data.ToString())),
                    "GET_EMPLOYEES" => EmployeeService.GetEmployees(),
                    "GET_EMPLOYEE" => EmployeeService.GetEmployee(GetIntFromData(request.Data)),
                    "UPDATE_EMPLOYEE" => EmployeeService.UpdateEmployee(JsonSerializer.Deserialize<User>(request.Data.ToString())),
                    "DELETE_EMPLOYEE" => EmployeeService.DeleteEmployee(GetIntFromData(request.Data)),
                    "RESET_PASSWORD" => EmployeeService.ResetPassword(request.Data.ToString()), // Simple string

                    _ => new Response { Success = false, Message = "Invalid action" }
                };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        // NEW METHOD: Handle JsonElement to int conversion
        private static int GetIntFromData(object data)
        {
            if (data is JsonElement jsonElement)
            {
                // Try to get the integer value from JsonElement
                if (jsonElement.ValueKind == JsonValueKind.Number)
                {
                    return jsonElement.GetInt32();
                }
                else if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    // If it's a string like "1", parse it
                    return int.Parse(jsonElement.GetString());
                }
                else
                {
                    // For other cases, try to deserialize
                    return JsonSerializer.Deserialize<int>(jsonElement.GetRawText());
                }
            }

            // Fallback to original Convert.ToInt32 for other types
            return Convert.ToInt32(data);
        }
    }
}