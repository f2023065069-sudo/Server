using CafeTime.Server.Common.Models;

using System;
using System.Collections.Generic;
using System.Text;

namespace CafeTime.Server.Services.PaymentStrategy
{
   
    
        public class OnlinePayment : IPaymentStrategy
        {
            public void Pay(Payment payment)
            {
                payment.TransactionId =
                    "ONLINE-" + Guid.NewGuid().ToString("N").Substring(0, 12);
            }
        }
    }



