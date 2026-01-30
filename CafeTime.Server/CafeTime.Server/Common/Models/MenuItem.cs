using System;
using System.Collections.Generic;
using System.Text;

namespace CafeTime.Server.Common.Models
{
    public class MenuItem
    {
        public int MenuId { get; set; }
        public string ItemName { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
        public bool IsAvailable { get; set; }
        public decimal BuyingPrice { get; set; }
        public decimal SellingPrice { get; set; }
    }
}
