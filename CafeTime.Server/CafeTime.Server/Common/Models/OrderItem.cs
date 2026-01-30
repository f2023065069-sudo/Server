using System;
using System.Collections.Generic;
using System.Text;

namespace CafeTime.Server.Common.Models
{
    public class OrderItem
    {
        public int MenuId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
