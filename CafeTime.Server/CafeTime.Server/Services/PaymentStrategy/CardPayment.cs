using CafeTime.Server.Common.Models;

using System;
using System.Collections.Generic;
using System.Text;

    namespace CafeTime.Server.Services.PaymentStrategy
    {
        public class CardPayment : IPaymentStrategy
        {
            public void Pay(Payment payment)
            {
                payment.TransactionId =
                    "CARD-" + Guid.NewGuid().ToString("N").Substring(0, 10);
            }
        }
    }



