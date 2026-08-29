# Lesson 07: Dự án Capstone Thực Chiến – Insurance Quote Engine

## 🎯 Mục tiêu bài học
- Tổng hợp toàn bộ kiến thức từ Lesson 01 đến Lesson 06 vào một **dự án Domain thực tế**.
- Áp dụng phương pháp luận **TDD (Test-Driven Development)** để xây dựng engine tính phí bảo hiểm (`InsuranceQuoteEngine`) từ yêu cầu nghiệp vụ.
- Thiết kế luồng xử lý hoàn chỉnh theo kiến trúc Domain-Driven:
  - Xác thực hợp lệ (Eligibility Validation)
  - Đánh giá rủi ro (Risk Assessment)
  - Tính phí bảo hiểm gốc (Base Premium Calculation)
  - Áp dụng gói bảo hiểm (Coverage Multipliers)
  - Áp dụng chính sách giảm giá (Customer Discounts)
  - Kiểm tra điều kiện chuyển duyệt thủ công (Referral) hoặc từ chối (Decline)
- Xuất bản bản báo giá bảo hiểm (`InsuranceQuoteResult`).

---

## 🏛️ 1. Kiến trúc luồng xử lý (Quote Processing Pipeline)

```text
               ┌──────────────────────────────────────────────┐
               │         QuoteRequest (Customer + Property)   │
               └──────────────────────┬───────────────────────┘
                                      │
                                      ▼
                      [1. Eligibility & Address Check]
                                      │
                         Passed? ─────┴──── No ───► [ DECLINE QUOTE ]
                                      │
                                  Yes │
                                      ▼
                           [2. Risk Assessment]
                                      │
                         Flood/Old? ──┴──── Yes ──► [ REFER TO UNDERWRITER ]
                                      │
                                   No │
                                      ▼
                      [3. Base Premium Calculation]
                        (Property Value * Risk Factor)
                                      │
                                      ▼
                        [4. Coverage Multiplier]
                      (Basic: 1.0x, Standard: 1.25x, Premium: 1.6x)
                                      │
                                      ▼
                         [5. Customer Discounts]
                        (VIP: -20%, Premium: -10%)
                                      │
                                      ▼
                        [6. Final Quote Generated]
```

---

## 📜 2. Bảng quy tắc nghiệp vụ chi tiết (Business Rules)

| STT | Quy tắc | Mô tả chi tiết | Kết quả |
| :--- | :--- | :--- | :--- |
| **BR-01** | Độ tuổi khách hàng | Tuổi < 18 hoặc > 75 | **DECLINE** (Từ chối) với lý do: *"Customer age is ineligible for insurance."* |
| **BR-02** | Khu vực lũ lụt | Bất động sản nằm trong khu vực nguy cơ ngập lũ (`IsInFloodZone = true`) | **REFER** (Chuyển chuyên viên duyệt) |
| **BR-03** | Nhà quá cũ | Năm xây dựng trước 1950 (`YearBuilt < 1950`) | **REFER** (Chuyển chuyên viên duyệt) |
| **BR-04** | Giá trị nhà tối thiểu | Giá trị ước tính (`EstimatedValue`) < 100,000,000 VND | **DECLINE** với lý do: *"Property value is below insurable limit."* |
| **BR-05** | Phí cơ sở (Base Rate) | `BasePremium = EstimatedValue * 0.001` (0.1% giá trị nhà) | Số tiền cơ sở |
| **BR-06** | Gói bảo hiểm (Coverage) | - **Basic:** Nhân hệ số `1.0`<br>- **Standard:** Nhân hệ số `1.25`<br>- **Comprehensive:** Nhân hệ số `1.60` | Phí theo gói |
| **BR-07** | Chiết khấu khách hàng | - **Normal:** 0%<br>- **Premium:** Giảm 10%<br>- **VIP:** Giảm 20% | Phí cuối cùng (`FinalPremium`) |
| **BR-08** | Lịch sử bồi thường | Nếu khách hàng có yêu cầu bồi thường trong quá khứ (`HasPastClaims = true`) thì **không được áp dụng giảm giá** | Giữ nguyên phí |

---

## 🛠️ 3. Thực hành TDD Step-by-Step

Chúng ta sẽ tạo các Models và viết Engine từng bước theo TDD.

### Bước 3.1: Tạo Domain Enums & Models

Tạo file `src/InsuranceQuoteEngine/Domain/QuoteDomainModels.cs`:

