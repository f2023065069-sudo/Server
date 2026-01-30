using System;
using System.Collections.Generic;
using System.Text;

namespace CafeTime.Server.Common.Models
{
    public class Payment
    {
        public int BillId { get; set; }
        public string PaymentMethod { get; set; }
        public decimal AmountPaid { get; set; }
        public string TransactionId { get; set; }
    }
}
