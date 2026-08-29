# Lesson 03: Mocking Strategies & Isolating Dependencies with Moq

## 🎯 Lesson Objectives
- Understand the taxonomy of **Test Doubles**: **Dummy**, **Stub**, **Mock**, **Spy**, and **Fake**.
- Master the **Moq** mocking library in .NET:
  - `Setup()` & `Returns()` / `ReturnsAsync()`
  - `Throws()` / `ThrowsAsync()`
  - `Verify()` with `Times.Once()`, `Times.Never()`
  - Flexible argument matching with `It.IsAny<T>()`, `It.Is<T>(predicate)`
- Identify and eliminate **Over-Mocking** (excessive mocking that undermines test value).
- **Hands-on:** Build and write 15–20 unit tests for `OrderService` covering asynchronous execution, exceptions, and interaction verification.

---

## 📖 1. Overview of Test Doubles

When testing a class (the **System Under Test - SUT**), it often depends on out-of-process services (Databases, Payment Gateways, Third-party APIs, Message Queues). Invoking real services in unit tests is problematic because:
- They are slow.
- They are non-deterministic (network timeouts, database unavailability).
- They produce unwanted side effects (charging real credit cards, sending emails, mutating shared persistent databases).

We resolve this using **Test Doubles**:

```text
               ┌───────────────────────┐
               │      Test Double      │
               └──────────┬────────────┘
         ┌────────────────┼────────────────┬──────────────┐
         ▼                ▼                ▼              ▼
     ┌───────┐        ┌───────┐        ┌──────┐       ┌───────┐
     │ Dummy │        │ Stub  │        │ Mock │       │ Fake  │
     └───────┘        └───────┘        └──────┘       └───────┘
  (Passed only to   (Returns fixed   (Verifies whether (Lightweight real
   satisfy method    data when        methods were      implementation, e.g.
   parameters)       invoked)         called as expected) InMemory DB)
```

- **Stub (State Verification):** Pre-programs indirect inputs to the SUT with fixed responses.
- **Mock (Behavior / Interaction Verification):** Records and asserts on observable interactions between the SUT and its collaborators (invocations, argument matching, call frequencies).

---

## ⚠️ 2. The Over-Mocking Trap & Best Practices

### ❌ When NOT to Mock:
1. **Domain Entities / Value Objects:** Never mock `Customer`, `Order`, or `Money`. Instantiate real objects.
2. **Pure Functions / Internal Algorithms:** Pure computation without I/O.
3. **Internal Helpers:** Avoid mocking private or internal classes within the same bounded context if they can execute fast in memory.

### ✅ When to Mock:
1. **Out-of-process Collaborators (I/O Boundaries):**
   - Database Repositories (`ICustomerRepository`, `IOrderRepository`)
   - External APIs (`IPaymentGateway`, `ISmsService`, `IEmailSender`)
   - Message Brokers (`IEventBus`, `IMessageQueue`)
   - System Clock (`ITimeProvider`, `IClock`)

---

## 🛠️ 3. Step-by-Step Exercise: Implementing & Testing `OrderService`

### Business Requirements:
Build an `OrderService` to process checkout orders:
1. Receives `CreateOrderRequest(Guid CustomerId, decimal Amount)`.
2. Verifies the customer via `ICustomerRepository`:
   - If customer not found → Throw `CustomerNotFoundException`.
   - If customer account is blocked (`IsBlocked == true`) → Throw `CustomerBlockedException`.
3. Validates order `Amount`:
   - If `Amount <= 0` → Throw `ArgumentOutOfRangeException`.
4. Calls `IPaymentService.ProcessPaymentAsync(customerId, amount)`:
   - If payment fails (`Success == false`) → Throw `PaymentFailedException`.
5. Instantiates and persists a new `Order` via `IOrderRepository`.
6. Returns the generated `OrderId`.
7. **Interaction Constraint:** If customer validation fails, the service must **never** call `IPaymentService` or `IOrderRepository`.

---

