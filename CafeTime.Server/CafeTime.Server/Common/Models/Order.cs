using System;
using System.Collections.Generic;
using System.Text;

namespace CafeTime.Server.Common.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public string OrderType { get; set; }
        public int EmployeeId { get; set; }
        public string Status { get; set; }
        public List<OrderItem> Items { get; set; }
    }
}
