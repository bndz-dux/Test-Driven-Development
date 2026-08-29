# Lesson 06: Kỹ thuật Unit Testing Nâng cao (Advanced Unit Testing)

## 🎯 Mục tiêu bài học
- Nắm vững kỹ thuật **Parameterized Testing (Kiểm thử tham số hóa)** trong xUnit:
  - `[Theory]` kết hợp `[InlineData]`
  - `[MemberData]` cho các tập dữ liệu phức tạp
  - `[ClassData]` để tách riêng bộ dữ liệu kiểm thử
- Giải quyết bài toán **Kiểm thử logic phụ thuộc vào Thời gian (Time-dependent Logic)**:
  - Loại bỏ `DateTime.UtcNow` trực tiếp trong Domain.
  - Sử dụng trừu tượng `IClock` hoặc `TimeProvider` (.NET 8+).
  - Kiểm thử thời hạn hết hạn báo giá (Quote Expiration) và ngày hiệu lực hợp đồng (Policy Effective Date).
- Kiểm thử bất đồng bộ (Async), Ngoại lệ sâu, và tính Bất biến/Idempotency.

---

## 📊 1. Parameterized Testing trong xUnit (`[Theory]`)

Khi bạn có cùng 1 logic kiểm thử nhưng muốn chạy với 10, 20 bộ dữ liệu đầu vào và kết quả mong đợi khác nhau:

### 1.1. Dùng `[InlineData]` cho dữ liệu nguyên thủy đơn giản
```csharp
[Theory]
[InlineData(100, 0.0, 100)]
[InlineData(100, 0.2, 80)]
[InlineData(100, 1.0, 0)]
[InlineData(200, 0.5, 100)]
public void Calculate_ShouldReturnExpectedPrice_ForVariousDiscounts(
    decimal price, 
    decimal discount, 
    decimal expected)
{
    var calculator = new PriceCalculator();
    var result = calculator.Calculate(price, discount);
    result.Should().Be(expected);
}
```

### 1.2. Dùng `[MemberData]` cho tập dữ liệu Object phức tạp
`[MemberData]` đọc dữ liệu từ một `public static` property hoặc method trả về `IEnumerable<object[]>`:

```csharp
public class QuoteTestData
{
    public static IEnumerable<object[]> GetDiscountScenarios()
    {
        yield return new object[] { CustomerMembership.Normal, 1000m, 0.0m };
        yield return new object[] { CustomerMembership.Premium, 1000m, 0.10m };
        yield return new object[] { CustomerMembership.Vip, 1000m, 0.20m };
        yield return new object[] { CustomerMembership.Normal, 6_000_000m, 0.05m }; // bonus 5%
    }
}

[Theory]
[MemberData(nameof(QuoteTestData.GetDiscountScenarios), MemberType = typeof(QuoteTestData))]
public void CalculateDiscount_ShouldMatchExpectedScenario(
    CustomerMembership membership, 
    decimal amount, 
    decimal expectedDiscount)
{
    var calculator = new DiscountCalculator();
    var result = calculator.CalculateDiscount(membership, amount);
    result.Should().Be(expectedDiscount);
}
```

### 1.3. Dùng `[ClassData]` để tách riêng dữ liệu ra file độc lập
```csharp
public class InvalidPostalCodesClassData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { null! };
        yield return new object[] { "" };
        yield return new object[] { "   " };
        yield return new object[] { "123" };
        yield return new object[] { "123456" };
        yield return new object[] { "7000A" };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

[Theory]
[ClassData(typeof(InvalidPostalCodesClassData))]
public void Validate_ShouldRejectInvalidPostalCodes(string invalidPostalCode)
{
    var validator = new CustomerEligibilityValidator();
    var applicant = new CustomerApplicant("John Doe", 30, invalidPostalCode);
    
    var result = validator.Validate(applicant);
    
    result.IsValid.Should().BeFalse();
}
```

---

## ⏰ 2. Kiểm thử logic phụ thuộc vào Thời gian (Time-dependent Logic)

### ❌ Sai lầm: Gọi `DateTime.UtcNow` trực tiếp trong Business Logic
Nếu trong code nghiệp vụ bạn viết:
```csharp
if (DateTime.UtcNow > quote.ExpiresAtUtc)
{
    throw new QuoteExpiredException();
}
```
Bài test của bạn sẽ trở thành **Flaky Test (chập chờn)** hoặc không thể nào test được tình huống "Giả sử bây giờ đã là 30 ngày sau".

