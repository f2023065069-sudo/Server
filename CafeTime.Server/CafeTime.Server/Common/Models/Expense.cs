using System;
using System.Collections.Generic;
using System.Text;

namespace CafeTime.Server.Common.Models
{
    public class Expense
    {
        public int Id { get; set; }
        public string Type { get; set; } // Maintenance / Salary
        public string Description { get; set; }
        public double Amount { get; set; }
        public DateTime Date { get; set; }
    }
}

