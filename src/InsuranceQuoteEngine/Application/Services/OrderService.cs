using InsuranceQuoteEngine.Domain;

namespace InsuranceQuoteEngine.Application;

public class OrderService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IPaymentService _paymentService;
    private readonly IOrderRepository _orderRepository;

    public OrderService(
        ICustomerRepository customerRepository,
        IPaymentService paymentService,
        IOrderRepository orderRepository)
    {
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
    }

    public async Task<Guid> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Amount), "Order amount must be greater than zero.");
        }

        // 1. Kiểm tra khách hàng
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            throw new CustomerNotFoundException(request.CustomerId);
        }

        if (customer.IsBlocked)
        {
            throw new CustomerBlockedException(request.CustomerId);
        }

        // 2. Xử lý thanh toán
        var paymentResult = await _paymentService.ProcessPaymentAsync(request.CustomerId, request.Amount, cancellationToken);
        if (!paymentResult.Success)
        {
            throw new PaymentFailedException(paymentResult.ErrorMessage ?? "Unknown payment error.");
        }

        // 3. Lưu đơn hàng
        var order = new Order(Guid.NewGuid(), customer.Id, request.Amount, DateTime.UtcNow);
        await _orderRepository.SaveAsync(order, cancellationToken);

        return order.Id;
    }
}