### ✅ Giải pháp: Trừu tượng hóa thời gian qua `IClock` hoặc `TimeProvider`
Trong .NET 8+, Microsoft đã chuẩn hóa việc này bằng lớp trừu tượng `System.TimeProvider` và package `Microsoft.Extensions.TimeProvider.Testing`.

---

## 🛠️ 3. Thực hành Step-by-Step: Xây dựng `PolicyLifecycleManager` với `IClock`

### Yêu cầu nghiệp vụ:
Xây dựng lớp quản lý hiệu lực hợp đồng bảo hiểm `PolicyLifecycleManager`:
1. **Báo giá hết hạn (Quote Expiration):** Báo giá chỉ có giá trị trong vòng **30 ngày** kể từ ngày tạo.
2. **Kích hoạt hợp đồng (Activate Policy):**
   - Không được kích hoạt nếu báo giá đã hết hạn → Ném `QuoteExpiredException`.
   - Khi kích hoạt hợp đồng thành công:
     - Ngày bắt đầu hiệu lực `EffectiveDateUtc` = Thời điểm hiện tại.
     - Ngày kết thúc hợp đồng `ExpiryDateUtc` = Tròn 1 năm sau (`EffectiveDateUtc.AddYears(1)`).
3. **Hủy hợp đồng (Cancel Policy):**
   - Chỉ được hoàn tiền 100% nếu hủy trong vòng **14 ngày đầu (Cooling-off period)**.
   - Sau 14 ngày, tính phí phạt 20% trên số tiền còn lại.

---

### Bước 3.1: Định nghĩa `IClock` & Domain Models

Tạo file `src/InsuranceQuoteEngine/Domain/TimeAbstraction.cs`:

```csharp
namespace InsuranceQuoteEngine.Domain;

public interface IClock
{
    DateTime UtcNow { get; }
}

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
```

Tạo file `src/InsuranceQuoteEngine/Domain/PolicyLifecycle.cs`:

```csharp
namespace InsuranceQuoteEngine.Domain;

public record Policy(
    Guid Id, 
    Guid QuoteId, 
    DateTime EffectiveDateUtc, 
    DateTime ExpiryDateUtc, 
    decimal AnnualPremium, 
    bool IsActive);

public class QuoteExpiredException : Exception
{
    public QuoteExpiredException(Guid quoteId) 
        : base($"Quote '{quoteId}' has expired and cannot be activated.") { }
}

public class PolicyLifecycleManager
{
    private readonly IClock _clock;

    public PolicyLifecycleManager(IClock clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public Policy ActivateQuote(InsuranceQuote quote)
    {
        ArgumentNullException.ThrowIfNull(quote);

        var now = _clock.UtcNow;

        if (now > quote.ExpiresAtUtc)
        {
            throw new QuoteExpiredException(quote.Id);
        }

        return new Policy(
            Id: Guid.NewGuid(),
            QuoteId: quote.Id,
            EffectiveDateUtc: now,
            ExpiryDateUtc: now.AddYears(1),
            AnnualPremium: quote.FinalPremium,
            IsActive: true
        );
    }

    public decimal CalculateRefundOnCancellation(Policy policy, DateTime cancellationDateUtc)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (cancellationDateUtc < policy.EffectiveDateUtc)
        {
            throw new ArgumentException("Cancellation date cannot be before policy effective date.");
        }

        // Trong vòng 14 ngày (Cooling-off period): Hoàn 100%
        var daysActive = (cancellationDateUtc - policy.EffectiveDateUtc).TotalDays;
        if (daysActive <= 14)
        {
            return policy.AnnualPremium;
        }

        // Đã hết hạn hợp đồng
        if (cancellationDateUtc >= policy.ExpiryDateUtc)
        {
            return 0m;
        }

        // Sau 14 ngày: Tính theo tỷ lệ ngày chưa sử dụng trừ 20% phí quản lý
        var totalDays = (policy.ExpiryDateUtc - policy.EffectiveDateUtc).TotalDays;
        var unusedDays = (policy.ExpiryDateUtc - cancellationDateUtc).TotalDays;
        var unearnedPremium = policy.AnnualPremium * (decimal)(unusedDays / totalDays);

        // Phạt 20%
        var refund = unearnedPremium * 0.80m;
        return Math.Round(refund, 2);
    }
}
```

---

### Bước 3.2: Viết bộ Unit Tests hoàn toàn Deterministic (Không phụ thuộc vào thời gian thật)

Tạo file `tests/InsuranceQuoteEngine.UnitTests/Domain/PolicyLifecycleManagerTests.cs`:

