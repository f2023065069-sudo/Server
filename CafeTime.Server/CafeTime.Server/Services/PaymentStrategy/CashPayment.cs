using CafeTime.Server.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CafeTime.Server.Services.PaymentStrategy
{
    public class CashPayment : IPaymentStrategy
    {
        public void Pay(Payment payment)
        {
            payment.TransactionId =
                 "CASH-" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }
    }

}
