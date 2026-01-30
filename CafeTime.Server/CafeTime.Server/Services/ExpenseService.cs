using CafeTime.Server.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CafeTime.Server.Services
{
    public static class ExpenseService
    {
        private static List<Expense> expenses = new();

        public static void AddExpense(Expense expense)
        {
            expenses.Add(expense);
        }

        public static decimal GetTotalExpense()
        {
            return (decimal)expenses.Sum(e => e.Amount);
        }
    }

}