```csharp
namespace InsuranceQuoteEngine.Domain;

public enum CoverageTier
{
    Basic,
    Standard,
    Comprehensive
}

public enum QuoteStatus
{
    Approved,
    Referred,
    Declined
}

public record GenerateQuoteRequest(
    CustomerProfile Customer,
    PropertyDetails Property,
    CoverageTier Coverage);

public record QuoteResult(
    Guid QuoteId,
    QuoteStatus Status,
    decimal BasePremium,
    decimal FinalPremium,
    string? DecisionReason,
    DateTime GeneratedAtUtc,
    DateTime ExpiresAtUtc);
```

---

### Bước 3.2: Viết Test Suite bằng TDD (Từng kịch bản nghiệp vụ)

Tạo file `tests/InsuranceQuoteEngine.UnitTests/Domain/InsuranceQuoteEngineTests.cs`:

```csharp
using FluentAssertions;
using InsuranceQuoteEngine.Domain;
using InsuranceQuoteEngine.UnitTests.Builders;
using Moq;
using Xunit;

namespace InsuranceQuoteEngine.UnitTests.Domain;

public class InsuranceQuoteEngineTests
{
    private readonly Mock<IClock> _clockMock = new();
    private readonly InsuranceQuoteEngineService _sut;
    private readonly DateTime _fixedNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public InsuranceQuoteEngineTests()
    {
        _clockMock.Setup(c => c.UtcNow).Returns(_fixedNow);
        _sut = new InsuranceQuoteEngineService(_clockMock.Object);
    }

    #region BR-01 & BR-04: Decline Rules

    [Theory]
    [InlineData(17)]
    [InlineData(76)]
    public void GenerateQuote_ShouldDecline_WhenCustomerAgeIsOutsidePermittedRange(int invalidAge)
    {
        // Arrange
        var customer = new CustomerProfileBuilder().WithAge(invalidAge).Build();
        var property = new PropertyDetailsBuilder().Build();
        var request = new GenerateQuoteRequest(customer, property, CoverageTier.Basic);

        // Act
        var result = _sut.GenerateQuote(request);

        // Assert
        result.Status.Should().Be(QuoteStatus.Declined);
        result.DecisionReason.Should().Contain("Customer age is ineligible for insurance.");
        result.FinalPremium.Should().Be(0m);
    }

    [Fact]
    public void GenerateQuote_ShouldDecline_WhenPropertyValueIsBelowMinimumThreshold()
    {
        // Arrange
        var customer = new CustomerProfileBuilder().WithAge(30).Build();
        var property = new PropertyDetailsBuilder().WithEstimatedValue(50_000_000m).Build(); // < 100tr
        var request = new GenerateQuoteRequest(customer, property, CoverageTier.Basic);

        // Act
        var result = _sut.GenerateQuote(request);

        // Assert
        result.Status.Should().Be(QuoteStatus.Declined);
        result.DecisionReason.Should().Contain("Property value is below insurable limit.");
        result.FinalPremium.Should().Be(0m);
    }

    #endregion

    #region BR-02 & BR-03: Referral Rules

    [Fact]
    public void GenerateQuote_ShouldRefer_WhenPropertyIsInFloodZone()
    {
        // Arrange
        var customer = new CustomerProfileBuilder().WithAge(30).Build();
        var property = new PropertyDetailsBuilder()
            .WithEstimatedValue(1_000_000_000m)
            .InFloodZone(true)
            .Build();
        var request = new GenerateQuoteRequest(customer, property, CoverageTier.Basic);

        // Act
        var result = _sut.GenerateQuote(request);

        // Assert
        result.Status.Should().Be(QuoteStatus.Referred);
        result.DecisionReason.Should().Contain("Property is in a designated flood hazard zone.");
    }

    [Fact]
    public void GenerateQuote_ShouldRefer_WhenPropertyWasBuiltBefore1950()
    {
        // Arrange
        var customer = new CustomerProfileBuilder().WithAge(30).Build();
        var property = new PropertyDetailsBuilder()
            .WithEstimatedValue(1_000_000_000m)
            .WithYearBuilt(1948)
            .Build();
        var request = new GenerateQuoteRequest(customer, property, CoverageTier.Basic);

        // Act
        var result = _sut.GenerateQuote(request);

        // Assert
        result.Status.Should().Be(QuoteStatus.Referred);
        result.DecisionReason.Should().Contain("Property built prior to 1950 requires structural underwriting.");
    }

    #endregion

    #region BR-05, BR-06, BR-07: Premium Calculations & Coverage Multipliers

    [Fact]
    public void GenerateQuote_ShouldCalculateCorrectPremium_ForStandardCustomerWithBasicCoverage()
    {
        // Arrange: Nhà 1,000,000,000 -> Base rate 0.1% = 1,000,000. Basic tier = 1.0x. Không giảm giá.
        var customer = new CustomerProfileBuilder().WithType(CustomerType.Standard).Build();
        var property = new PropertyDetailsBuilder().WithEstimatedValue(1_000_000_000m).WithYearBuilt(2015).Build();
        var request = new GenerateQuoteRequest(customer, property, CoverageTier.Basic);

        // Act
        var result = _sut.GenerateQuote(request);

        // Assert
        result.Status.Should().Be(QuoteStatus.Approved);
        result.BasePremium.Should().Be(1_000_000m);
        result.FinalPremium.Should().Be(1_000_000m);
        result.ExpiresAtUtc.Should().Be(_fixedNow.AddDays(30));
    }

    [Fact]
    public void GenerateQuote_ShouldApplyStandardCoverageMultiplier_Of125Percent()
    {
        // Arrange: Nhà 1 tỷ -> Base 1,000,000. Standard tier = 1.25x => 1,250,000.
        var customer = new CustomerProfileBuilder().WithType(CustomerType.Standard).Build();
        var property = new PropertyDetailsBuilder().WithEstimatedValue(1_000_000_000m).WithYearBuilt(2015).Build();
        var request = new GenerateQuoteRequest(customer, property, CoverageTier.Standard);

        // Act
        var result = _sut.GenerateQuote(request);

        // Assert
        result.Status.Should().Be(QuoteStatus.Approved);
        result.FinalPremium.Should().Be(1_250_000m);
    }

    [Fact]
    public void GenerateQuote_ShouldApplyComprehensiveCoverageMultiplier_Of160Percent()
    {
        // Arrange: Nhà 1 tỷ -> Base 1,000,000. Comprehensive tier = 1.60x => 1,600,000.
        var customer = new CustomerProfileBuilder().WithType(CustomerType.Standard).Build();
        var property = new PropertyDetailsBuilder().WithEstimatedValue(1_000_000_000m).WithYearBuilt(2015).Build();
        var request = new GenerateQuoteRequest(customer, property, CoverageTier.Comprehensive);

        // Act
        var result = _sut.GenerateQuote(request);

        // Assert
        result.Status.Should().Be(QuoteStatus.Approved);
        result.FinalPremium.Should().Be(1_600_000m);
    }

    [Fact]
    public void GenerateQuote_ShouldApplyVipDiscount_Of20Percent()
    {
        // Arrange: Nhà 1 tỷ, gói Comprehensive (1,600,000). Khách VIP được giảm 20% => còn 1,280,000.
        var customer = new CustomerProfileBuilder().AsVip().Build();
        var property = new PropertyDetailsBuilder().WithEstimatedValue(1_000_000_000m).WithYearBuilt(2015).Build();
        var request = new GenerateQuoteRequest(customer, property, CoverageTier.Comprehensive);

        // Act
        var result = _sut.GenerateQuote(request);

        // Assert
        result.Status.Should().Be(QuoteStatus.Approved);
        result.FinalPremium.Should().Be(1_280_000m);
    }

    [Fact]
    public void GenerateQuote_ShouldNotApplyDiscount_WhenCustomerHasPastClaims()
    {
        // Arrange: Khách VIP (đáng lẽ giảm 20%) nhưng có tiền sử bồi thường (HasPastClaims = true) => Mất quyền giảm giá
        var customer = new CustomerProfileBuilder()
            .AsVip()
            .WithPastClaims(true)
            .Build();
        var property = new PropertyDetailsBuilder().WithEstimatedValue(1_000_000_000m).WithYearBuilt(2015).Build();
        var request = new GenerateQuoteRequest(customer, property, CoverageTier.Basic);

        // Act
        var result = _sut.GenerateQuote(request);

        // Assert
        result.Status.Should().Be(QuoteStatus.Approved);
        result.FinalPremium.Should().Be(1_000_000m); // Không được giảm, giữ nguyên 1,000,000
    }

    #endregion
}
```