```csharp
using FluentAssertions;
using InsuranceQuoteEngine.Domain;
using InsuranceQuoteEngine.UnitTests.Builders;
using Moq;
using Xunit;

namespace InsuranceQuoteEngine.UnitTests.Domain;

public class PolicyLifecycleManagerTests
{
    private readonly Mock<IClock> _clockMock = new();
    private readonly PolicyLifecycleManager _sut;

    public PolicyLifecycleManagerTests()
    {
        _sut = new PolicyLifecycleManager(_clockMock.Object);
    }

    [Fact]
    public void ActivateQuote_ShouldCreateValidPolicy_WhenQuoteIsNotExpired()
    {
        // Arrange: Đóng băng thời gian tại ngày 01/01/2026 10:00:00 UTC
        var fixedNow = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        _clockMock.Setup(c => c.UtcNow).Returns(fixedNow);

        var quote = new InsuranceQuoteBuilder()
            .WithFinalPremium(1200m)
            .Build(); // Hạn mặc định là +30 ngày (31/01/2026)

        // Act
        var policy = _sut.ActivateQuote(quote);

        // Assert
        policy.Should().NotBeNull();
        policy.EffectiveDateUtc.Should().Be(fixedNow);
        policy.ExpiryDateUtc.Should().Be(fixedNow.AddYears(1)); // 01/01/2027
        policy.AnnualPremium.Should().Be(1200m);
        policy.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ActivateQuote_ShouldThrowQuoteExpiredException_WhenCurrentTimeIsPastExpirationDate()
    {
        // Arrange: Thời điểm hiện tại là sau ngày hết hạn của báo giá
        var fixedNow = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        _clockMock.Setup(c => c.UtcNow).Returns(fixedNow);

        var quote = new InsuranceQuoteBuilder()
            .Expired() // Đã hết hạn trước đó
            .Build();

        // Act
        Action act = () => _sut.ActivateQuote(quote);

        // Assert
        act.Should().Throw<QuoteExpiredException>()
            .WithMessage($"*{quote.Id}*");
    }

    [Fact]
    public void CalculateRefund_ShouldReturnFullAmount_WhenCancelledWithin14DaysCoolingOffPeriod()
    {
        // Arrange
        var effectiveDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var policy = new Policy(
            Id: Guid.NewGuid(),
            QuoteId: Guid.NewGuid(),
            EffectiveDateUtc: effectiveDate,
            ExpiryDateUtc: effectiveDate.AddYears(1),
            AnnualPremium: 1000m,
            IsActive: true
        );

        var cancellationDate = effectiveDate.AddDays(10); // Ngày thứ 10 (<= 14 ngày)

        // Act
        var refund = _sut.CalculateRefundOnCancellation(policy, cancellationDate);

        // Assert
        refund.Should().Be(1000m); // Hoàn 100%
    }

    [Fact]
    public void CalculateRefund_ShouldApply20PercentPenalty_WhenCancelledAfter14Days()
    {
        // Arrange: Hợp đồng 365 ngày giá 1000, hủy vào ngày thứ 100
        var effectiveDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var expiryDate = effectiveDate.AddDays(365);
        var policy = new Policy(
            Id: Guid.NewGuid(),
            QuoteId: Guid.NewGuid(),
            EffectiveDateUtc: effectiveDate,
            ExpiryDateUtc: expiryDate,
            AnnualPremium: 1000m,
            IsActive: true
        );

        var cancellationDate = effectiveDate.AddDays(100); 
        // Còn 265 ngày chưa dùng => Unearned = 1000 * (265 / 365) = 726.027
        // Phạt 20% => Hoàn 80% của 726.027 = 580.82

        // Act
        var refund = _sut.CalculateRefundOnCancellation(policy, cancellationDate);

        // Assert
        refund.Should().Be(580.82m);
    }
}
```

---

## ✅ Check-list hoàn thành Lesson 06
- [ ] Sử dụng thành thạo `[Theory]`, `[InlineData]`, `[MemberData]`, `[ClassData]`.
- [ ] Không còn gọi `DateTime.UtcNow` trực tiếp trong Domain Logic.
- [ ] Làm chủ kỹ thuật đóng băng thời gian trong test với Mock `IClock`.
- [ ] Toàn bộ test suite chạy ổn định 100%, không bị ảnh hưởng bởi múi giờ hay thời điểm chạy test.

👉 **Tiếp theo:** Chuyển sang [Lesson 07: Dự án Capstone Thực Chiến – Insurance Quote Engine](./07-insurance-quote-engine-capstone.md)!
