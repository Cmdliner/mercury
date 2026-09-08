using Mercury.Payments.Entities;

namespace Mercury.Payments;

public class PaymentCollectorFactory(IEnumerable<IPaymentCollector> collectors)
{
    public IPaymentCollector GetCollector(PaymentProvider provider) =>
        collectors.FirstOrDefault(c => c.Provider == provider) ??
        throw new ArgumentException($"No collector registered for provider {provider}");
}