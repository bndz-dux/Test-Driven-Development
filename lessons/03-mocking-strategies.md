# Lesson 03: Chiến lược Mocking & Cô lập phụ thuộc với Moq (Mocking Strategies)

## 🎯 Mục tiêu bài học
- Hiểu rõ bản chất của **Test Doubles** và phân biệt: **Dummy**, **Stub**, **Mock**, **Spy**, **Fake**.
- Sử dụng thành thạo thư viện **Moq** trong .NET:
  - `Setup()` & `Returns()` / `ReturnsAsync()`
  - `Throws()` / `ThrowsAsync()`
  - `Verify()` với `Times.Once()`, `Times.Never()`
  - Khớp đối số linh hoạt với `It.IsAny<T>()`, `It.Is<T>(predicate)`
- Nhận diện và loại bỏ **Over-Mocking** (Mock quá đà làm hỏng giá trị của test).
- **Thực hành:** Xây dựng và viết bộ 15–20 Unit Tests cho `OrderService` với các kịch bản bất đồng bộ (async), ngoại lệ và kiểm tra tương tác.

---

## 📖 1. Tổng quan về Test Doubles

Khi kiểm thử một lớp (gọi là **SUT - System Under Test**), lớp đó thường phụ thuộc vào các dịch vụ bên ngoài (Database, Payment Gateway, Third-party API, Message Queue). Chúng ta không thể gọi các dịch vụ thật đó trong Unit Test vì:
- Quá chậm.
- Không ổn định (mất mạng, DB down).
- Gây tác dụng phụ (trừ tiền thật, gửi email thật, ghi bẩn database).

Để giải quyết, ta sử dụng **Test Doubles** (vật đóng thế):

```text
               ┌───────────────────────┐
               │      Test Double      │
               └──────────┬────────────┘
         ┌────────────────┼────────────────┬──────────────┐
         ▼                ▼                ▼              ▼
     ┌───────┐        ┌───────┐        ┌──────┐       ┌───────┐
     │ Dummy │        │ Stub  │        │ Mock │       │ Fake  │
     └───────┘        └───────┘        └──────┘       └───────┘
  (Chỉ truyền cho  (Trả về dữ liệu   (Kiểm tra xem    (Bản cài đặt
   đủ tham số,      cố định khi       method có được   nhẹ, ví dụ:
   không dùng)      được gọi)         gọi hay không)   InMemoryDB)
```

- **Stub (State Verification):** Cung cấp sẵn câu trả lời giả lập cho các cuộc gọi từ SUT.
- **Mock (Behavior / Interaction Verification):** Ghi nhận và cho phép kiểm tra xem SUT có gọi đúng method, đúng số lần, đúng tham số kỳ vọng hay không.

---

## ⚠️ 2. Cạm bẫy "Over-Mocking" và Quy tắc vàng

### ❌ Khi nào KHÔNG NÊN Mock?
1. **Domain Entities / Value Objects:** Tuyệt đối không mock `Customer`, `Order`, `Money`. Hãy new object thật!
2. **Pure Functions / Logic nội bộ:** Các hàm tính toán thuần túy không có I/O.
3. **Mọi class nội bộ:** Không nên mock từng class con bên trong module nếu chúng có thể chạy nhanh trên bộ nhớ.

### ✅ Khi nào NÊN Mock?
1. **Out-of-process Dependencies (I/O):**
   - Database Repositories (`ICustomerRepository`, `IOrderRepository`)
   - External APIs (`IPaymentGateway`, `ISmsService`, `IEmailSender`)
   - Message Brokers (`IEventBus`, `IMessageQueue`)
   - Đồng hồ hệ thống (`ITimeProvider`, `IClock`)

---

## 🛠️ 3. Thực hành Step-by-Step: Xây dựng & Kiểm thử `OrderService`

