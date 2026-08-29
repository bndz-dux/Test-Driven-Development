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