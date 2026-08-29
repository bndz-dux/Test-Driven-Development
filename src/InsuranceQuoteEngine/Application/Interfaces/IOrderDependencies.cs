using InsuranceQuoteEngine.Domain;

namespace InsuranceQuoteEngine.Application;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken = default);
}

public interface IPaymentService
{
    Task<PaymentResult> ProcessPaymentAsync(Guid customerId, decimal amount, CancellationToken cancellationToken = default);
}

public interface IOrderRepository
{
    Task SaveAsync(Order order, CancellationToken cancellationToken = default);
}
