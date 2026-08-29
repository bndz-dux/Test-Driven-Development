# Lesson 04: Xây dựng Test Data Builders & Fixtures (Test Data Builders)

## 🎯 Mục tiêu bài học
- Nhận diện vấn đề **"Test Clutter" (Rác trong test)** khi phải khởi tạo các đối tượng lớn với nhiều thuộc tính không liên quan đến bài test.
- Nắm vững mẫu thiết kế **Test Data Builder Pattern**:
  - Cung cấp sẵn các giá trị mặc định hợp lệ (Valid Defaults).
  - Sử dụng phương thức chuỗi (Fluent API: `With...`) để tùy biến dữ liệu bài test cần.
- Xây dựng thư viện Builders tái sử dụng: `CustomerBuilder`, `OrderBuilder`, `QuoteBuilder`.
- Áp dụng **xUnit Fixtures (`IClassFixture<T>`)** để chia sẻ tài nguyên khởi tạo tốn kém.
- **Thực hành:** Refactor toàn bộ test suite từ cách khởi tạo truyền thống sang dùng Builders.

---

## 😫 1. Vấn đề của khởi tạo dữ liệu trực tiếp trong Test

Hãy nhìn vào đoạn test dưới đây:

```csharp
[Fact]
public void CalculateDiscount_ShouldApplyDiscount_ForVipCustomer()
{
    // Arrange: Quá nhiều dòng rác, thuộc tính phụ che lấp mất mục đích thực sự của bài test!
    var customer = new Customer
    {
        Id = Guid.NewGuid(),
        FirstName = "John",
        LastName = "Doe",
        Email = "john.doe@example.com",
        PhoneNumber = "0901234567",
        Address = "123 Main St, District 1, HCM",
        DateOfBirth = new DateTime(1990, 5, 20),
        MembershipLevel = CustomerType.Vip, // <-- ĐÂY LÀ GIÁ TRỊ DUY NHẤT ẢNH HƯỞNG ĐẾN TEST!
        IsEmailVerified = true,
        CreatedAt = DateTime.UtcNow
    };

    var result = _calculator.CalculateDiscount(customer);

    result.Should().Be(0.20m);
}
```

### Hậu quả:
1. **Khó đọc:** Người đọc test phải căng mắt tìm xem dòng nào là mấu chốt kích hoạt nghiệp vụ.
2. **Khó bảo trì:** Khi thêm một trường bắt buộc mới vào `Customer` (ví dụ `NationalId`), **hàng trăm bài test** sẽ đồng loạt báo lỗi biên dịch (Compile Error).

---

## ✨ 2. Giải pháp: Test Data Builder Pattern

Test Data Builder là một class phụ trợ trong thư mục test có nhiệm vụ:
1. Tạo một đối tượng có trạng thái **mặc định hợp lệ (valid default state)**.
2. Cung cấp các hàm `With...` trả về chính builder (`this`) để ghi đè chỉ những thuộc tính mà bài test quan tâm.
3. Cung cấp hàm `Build()` hoặc toán tử ép kiểu ngầm định (`implicit operator`) để trả về đối tượng thật.

### Sau khi dùng Builder:
```csharp
[Fact]
public void CalculateDiscount_ShouldApplyDiscount_ForVipCustomer()
{
    // Arrange: Cực kỳ ngắn gọn, thể hiện rõ 100% ý đồ nghiệp vụ!
    var customer = new CustomerBuilder()
        .AsVip()
        .Build();

    var result = _calculator.CalculateDiscount(customer);

    result.Should().Be(0.20m);
}
```

---

## 🛠️ 3. Thực hành Step-by-Step: Xây dựng Reusable Builders

### Bước 3.1: Định nghĩa Domain Models

Tạo file `src/InsuranceQuoteEngine/Domain/InsuranceModels.cs`:

```csharp
namespace InsuranceQuoteEngine.Domain;

public enum CustomerType
{
    Standard,
    Premium,
    Vip
}

public record CustomerProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string FullName { get; init; } = "John Doe";
    public string Email { get; init; } = "john.doe@test.com";
    public int Age { get; init; } = 30;
    public CustomerType Type { get; init; } = CustomerType.Standard;
    public string PostalCode { get; init; } = "70000";
    public bool HasPastClaims { get; init; } = false;
}

public record PropertyDetails
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Address { get; init; } = "123 Main Street";
    public int YearBuilt { get; init; } = 2018;
    public decimal EstimatedValue { get; init; } = 500_000m;
    public bool IsInFloodZone { get; init; } = false;
}

public record InsuranceQuote
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public CustomerProfile Customer { get; init; } = default!;
    public PropertyDetails Property { get; init; } = default!;
    public decimal BasePremium { get; init; } = 1000m;
    public decimal FinalPremium { get; init; } = 1000m;
    public bool IsReferredToUnderwriter { get; init; } = false;
    public bool IsDeclined { get; init; } = false;
    public DateTime ExpiresAtUtc { get; init; } = DateTime.UtcNow.AddDays(30);
}
```

---

### Bước 3.2: Xây dựng `CustomerProfileBuilder`

