using CafeTime.Server.Common.Models;

namespace CafeTime.Server.Services.PaymentStrategy
{
    public class PaymentContext
    {
        private IPaymentStrategy _strategy;

        public void SetStrategy(string paymentMethod)
        {
            switch (paymentMethod.ToLower())
            {
                case "cash":
                    _strategy = new CashPayment();
                    break;

                case "card":
                    _strategy = new CardPayment();
                    break;

                case "online":
                    _strategy = new OnlinePayment();
                    break;

                default:
                    throw new Exception("Invalid payment method");
            }
        }

        public void Execute(Payment payment)
        {
            _strategy.Pay(payment);
        }
    }
}
