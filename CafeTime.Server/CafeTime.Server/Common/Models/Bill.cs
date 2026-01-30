using System;
using System.Collections.Generic;
using System.Text;

namespace CafeTime.Server.Common.Models
{
    public class Bill
    {
        public int BillId { get; set; }
        public int OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Tax { get; set; }
        public decimal Discount { get; set; }
        public decimal FinalAmount { get; set; }
    }
}