Tạo file `tests/InsuranceQuoteEngine.UnitTests/Builders/CustomerProfileBuilder.cs`:

```csharp
using InsuranceQuoteEngine.Domain;

namespace InsuranceQuoteEngine.UnitTests.Builders;

public class CustomerProfileBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _fullName = "Default Test Customer";
    private string _email = "test.customer@example.com";
    private int _age = 30;
    private CustomerType _type = CustomerType.Standard;
    private string _postalCode = "70000";
    private bool _hasPastClaims = false;

    public CustomerProfileBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public CustomerProfileBuilder WithFullName(string name)
    {
        _fullName = name;
        return this;
    }

    public CustomerProfileBuilder WithAge(int age)
    {
        _age = age;
        return this;
    }

    public CustomerProfileBuilder WithType(CustomerType type)
    {
        _type = type;
        return this;
    }

    public CustomerProfileBuilder AsPremium() => WithType(CustomerType.Premium);
    public CustomerProfileBuilder AsVip() => WithType(CustomerType.Vip);

    public CustomerProfileBuilder WithPastClaims(bool hasClaims = true)
    {
        _hasPastClaims = hasClaims;
        return this;
    }

    public CustomerProfileBuilder WithPostalCode(string postalCode)
    {
        _postalCode = postalCode;
        return this;
    }

    public CustomerProfile Build()
    {
        return new CustomerProfile
        {
            Id = _id,
            FullName = _fullName,
            Email = _email,
            Age = _age,
            Type = _type,
            PostalCode = _postalCode,
            HasPastClaims = _hasPastClaims
        };
    }

    // Tiện ích: Cho phép gán trực tiếp CustomerProfile customer = new CustomerProfileBuilder();
    public static implicit operator CustomerProfile(CustomerProfileBuilder builder) => builder.Build();
}
```

---

### Bước 3.3: Xây dựng `PropertyDetailsBuilder` & `InsuranceQuoteBuilder`

Tạo file `tests/InsuranceQuoteEngine.UnitTests/Builders/PropertyDetailsBuilder.cs`:

```csharp
using InsuranceQuoteEngine.Domain;

namespace InsuranceQuoteEngine.UnitTests.Builders;

public class PropertyDetailsBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _address = "123 Nguyen Hue, Ben Nghe, Q1";
    private int _yearBuilt = 2020;
    private decimal _estimatedValue = 1_000_000m;
    private bool _isInFloodZone = false;

    public PropertyDetailsBuilder WithAddress(string address)
    {
        _address = address;
        return this;
    }

    public PropertyDetailsBuilder WithYearBuilt(int year)
    {
        _yearBuilt = year;
        return this;
    }

    public PropertyDetailsBuilder WithEstimatedValue(decimal value)
    {
        _estimatedValue = value;
        return this;
    }

    public PropertyDetailsBuilder InFloodZone(bool inFloodZone = true)
    {
        _isInFloodZone = inFloodZone;
        return this;
    }

    public PropertyDetails Build() => new()
    {
        Id = _id,
        Address = _address,
        YearBuilt = _yearBuilt,
        EstimatedValue = _estimatedValue,
        IsInFloodZone = _isInFloodZone
    };

    public static implicit operator PropertyDetails(PropertyDetailsBuilder builder) => builder.Build();
}
```

Tạo file `tests/InsuranceQuoteEngine.UnitTests/Builders/InsuranceQuoteBuilder.cs`:

```csharp
using InsuranceQuoteEngine.Domain;

namespace InsuranceQuoteEngine.UnitTests.Builders;

public class InsuranceQuoteBuilder
{
    private Guid _id = Guid.NewGuid();
    private CustomerProfile _customer = new CustomerProfileBuilder().Build();
    private PropertyDetails _property = new PropertyDetailsBuilder().Build();
    private decimal _basePremium = 1000m;
    private decimal _finalPremium = 1000m;
    private bool _isReferred = false;
    private bool _isDeclined = false;
    private DateTime _expiresAtUtc = DateTime.UtcNow.AddDays(30);

    public InsuranceQuoteBuilder WithCustomer(CustomerProfile customer)
    {
        _customer = customer;
        return this;
    }

    public InsuranceQuoteBuilder WithCustomer(Action<CustomerProfileBuilder> configure)
    {
        var builder = new CustomerProfileBuilder();
        configure(builder);
        _customer = builder.Build();
        return this;
    }

    public InsuranceQuoteBuilder WithProperty(PropertyDetails property)
    {
        _property = property;
        return this;
    }

    public InsuranceQuoteBuilder WithProperty(Action<PropertyDetailsBuilder> configure)
    {
        var builder = new PropertyDetailsBuilder();
        configure(builder);
        _property = builder.Build();
        return this;
    }

    public InsuranceQuoteBuilder WithBasePremium(decimal premium)
    {
        _basePremium = premium;
        return this;
    }

    public InsuranceQuoteBuilder WithFinalPremium(decimal premium)
    {
        _finalPremium = premium;
        return this;
    }

    public InsuranceQuoteBuilder AsReferred()
    {
        _isReferred = true;
        return this;
    }

    public InsuranceQuoteBuilder AsDeclined()
    {
        _isDeclined = true;
        return this;
    }

    public InsuranceQuoteBuilder Expired()
    {
        _expiresAtUtc = DateTime.UtcNow.AddDays(-1);
        return this;
    }

    public InsuranceQuote Build() => new()
    {
        Id = _id,
        Customer = _customer,
        Property = _property,
        BasePremium = _basePremium,
        FinalPremium = _finalPremium,
        IsReferredToUnderwriter = _isReferred,
        IsDeclined = _isDeclined,
        ExpiresAtUtc = _expiresAtUtc
    };

    public static implicit operator InsuranceQuote(InsuranceQuoteBuilder builder) => builder.Build();
}
```

