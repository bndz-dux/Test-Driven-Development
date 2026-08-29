# Lesson 04: Test Data Builders & Fixtures

## 🎯 Lesson Objectives
- Identify and eliminate **"Test Clutter"** when instantiating large objects with numerous irrelevant properties in test methods.
- Master the **Test Data Builder Pattern**:
  - Provide sensible, valid default values.
  - Expose a fluent API (`With...`) to override only test-relevant properties.
- Build reusable builder libraries: `CustomerBuilder`, `OrderBuilder`, `QuoteBuilder`.
- Apply **xUnit Fixtures (`IClassFixture<T>`)** to share expensive setup resources across tests.
- **Hands-on:** Refactor brittle, noisy test object setups using expressive Test Data Builders.

---

## 😫 1. The Problem with Inline Test Setup

Consider this test example:

```csharp
[Fact]
public void CalculateDiscount_ShouldApplyDiscount_ForVipCustomer()
{
    // Arrange: Excessive irrelevant clutter obscures the actual intent of the test!
    var customer = new Customer
    {
        Id = Guid.NewGuid(),
        FirstName = "John",
        LastName = "Doe",
        Email = "john.doe@example.com",
        PhoneNumber = "0901234567",
        Address = "123 Main St, District 1, HCM",
        DateOfBirth = new DateTime(1990, 5, 20),
        MembershipLevel = CustomerType.Vip, // <-- THE ONLY PROPERTY RELEVANT TO THIS TEST!
        IsEmailVerified = true,
        CreatedAt = DateTime.UtcNow
    };

    var result = _calculator.CalculateDiscount(customer);

    result.Should().Be(0.20m);
}
```

### Consequences:
1. **Low Readability:** Readers must sift through boilerplate to find what triggers the business rule.
2. **High Fragility:** Adding a single mandatory property to `Customer` (e.g. `NationalId`) causes **hundreds of tests** across the solution to fail compilation simultaneously.

---

## ✨ 2. The Solution: Test Data Builder Pattern

A Test Data Builder is a test helper class designed to:
1. Initialize an object with a **valid default state**.
2. Provide fluent `With...` methods returning the builder (`this`) to override only properties relevant to the specific test scenario.
3. Provide a `Build()` method or an `implicit operator` to instantiate the domain object.

### Refactored with a Builder:
```csharp
[Fact]
public void CalculateDiscount_ShouldApplyDiscount_ForVipCustomer()
{
    // Arrange: Expressive, concise, and 100% focused on business intent!
    var customer = new CustomerBuilder()
        .AsVip()
        .Build();

    var result = _calculator.CalculateDiscount(customer);

    result.Should().Be(0.20m);
}
```

---

## 🛠️ 3. Step-by-Step Exercise: Building Reusable Builders

### Step 3.1: Define Domain Models

Create file `src/InsuranceQuoteEngine/Domain/InsuranceModels.cs`:

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

### Step 3.2: Implement `CustomerProfileBuilder`

Create file `tests/InsuranceQuoteEngine.UnitTests/Builders/CustomerProfileBuilder.cs`:

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

    // Implicit operator enables direct assignment: CustomerProfile customer = new CustomerProfileBuilder();
    public static implicit operator CustomerProfile(CustomerProfileBuilder builder) => builder.Build();
}
```

---

### Step 3.3: Implement `PropertyDetailsBuilder` & `InsuranceQuoteBuilder`

Create file `tests/InsuranceQuoteEngine.UnitTests/Builders/PropertyDetailsBuilder.cs`:

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

Create file `tests/InsuranceQuoteEngine.UnitTests/Builders/InsuranceQuoteBuilder.cs`:

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

### Step 3.4: Use Builders in Unit Tests

Create file `tests/InsuranceQuoteEngine.UnitTests/Domain/QuoteDiscountPolicyTests.cs`:

```csharp
using FluentAssertions;
using InsuranceQuoteEngine.Domain;
using InsuranceQuoteEngine.UnitTests.Builders;
using Xunit;

namespace InsuranceQuoteEngine.UnitTests.Domain;

public class QuoteDiscountPolicyTests
{
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
        // Arrange - Expressive and clean with no redundant noise
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
        // Arrange - Using default value (Standard)
        var standardCustomer = new CustomerProfileBuilder().Build();

        // Act
        var discount = CalculateDiscountPercentage(standardCustomer);

        // Assert
        discount.Should().Be(0.0m);
    }

    [Fact]
    public void QuoteBuilder_NestedConfiguration_ShouldCreateComplexQuoteCleanly()
    {
        // Demonstrates nested builder configuration
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

## ⚡ 4. Using xUnit Fixtures (`IClassFixture<T>`)

When test execution requires expensive one-time setup (such as configuring in-memory shared test states or serializer configurations), implement `IClassFixture<T>` to instantiate resources **once per test class** rather than per test method:

```csharp
public class SharedEngineFixture : IDisposable
{
    public SharedEngineFixture()
    {
        // Initialize expensive shared resources
    }

    public void Dispose()
    {
        // Clean up resources
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

## ✅ Lesson 04 Completion Checklist
- [ ] Understand the role of Test Data Builders in eliminating test clutter and preventing brittle tests.
- [ ] Successfully built `CustomerProfileBuilder`, `PropertyDetailsBuilder`, and `InsuranceQuoteBuilder`.
- [ ] Implemented `implicit operator` and nested builders (`Action<TBuilder>`).
- [ ] Refactored test initializations to leverage builder patterns.

👉 **Next Step:** Proceed to [Lesson 05: TDD Core Workflow (Red-Green-Refactor)](./05-tdd-core-workflow.md)!