### Bài toán nghiệp vụ:
Xây dựng `OrderService` để xử lý đơn hàng:
1. Nhận vào `CreateOrderRequest(Guid CustomerId, decimal Amount)`.
2. Kiểm tra khách hàng trong `ICustomerRepository`:
   - Nếu không tìm thấy khách hàng → Ném `CustomerNotFoundException`.
   - Nếu tài khoản khách hàng bị khóa (`IsBlocked == true`) → Ném `CustomerBlockedException`.
3. Kiểm tra số tiền `Amount`:
   - Nếu `Amount <= 0` → Ném `ArgumentOutOfRangeException`.
4. Gọi `IPaymentService.ProcessPaymentAsync(customerId, amount)`:
   - Nếu thanh toán thất bại (`Success == false`) → Ném `PaymentFailedException`.
5. Tạo và lưu `Order` mới vào `IOrderRepository`.
6. Trả về mã đơn hàng `OrderId`.
7. **Quy tắc tương tác:** Nếu kiểm tra khách hàng thất bại, tuyệt đối **không** được gọi `IPaymentService` hay `IOrderRepository`.

---

### Bước 3.1: Tạo Domain Models, Exceptions & Interfaces

Tạo file `src/InsuranceQuoteEngine/Domain/OrderModels.cs`:

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

Tạo file `src/InsuranceQuoteEngine/Application/IOrderDependencies.cs`:

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

### Bước 3.2: Cài đặt `OrderService`

Tạo file `src/InsuranceQuoteEngine/Application/OrderService.cs`:

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
```

---

### Bước 3.3: Viết bộ Unit Tests hoàn chỉnh với Moq

Tạo file `tests/InsuranceQuoteEngine.UnitTests/Application/OrderServiceTests.cs`:

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
        // Khởi tạo SUT với các dependencies đã được mock
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

        // Verify: Đơn hàng phải được lưu đúng thông tin
        _orderRepoMock.Verify(x => x.SaveAsync(
            It.Is<Order>(o => o.CustomerId == customerId && o.Amount == 150m && o.Id == orderId),
            It.IsAny<CancellationToken>()), 
            Times.Once);

        // Verify: PaymentService chỉ được gọi duy nhất 1 lần
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
            .ReturnsAsync((Customer?)null); // Giả lập không tìm thấy

        // Act
        Func<Task> act = async () => await _sut.CreateOrderAsync(request);

        // Assert
        await act.Should().ThrowAsync<CustomerNotFoundException>()
            .WithMessage($"*{customerId}*");

        // Verify: Tuyệt đối không được thanh toán hay lưu đơn hàng khi khách không tồn tại!
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

        // Verify: Không được thanh toán
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

        // Verify: Không chạm tới bất kỳ repository nào
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

        // Verify: Tuyệt đối KHÔNG lưu đơn hàng vào DB khi thanh toán thất bại
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

## 📋 Checklist thực hành Mocking chuyên nghiệp
```text
[ ] Luôn khởi tạo mock bằng Interface (vd: Mock<ICustomerRepository>)
[ ] Dùng ReturnsAsync() cho phương thức bất đồng bộ (Task / ValueTask)
[ ] Dùng It.IsAny<T>() khi không quan tâm giá trị cụ thể
[ ] Dùng It.Is<T>(predicate) để kiểm tra sâu thuộc tính của argument
[ ] Luôn Verify(..., Times.Never) trong các kịch bản lỗi để đảm bảo không phát sinh tác dụng phụ
[ ] Không bao giờ mock Domain Entities (hãy new đối tượng thật)
```

---

## ✅ Check-list hoàn thành Lesson 03
- [ ] Phân biệt được Stub vs Mock vs Fake.
- [ ] Sử dụng thành thạo `Setup`, `ReturnsAsync`, `ThrowsAsync`, `Verify`, `Times.Once`, `Times.Never`.
- [ ] Hiểu rõ tác hại của Over-Mocking và biết cách cô lập tầng Application Service với I/O.
- [ ] Chạy thành công toàn bộ test cases cho `OrderService`.

👉 **Tiếp theo:** Chuyển sang [Lesson 04: Xây dựng Test Data Builders & Fixtures](./04-test-data-builders.md)!