---

### Bước 3.4: Sử dụng Builders trong Unit Tests thực tế

Tạo file `tests/InsuranceQuoteEngine.UnitTests/Domain/QuoteDiscountPolicyTests.cs`:

```csharp
using FluentAssertions;
using InsuranceQuoteEngine.Domain;
using InsuranceQuoteEngine.UnitTests.Builders;
using Xunit;

namespace InsuranceQuoteEngine.UnitTests.Domain;

public class QuoteDiscountPolicyTests
{
    // Giả lập một hàm tính giảm giá dựa trên hồ sơ khách hàng
    private decimal CalculateDiscountPercentage(CustomerProfile customer)
    {
        return customer.Type switch
        {
            CustomerType.Vip => 0.20m,      // 20%
            CustomerType.Premium => 0.10m,  // 10%
            _ => 0m                         // 0%
        };
    }

    [Fact]
    public void CalculateDiscount_ShouldReturn20Percent_WhenCustomerIsVip()
    {
        // Arrange - Cực kỳ tường minh, không có dữ liệu thừa
        var vipCustomer = new CustomerProfileBuilder()
            .AsVip()
            .Build();

        // Act
        var discount = CalculateDiscountPercentage(vipCustomer);

        // Assert
        discount.Should().Be(0.20m);
    }

    [Fact]
    public void CalculateDiscount_ShouldReturn10Percent_WhenCustomerIsPremium()
    {
        // Arrange
        var premiumCustomer = new CustomerProfileBuilder()
            .AsPremium()
            .Build();

        // Act
        var discount = CalculateDiscountPercentage(premiumCustomer);

        // Assert
        discount.Should().Be(0.10m);
    }

    [Fact]
    public void CalculateDiscount_ShouldReturnZero_WhenCustomerIsStandard()
    {
        // Arrange - Dùng giá trị mặc định (Standard)
        var standardCustomer = new CustomerProfileBuilder().Build();

        // Act
        var discount = CalculateDiscountPercentage(standardCustomer);

        // Assert
        discount.Should().Be(0.0m);
    }

    [Fact]
    public void QuoteBuilder_NestedConfiguration_ShouldCreateComplexQuoteCleanly()
    {
        // Minh họa cấu hình lồng nhau rất tự nhiên và sạch đẹp
        InsuranceQuote quote = new InsuranceQuoteBuilder()
            .WithCustomer(c => c.AsVip().WithAge(45))
            .WithProperty(p => p.InFloodZone(true).WithEstimatedValue(2_000_000m))
            .WithBasePremium(1500m);

        quote.Customer.Type.Should().Be(CustomerType.Vip);
        quote.Customer.Age.Should().Be(45);
        quote.Property.IsInFloodZone.Should().BeTrue();
        quote.BasePremium.Should().Be(1500m);
    }
}
```

---

## ⚡ 4. Sử dụng xUnit Fixtures (`IClassFixture<T>`)

Khi có những tài nguyên khởi tạo tốn nhiều thời gian (ví dụ: cấu hình bộ nhớ chung, ánh xạ AutoMapper/JsonSerializerOptions), hãy dùng `IClassFixture<T>` để khởi tạo **1 lần duy nhất** cho cả class test thay vì mỗi test method:

```csharp
public class SharedEngineFixture : IDisposable
{
    public SharedEngineFixture()
    {
        // Khởi tạo tài nguyên dùng chung nặng
    }

    public void Dispose()
    {
        // Dọn dẹp tài nguyên
    }
}

public class EngineFixtureTests : IClassFixture<SharedEngineFixture>
{
    private readonly SharedEngineFixture _fixture;

    public EngineFixtureTests(SharedEngineFixture fixture)
    {
        _fixture = fixture;
    }
}
```

---

## ✅ Check-list hoàn thành Lesson 04
- [ ] Hiểu rõ lợi ích của Test Data Builder trong việc giảm thiểu "Test Clutter" và chống "Brittle Tests".
- [ ] Tạo thành công `CustomerProfileBuilder`, `PropertyDetailsBuilder`, `InsuranceQuoteBuilder`.
- [ ] Áp dụng `implicit operator` và Nested Builders (`Action<TBuilder>`).
- [ ] Refactor các đoạn khởi tạo test cũ để sử dụng Builders.

👉 **Tiếp theo:** Chuyển sang [Lesson 05: Thực hành Test-Driven Development (TDD) Cốt Lõi](./05-tdd-core-workflow.md)!