### Step 3.1: Create Domain Models, Exceptions & Interfaces

Create file `src/InsuranceQuoteEngine/Domain/OrderModels.cs`:

```csharp
namespace InsuranceQuoteEngine.Domain;

public record Customer(Guid Id, string FullName, bool IsBlocked);

public record Order(Guid Id, Guid CustomerId, decimal Amount, DateTime CreatedAtUtc);

public record CreateOrderRequest(Guid CustomerId, decimal Amount);

public record PaymentResult(bool Success, string TransactionId, string? ErrorMessage);

public class CustomerNotFoundException : Exception
{
    public CustomerNotFoundException(Guid customerId) 
        : base($"Customer with ID '{customerId}' was not found.") { }
}

public class CustomerBlockedException : Exception
{
    public CustomerBlockedException(Guid customerId) 
        : base($"Customer with ID '{customerId}' is blocked from making purchases.") { }
}

public class PaymentFailedException : Exception
{
    public PaymentFailedException(string reason) 
        : base($"Payment processing failed: {reason}") { }
}
```

Create file `src/InsuranceQuoteEngine/Application/IOrderDependencies.cs`:

```csharp
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
```

---

### Step 3.2: Implement `OrderService`

Create file `src/InsuranceQuoteEngine/Application/Services/OrderService.cs`:

```csharp
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

        // 1. Verify customer
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            throw new CustomerNotFoundException(request.CustomerId);
        }

        if (customer.IsBlocked)
        {
            throw new CustomerBlockedException(request.CustomerId);
        }

        // 2. Process payment
        var paymentResult = await _paymentService.ProcessPaymentAsync(request.CustomerId, request.Amount, cancellationToken);
        if (!paymentResult.Success)
        {
            throw new PaymentFailedException(paymentResult.ErrorMessage ?? "Unknown payment error.");
        }

        // 3. Persist order
        var order = new Order(Guid.NewGuid(), customer.Id, request.Amount, DateTime.UtcNow);
        await _orderRepository.SaveAsync(order, cancellationToken);

        return order.Id;
    }
}
```

---

### Step 3.3: Write the Complete Test Suite with Moq

Create file `tests/InsuranceQuoteEngine.UnitTests/Application/OrderServiceTests.cs`:

