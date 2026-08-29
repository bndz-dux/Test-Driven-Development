# Lesson 06: Advanced Unit Testing Techniques

## 🎯 Lesson Objectives
- Master **Parameterized Testing** in xUnit:
  - `[Theory]` combined with `[InlineData]`
  - `[MemberData]` for complex object collections
  - `[ClassData]` for dedicated test-case data structures
- Solve **Time-dependent Logic Testing**:
  - Eliminate direct ambient calls to `DateTime.UtcNow` in domain logic.
  - Apply the `IClock` abstraction or .NET 8+ `TimeProvider`.
  - Deterministically test quote expiration and policy effective date ranges.
- Handle asynchronous unit tests, deep exception assertions, and idempotency guarantees.

---

## 📊 1. Parameterized Testing in xUnit (`[Theory]`)

When a single test method must execute against dozens of different input combinations and expected outputs:

### 1.1. Using `[InlineData]` for Primitive Data
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

### 1.2. Using `[MemberData]` for Complex Object Data
`[MemberData]` reads test data from a `public static` property or method returning `IEnumerable<object[]>`:

```csharp
public class QuoteTestData
{
    public static IEnumerable<object[]> GetDiscountScenarios()
    {
        yield return new object[] { CustomerMembership.Normal, 1000m, 0.0m };
        yield return new object[] { CustomerMembership.Premium, 1000m, 0.10m };
        yield return new object[] { CustomerMembership.Vip, 1000m, 0.20m };
        yield return new object[] { CustomerMembership.Normal, 6_000_000m, 0.05m }; // 5% bonus for order > 5M
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

### 1.3. Using `[ClassData]` for Dedicated Test Data Classes
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

## ⏰ 2. Testing Time-dependent Logic

### ❌ Anti-pattern: Calling `DateTime.UtcNow` Directly in Business Logic
Writing ambient clock calls directly in domain code:
```csharp
if (DateTime.UtcNow > quote.ExpiresAtUtc)
{
    throw new QuoteExpiredException();
}
```
This produces **flaky tests** and makes it impossible to reliably simulate "30 days into the future" or specific temporal boundary conditions.

### ✅ Solution: Abstracting Time via `IClock` or `TimeProvider`
In .NET 8+, Microsoft standardized time abstraction via `System.TimeProvider` and `Microsoft.Extensions.TimeProvider.Testing`.

---

## 🛠️ 3. Step-by-Step Exercise: `PolicyLifecycleManager` with `IClock`

### Business Requirements:
Build a `PolicyLifecycleManager` domain service:
1. **Quote Expiration:** Quotes are valid for **30 days** from generation.
2. **Policy Activation:**
   - Expired quotes cannot be activated → Throw `QuoteExpiredException`.
   - Upon successful activation:
     - Policy `EffectiveDateUtc` = Current UTC time.
     - Policy `ExpiryDateUtc` = Exactly 1 year later (`EffectiveDateUtc.AddYears(1)`).
3. **Policy Cancellation:**
   - 100% full refund if cancelled within the **14-day cooling-off period**.
   - After 14 days, refund is pro-rated on unused days minus a 20% cancellation fee.

---

### Step 3.1: Define `IClock` and Domain Models

Create file `src/InsuranceQuoteEngine/Domain/TimeAbstraction.cs`:

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

Create file `src/InsuranceQuoteEngine/Domain/Services/PolicyLifecycleManager.cs`:

```csharp
namespace InsuranceQuoteEngine.Domain;

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

        // Within 14 days (Cooling-off period): 100% full refund
        var daysActive = (cancellationDateUtc - policy.EffectiveDateUtc).TotalDays;
        if (daysActive <= 14)
        {
            return policy.AnnualPremium;
        }

        // Already expired policy
        if (cancellationDateUtc >= policy.ExpiryDateUtc)
        {
            return 0m;
        }

        // After 14 days: Pro-rata refund on unused days minus 20% administrative cancellation fee
        var totalDays = (policy.ExpiryDateUtc - policy.EffectiveDateUtc).TotalDays;
        var unusedDays = (policy.ExpiryDateUtc - cancellationDateUtc).TotalDays;
        var unearnedPremium = policy.AnnualPremium * (decimal)(unusedDays / totalDays);

        // 20% penalty fee (retain 80%)
        var refund = unearnedPremium * 0.80m;
        return Math.Round(refund, 2);
    }
}
```

---

### Step 3.2: Write Deterministic Unit Tests

Create file `tests/InsuranceQuoteEngine.UnitTests/Domain/PolicyLifecycleManagerTests.cs`:

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
        // Arrange: Freeze time at 2026-01-01 10:00:00 UTC
        var fixedNow = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        _clockMock.Setup(c => c.UtcNow).Returns(fixedNow);

        var quote = new InsuranceQuoteBuilder()
            .WithFinalPremium(1200m)
            .Build(); // Default expiration is +30 days (2026-01-31)

        // Act
        var policy = _sut.ActivateQuote(quote);

        // Assert
        policy.Should().NotBeNull();
        policy.EffectiveDateUtc.Should().Be(fixedNow);
        policy.ExpiryDateUtc.Should().Be(fixedNow.AddYears(1)); // 2027-01-01
        policy.AnnualPremium.Should().Be(1200m);
        policy.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ActivateQuote_ShouldThrowQuoteExpiredException_WhenCurrentTimeIsPastExpirationDate()
    {
        // Arrange: Current time is past quote expiration date
        var fixedNow = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        _clockMock.Setup(c => c.UtcNow).Returns(fixedNow);

        var quote = new InsuranceQuoteBuilder()
            .Expired()
            .Build();

        // Act
        Action act = () => _sut.ActivateQuote(quote);

        // Assert
        act.Should().Throw<QuoteExpiredException>()
            .WithMessage($"*{quote.Id}*");
    }

    [Fact]
    public void CalculateRefundOnCancellation_ShouldReturnFullAmount_WhenCancelledWithin14DaysCoolingOffPeriod()
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

        var cancellationDate = effectiveDate.AddDays(10); // Day 10 (<= 14 days)

        // Act
        var refund = _sut.CalculateRefundOnCancellation(policy, cancellationDate);

        // Assert
        refund.Should().Be(1000m); // 100% refund
    }

    [Fact]
    public void CalculateRefundOnCancellation_ShouldApply20PercentPenalty_WhenCancelledAfter14Days()
    {
        // Arrange: 365-day policy priced at 1000, cancelled on day 100
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
        // 265 unused days left => Unearned = 1000 * (265 / 365) = 726.027
        // 20% penalty fee => 80% refund of 726.027 = 580.82

        // Act
        var refund = _sut.CalculateRefundOnCancellation(policy, cancellationDate);

        // Assert
        refund.Should().Be(580.82m);
    }
}
```

---

## ✅ Lesson 06 Completion Checklist
- [ ] Mastered `[Theory]`, `[InlineData]`, `[MemberData]`, and `[ClassData]`.
- [ ] Eliminated direct `DateTime.UtcNow` dependencies from domain logic.
- [ ] Controlled temporal execution states in tests using mocked `IClock`.
- [ ] Validated 100% deterministic test execution immune to clock drift and timezone offsets.

👉 **Next Step:** Proceed to [Lesson 07: Capstone Project – Insurance Quote Engine](./07-insurance-quote-engine-capstone.md)!
