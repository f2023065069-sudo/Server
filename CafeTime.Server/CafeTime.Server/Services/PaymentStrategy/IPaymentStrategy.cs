using System;
using System.Collections.Generic;
using System.Text;


using CafeTime.Server.Common.Models;



namespace CafeTime.Server.Services.PaymentStrategy
{
    public interface IPaymentStrategy
    {
        void Pay(Payment payment);
    }

}