```csharp
using FluentAssertions;
using InsuranceQuoteEngine.Application;
using InsuranceQuoteEngine.Domain;
using Moq;
using Xunit;

namespace InsuranceQuoteEngine.UnitTests.Application;

public class OrderServiceTests
{
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IPaymentService> _paymentServiceMock = new();
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        // Initialize SUT with mocked dependencies
        _sut = new OrderService(
            _customerRepoMock.Object,
            _paymentServiceMock.Object,
            _orderRepoMock.Object);
    }

    #region Happy Path Tests

    [Fact]
    public async Task CreateOrderAsync_ShouldCreateAndSaveOrder_WhenAllInputsAndServicesSucceed()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var request = new CreateOrderRequest(customerId, 150m);
        var activeCustomer = new Customer(customerId, "Nguyen Van A", IsBlocked: false);

        _customerRepoMock
            .Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeCustomer);

        _paymentServiceMock
            .Setup(x => x.ProcessPaymentAsync(customerId, 150m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentResult(Success: true, TransactionId: "TX-12345", ErrorMessage: null));

        // Act
        var orderId = await _sut.CreateOrderAsync(request);

        // Assert
        orderId.Should().NotBeEmpty();

        // Verify: Order must be persisted with matching details
        _orderRepoMock.Verify(x => x.SaveAsync(
            It.Is<Order>(o => o.CustomerId == customerId && o.Amount == 150m && o.Id == orderId),
            It.IsAny<CancellationToken>()), 
            Times.Once);

        // Verify: PaymentService is called exactly once
        _paymentServiceMock.Verify(x => x.ProcessPaymentAsync(customerId, 150m, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Failure Paths & Interaction Verifications

    [Fact]
    public async Task CreateOrderAsync_ShouldThrowCustomerNotFoundException_WhenCustomerDoesNotExist()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var request = new CreateOrderRequest(customerId, 100m);

        _customerRepoMock
            .Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null); // Simulate customer not found

        // Act
        Func<Task> act = async () => await _sut.CreateOrderAsync(request);

        // Assert
        await act.Should().ThrowAsync<CustomerNotFoundException>()
            .WithMessage($"*{customerId}*");

        // Verify: Never attempt payment or order saving when customer does not exist
        _paymentServiceMock.Verify(x => x.ProcessPaymentAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);
        _orderRepoMock.Verify(x => x.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldThrowCustomerBlockedException_WhenCustomerIsBlocked()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var request = new CreateOrderRequest(customerId, 200m);
        var blockedCustomer = new Customer(customerId, "Bad Guy", IsBlocked: true);

        _customerRepoMock
            .Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blockedCustomer);

        // Act
        Func<Task> act = async () => await _sut.CreateOrderAsync(request);

        // Assert
        await act.Should().ThrowAsync<CustomerBlockedException>();

        // Verify: Never attempt payment or order saving when customer is blocked
        _paymentServiceMock.Verify(x => x.ProcessPaymentAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);
        _orderRepoMock.Verify(x => x.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public async Task CreateOrderAsync_ShouldThrowArgumentOutOfRangeException_WhenAmountIsZeroOrNegative(decimal invalidAmount)
    {
        // Arrange
        var request = new CreateOrderRequest(Guid.NewGuid(), invalidAmount);

        // Act
        Func<Task> act = async () => await _sut.CreateOrderAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();

        // Verify: Guard clause triggers before calling any repository
        _customerRepoMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldThrowPaymentFailedException_WhenPaymentServiceFails()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var request = new CreateOrderRequest(customerId, 100m);
        var activeCustomer = new Customer(customerId, "Nguyen Van A", IsBlocked: false);

        _customerRepoMock
            .Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeCustomer);

        _paymentServiceMock
            .Setup(x => x.ProcessPaymentAsync(customerId, 100m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentResult(Success: false, TransactionId: "", ErrorMessage: "Insufficient funds"));

        // Act
        Func<Task> act = async () => await _sut.CreateOrderAsync(request);

        // Assert
        await act.Should().ThrowAsync<PaymentFailedException>()
            .WithMessage("*Insufficient funds*");

        // Verify: NEVER persist order to DB when payment fails
        _orderRepoMock.Verify(x => x.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldBubbleException_WhenRepositoryThrowsException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var request = new CreateOrderRequest(customerId, 100m);

        _customerRepoMock
            .Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Database connection timed out."));

        // Act
        Func<Task> act = async () => await _sut.CreateOrderAsync(request);

        // Assert
        await act.Should().ThrowAsync<TimeoutException>()
            .WithMessage("Database connection timed out.");
    }

    #endregion
}
```

---

## 📋 Professional Mocking Checklist
```text
[ ] Always mock interfaces rather than concrete classes (e.g. Mock<ICustomerRepository>)
[ ] Use ReturnsAsync() for asynchronous methods returning Task / ValueTask
[ ] Use It.IsAny<T>() when specific argument values do not affect the test outcome
[ ] Use It.Is<T>(predicate) for targeted assertions on argument properties
[ ] Always use Verify(..., Times.Never) in error scenarios to ensure no unwanted side-effects occur
[ ] Never mock domain entities (instantiate real objects directly)
```

---

## ✅ Lesson 03 Completion Checklist
- [ ] Differentiate between Stubs, Mocks, and Fakes.
- [ ] Mastered `Setup`, `ReturnsAsync`, `ThrowsAsync`, `Verify`, `Times.Once`, and `Times.Never`.
- [ ] Understand how to avoid over-mocking while isolating application services from I/O.
- [ ] Executed and passed the entire test suite for `OrderService`.

👉 **Next Step:** Proceed to [Lesson 04: Test Data Builders & Fixtures](./04-test-data-builders.md)!