---

### Bước 3.3: Viết Production Code `InsuranceQuoteEngineService`

Tạo file `src/InsuranceQuoteEngine/Domain/InsuranceQuoteEngineService.cs`:

```csharp
namespace InsuranceQuoteEngine.Domain;

public class InsuranceQuoteEngineService
{
    private const decimal MinInsurablePropertyValue = 100_000_000m; // 100 triệu VND
    private const decimal BaseRateMultiplier = 0.001m;              // 0.1%
    private const int QuoteValidityDays = 30;

    private readonly IClock _clock;

    public InsuranceQuoteEngineService(IClock clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public QuoteResult GenerateQuote(GenerateQuoteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var now = _clock.UtcNow;
        var quoteId = Guid.NewGuid();

        // 1. Kiểm tra các điều kiện từ chối (Decline Rules)
        if (request.Customer.Age < 18 || request.Customer.Age > 75)
        {
            return new QuoteResult(
                quoteId,
                QuoteStatus.Declined,
                BasePremium: 0m,
                FinalPremium: 0m,
                DecisionReason: "Customer age is ineligible for insurance. Must be between 18 and 75.",
                GeneratedAtUtc: now,
                ExpiresAtUtc: now);
        }

        if (request.Property.EstimatedValue < MinInsurablePropertyValue)
        {
            return new QuoteResult(
                quoteId,
                QuoteStatus.Declined,
                BasePremium: 0m,
                FinalPremium: 0m,
                DecisionReason: $"Property value is below insurable limit of {MinInsurablePropertyValue:N0} VND.",
                GeneratedAtUtc: now,
                ExpiresAtUtc: now);
        }

        // 2. Kiểm tra các điều kiện chuyển chuyên viên duyệt (Referral Rules)
        if (request.Property.IsInFloodZone)
        {
            return new QuoteResult(
                quoteId,
                QuoteStatus.Referred,
                BasePremium: 0m,
                FinalPremium: 0m,
                DecisionReason: "Property is in a designated flood hazard zone.",
                GeneratedAtUtc: now,
                ExpiresAtUtc: now.AddDays(QuoteValidityDays));
        }

        if (request.Property.YearBuilt < 1950)
        {
            return new QuoteResult(
                quoteId,
                QuoteStatus.Referred,
                BasePremium: 0m,
                FinalPremium: 0m,
                DecisionReason: "Property built prior to 1950 requires structural underwriting.",
                GeneratedAtUtc: now,
                ExpiresAtUtc: now.AddDays(QuoteValidityDays));
        }

        // 3. Tính phí cơ sở (Base Premium)
        var basePremium = request.Property.EstimatedValue * BaseRateMultiplier;

        // 4. Áp dụng hệ số gói bảo hiểm (Coverage Tier)
        var coverageMultiplier = request.Coverage switch
        {
            CoverageTier.Standard => 1.25m,
            CoverageTier.Comprehensive => 1.60m,
            _ => 1.0m // Basic
        };

        var premiumAfterCoverage = basePremium * coverageMultiplier;

        // 5. Tính toán chiết khấu (Customer Discounts)
        var discountPercentage = 0.0m;
        if (!request.Customer.HasPastClaims)
        {
            discountPercentage = request.Customer.Type switch
            {
                CustomerType.Vip => 0.20m,
                CustomerType.Premium => 0.10m,
                _ => 0.0m
            };
        }

        var finalPremium = premiumAfterCoverage * (1.0m - discountPercentage);

        return new QuoteResult(
            quoteId,
            QuoteStatus.Approved,
            BasePremium: basePremium,
            FinalPremium: Math.Round(finalPremium, 2),
            DecisionReason: "Quote automatically approved by underwriting rules.",
            GeneratedAtUtc: now,
            ExpiresAtUtc: now.AddDays(QuoteValidityDays));
    }
}
```

---

### Bước 3.4: Chạy kiểm thử Capstone Project

Chạy lệnh trong terminal:
```bash
dotnet test --filter FullyQualifiedName~InsuranceQuoteEngineTests
```

**Kết quả:** Tất cả các bài test từ chối, chuyển duyệt, tính phí và chiết khấu đều **🟢 XANH 100%!**

---

## ✅ Check-list hoàn thành Lesson 07
- [ ] Áp dụng quy trình TDD phát triển một bài toán thực tế từ con số 0.
- [ ] Xử lý đầy đủ ma trận các trạng thái: Approved, Referred, Declined.
- [ ] Kết hợp trơn tru Test Data Builder và Mock Clock.
- [ ] Mã nguồn Domain sạch sẽ, không phụ thuộc vào bất kỳ thư viện I/O hay UI bên ngoài.

👉 **Tiếp theo:** Chuyển sang [Lesson 08: Đánh giá chất lượng Test với Mutation Testing (Stryker.NET)](./08-mutation-testing-stryker.md)!
