# Lesson 07: Capstone Project – Insurance Quote Engine

## 🎯 Lesson Objectives
- Consolidate all concepts from Lessons 01 through 06 into a **realistic domain-driven capstone application**.
- Apply **Test-Driven Development (TDD)** to architect and implement an underwriting calculation engine (`InsuranceQuoteEngine`) from business specifications.
- Design an end-to-end domain processing pipeline:
  - Eligibility and boundary validation
  - Risk assessment
  - Base premium calculation
  - Coverage tier multipliers
  - Customer discount calculations and claims penalties
  - Referral and decline rule evaluation
- Produce immutable quote outcomes (`InsuranceQuoteResult`).

---

## 🏛️ 1. Quote Processing Pipeline Architecture

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

## 📜 2. Detailed Business Rules

| ID | Rule | Description | Outcome |
| :--- | :--- | :--- | :--- |
| **BR-01** | Customer Age Limits | Age < 18 or > 75 | **DECLINE** with reason: *"Customer age is ineligible for insurance. Must be between 18 and 75."* |
| **BR-02** | Flood Zone Hazard | Property is situated in a designated hazard zone (`IsInFloodZone = true`) | **REFER** (Refer to underwriter) |
| **BR-03** | Historic Structure Age | Property built prior to 1950 (`YearBuilt < 1950`) | **REFER** (Refer to underwriter) |
| **BR-04** | Minimum Property Value | Estimated value (`EstimatedValue`) < 100,000,000 VND | **DECLINE** with reason: *"Property value is below insurable limit of 100,000,000 VND."* |
| **BR-05** | Base Premium Rate | `BasePremium = EstimatedValue * 0.001` (0.1% of property value) | Base amount |
| **BR-06** | Coverage Tiers | - **Basic:** Multiplier `1.0`<br>- **Standard:** Multiplier `1.25`<br>- **Comprehensive:** Multiplier `1.60` | Tier adjusted premium |
| **BR-07** | Customer Discounts | - **Normal:** 0%<br>- **Premium:** 10% discount<br>- **VIP:** 20% discount | Discounted premium (`FinalPremium`) |
| **BR-08** | Claims History Ineligibility | If customer has a prior claim record (`HasPastClaims = true`), **all loyalty discounts are forfeited** | Full undiscounted premium |

---

## 🛠️ 3. Step-by-Step TDD Implementation

We will define domain models and drive engine development using TDD.

### Step 3.1: Define Domain Enums & Models

Create file `src/InsuranceQuoteEngine/Domain/QuoteDomainModels.cs`:

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

### Step 3.2: Write the Test Suite using TDD

Create file `tests/InsuranceQuoteEngine.UnitTests/Domain/InsuranceQuoteEngineTests.cs`:

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
        var property = new PropertyDetailsBuilder().WithEstimatedValue(50_000_000m).Build(); // < 100M
        var request = new GenerateQuoteRequest(customer, property, CoverageTier.Basic);

        // Act
        var result = _sut.GenerateQuote(request);

        // Assert
        result.Status.Should().Be(QuoteStatus.Declined);
        result.DecisionReason.Should().Contain("Property value is below insurable limit");
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
        // Arrange: 1B VND property -> Base rate 0.1% = 1,000,000. Basic tier = 1.0x. No discount.
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
        // Arrange: 1B VND property -> Base 1,000,000. Standard tier = 1.25x => 1,250,000.
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
        // Arrange: 1B VND property -> Base 1,000,000. Comprehensive tier = 1.60x => 1,600,000.
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
        // Arrange: 1B VND property, Comprehensive tier (1,600,000). VIP customer gets 20% discount => 1,280,000.
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
        // Arrange: VIP customer with prior claims (HasPastClaims = true) => Disqualified from discount
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
        result.FinalPremium.Should().Be(1_000_000m); // Retains full 1,000,000
    }

    #endregion
}
```

---

### Step 3.3: Implement `InsuranceQuoteEngineService`

Create file `src/InsuranceQuoteEngine/Domain/InsuranceQuoteEngineService.cs`:

```csharp
namespace InsuranceQuoteEngine.Domain;

public class InsuranceQuoteEngineService
{
    private const decimal MinInsurablePropertyValue = 100_000_000m; // 100M VND
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

        // 1. Check decline rules (BR-01, BR-04)
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

        // 2. Check referral rules (BR-02, BR-03)
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

        // 3. Calculate base premium (BR-05)
        var basePremium = request.Property.EstimatedValue * BaseRateMultiplier;

        // 4. Apply coverage tier multiplier (BR-06)
        var coverageMultiplier = request.Coverage switch
        {
            CoverageTier.Standard => 1.25m,
            CoverageTier.Comprehensive => 1.60m,
            _ => 1.0m // Basic
        };

        var premiumAfterCoverage = basePremium * coverageMultiplier;

        // 5. Calculate customer discounts & claims adjustments (BR-07, BR-08)
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

### Step 3.4: Execute Capstone Test Suite

Run from the terminal:
```bash
dotnet test --filter FullyQualifiedName~InsuranceQuoteEngineTests
```

**Result:** All decline, referral, premium, and discount test cases **🟢 PASS 100%!**

---

## ✅ Lesson 07 Completion Checklist
- [ ] Applied full TDD lifecycle to develop a realistic insurance underwriting engine.
- [ ] Validated the full matrix of states: Approved, Referred, and Declined.
- [ ] Integrated Test Data Builders and mocked clock abstractions.
- [ ] Built pure, isolated domain logic free from third-party I/O and UI dependencies.

👉 **Next Step:** Proceed to [Lesson 08: Mutation Testing with Stryker.NET](./08-mutation-testing-stryker.md)!
