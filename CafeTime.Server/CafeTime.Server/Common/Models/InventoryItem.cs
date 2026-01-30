using System;
using System.Collections.Generic;
using System.Text;

namespace CafeTime.Server.Common.Models
{
    public class InventoryItem
    {
        public int InventoryId { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public int LowStockLevel { get; set; }
    }
}
