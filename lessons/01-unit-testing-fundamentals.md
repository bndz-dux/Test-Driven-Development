# Lesson 01: Unit Testing Fundamentals & xUnit

## 🎯 Lesson Objectives
- Understand what defines a "Unit" and distinguish between Unit Tests, Integration Tests, and End-to-End (E2E) Tests.
- Master the two critical qualities of a good unit test: **Isolation** and **Determinism**.
- Apply the classic **Arrange – Act – Assert (AAA)** structural pattern.
- Master industry-standard expressive test naming: `Method_ShouldExpectedBehavior_WhenCondition`.
- Utilize xUnit `[Fact]` and the `FluentAssertions` assertion library.
- **Hands-on:** Implement the `PriceCalculator` domain service and write a comprehensive suite of 10–15 unit tests.

---

## 📖 1. Core Concepts

### 1.1. What is a "Unit" in Unit Testing?
- A "Unit" is not necessarily a single method or a single class. A unit is a **unit of observable behavior** that can be verified in isolation without depending on real databases, network sockets, the file system, or the ambient system clock.
- **The Test Pyramid:**
  - **Unit Tests:** Millisecond execution speeds, run entirely in memory, form the broad foundation (largest quantity).
  - **Integration Tests:** Verify interactions between application components and out-of-process infrastructure (Database, external APIs, file system). Slower, moderate quantity.
  - **End-to-End (E2E) Tests:** Verify end-to-end user workflows from UI/API down to underlying infrastructure. Slowest, brittle, smallest quantity.

### 1.2. Two Vital Qualities of a Unit Test
1. **Isolated:** Test A must not affect or depend on the execution result or state of Test B. Test execution order must never matter.
2. **Deterministic:** Given the same input, a test must always produce the exact same outcome whether run locally by a developer, on a CI server, at midnight, or in any timezone.

---

## 📐 2. The AAA Pattern (Arrange – Act – Assert)

Every unit test should be divided into three clear phases:

```csharp
[Fact]
public void CalculateDiscount_ShouldReturnDiscountedPrice_WhenValidDiscountApplied()
{
    // 1. Arrange: Set up inputs, state, and the System Under Test (SUT)
    var calculator = new PriceCalculator();
    const decimal originalPrice = 100m;
    const decimal discountPercentage = 0.20m; // 20%

    // 2. Act: Execute the single action/method under test
    var result = calculator.Calculate(originalPrice, discountPercentage);

    // 3. Assert: Verify expected outcome or state mutation
    result.Should().Be(80m);
}
```

---

## 🏷️ 3. Test Naming Conventions

Test names serve as **living documentation** for system behavior. When a test fails in a CI build log, developers should understand the exact failure scenario without having to read the source code.

### ✅ Recommended Pattern:
```text
[MethodName]_[ShouldExpectedBehavior]_[WhenCondition]
Method_ShouldExpectedBehavior_WhenCondition
```

**Clean Examples:**
- `Calculate_ShouldReturnOriginalPrice_WhenDiscountIsZero`
- `Calculate_ShouldReturnZero_WhenDiscountIsOneHundredPercent`
- `Calculate_ShouldThrowArgumentOutOfRangeException_WhenPriceIsNegative`
- `Calculate_ShouldThrowArgumentOutOfRangeException_WhenDiscountIsGreaterThanOne`

### ❌ Anti-Patterns to Avoid:
- `Test1`, `TestPrice`
- `ShouldWork`, `CalculateTest`
- `Test_Discount_Success`

---

## 🔍 4. Working with FluentAssertions

Compared to classic xUnit `Assert.Equal(expected, actual)`, `FluentAssertions` offers natural language readability and detailed contextual error messages:

```csharp
// Standard xUnit Assertions
Assert.Equal(80m, result);
Assert.Throws<ArgumentOutOfRangeException>(() => calculator.Calculate(-100, 0.2m));

// FluentAssertions (Recommended)
result.Should().Be(80m);
result.Should().BeGreaterThan(0);

Action act = () => calculator.Calculate(-100, 0.2m);
act.Should().Throw<ArgumentOutOfRangeException>()
   .WithMessage("*Price cannot be negative*");
```

---

## 🛠️ 5. Step-by-Step Exercise: Implementing `PriceCalculator`

### Step 5.1: Requirements Analysis
Build a domain service `PriceCalculator` with the method:
`decimal Calculate(decimal originalPrice, decimal discountPercentage)`

**Business Rules:**
1. `discountPercentage` accepts values between `0.0` (0%) and `1.0` (100%).
2. If `discountPercentage == 0` → Original price remains unchanged.
3. If `discountPercentage == 0.2` (20%) on 100 → Result is 80.
4. If `discountPercentage == 1.0` (100%) on 100 → Result is 0.
5. If `originalPrice == 0` → Result is always 0.
6. If `originalPrice < 0` → Throw `ArgumentOutOfRangeException` ("Price cannot be negative").
7. If `discountPercentage < 0` or `discountPercentage > 1` → Throw `ArgumentOutOfRangeException` ("Discount percentage must be between 0.0 and 1.0").

---

### Step 5.2: Create Domain Code for `PriceCalculator`

Create file `src/InsuranceQuoteEngine/Domain/PriceCalculator.cs`:

```csharp
namespace InsuranceQuoteEngine.Domain;

public class PriceCalculator
{
    /// <summary>
    /// Calculates the discounted price after applying a percentage discount.
    /// </summary>
    /// <param name="originalPrice">Original price (>= 0)</param>
    /// <param name="discountPercentage">Discount percentage (0.0 to 1.0)</param>
    /// <returns>Price after discount</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public decimal Calculate(decimal originalPrice, decimal discountPercentage)
    {
        if (originalPrice < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(originalPrice), 
                "Price cannot be negative.");
        }

        if (discountPercentage < 0 || discountPercentage > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discountPercentage), 
                "Discount percentage must be between 0.0 and 1.0.");
        }

        var discountAmount = originalPrice * discountPercentage;
        return originalPrice - discountAmount;
    }
}
```

---

### Step 5.3: Write the Complete Unit Test Suite

Create file `tests/InsuranceQuoteEngine.UnitTests/Domain/PriceCalculatorTests.cs`:

```csharp
using FluentAssertions;
using InsuranceQuoteEngine.Domain;
using Xunit;

namespace InsuranceQuoteEngine.UnitTests.Domain;

public class PriceCalculatorTests
{
    private readonly PriceCalculator _sut; // SUT = System Under Test

    public PriceCalculatorTests()
    {
        _sut = new PriceCalculator();
    }

    #region Happy Path Tests

    [Fact]
    public void Calculate_ShouldReturnOriginalPrice_WhenDiscountIsZero()
    {
        // Arrange
        const decimal price = 100m;
        const decimal discount = 0m;

        // Act
        var result = _sut.Calculate(price, discount);

        // Assert
        result.Should().Be(100m);
    }

    [Fact]
    public void Calculate_ShouldReturnDiscountedPrice_WhenDiscountIsTwentyPercent()
    {
        // Arrange
        const decimal price = 100m;
        const decimal discount = 0.20m;

        // Act
        var result = _sut.Calculate(price, discount);

        // Assert
        result.Should().Be(80m);
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenDiscountIsOneHundredPercent()
    {
        // Arrange
        const decimal price = 100m;
        const decimal discount = 1.0m;

        // Act
        var result = _sut.Calculate(price, discount);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenOriginalPriceIsZero()
    {
        // Arrange
        const decimal price = 0m;
        const decimal discount = 0.50m;

        // Act
        var result = _sut.Calculate(price, discount);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public void Calculate_ShouldHandleFractionalAmounts_WhenDiscountProducesDecimals()
    {
        // Arrange
        const decimal price = 99.99m;
        const decimal discount = 0.15m; // 15% of 99.99 = 14.9985 => remaining 84.9915

        // Act
        var result = _sut.Calculate(price, discount);

        // Assert
        result.Should().Be(84.9915m);
    }

    #endregion

    #region Failure Path & Boundary Validation Tests

    [Fact]
    public void Calculate_ShouldThrowArgumentOutOfRangeException_WhenOriginalPriceIsNegative()
    {
        // Arrange
        const decimal negativePrice = -100m;
        const decimal discount = 0.20m;

        // Act
        Action act = () => _sut.Calculate(negativePrice, discount);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("originalPrice")
           .WithMessage("*Price cannot be negative*");
    }

    [Fact]
    public void Calculate_ShouldThrowArgumentOutOfRangeException_WhenDiscountIsLessThanZero()
    {
        // Arrange
        const decimal price = 100m;
        const decimal negativeDiscount = -0.01m;

        // Act
        Action act = () => _sut.Calculate(price, negativeDiscount);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("discountPercentage")
           .WithMessage("*Discount percentage must be between 0.0 and 1.0*");
    }

    [Fact]
    public void Calculate_ShouldThrowArgumentOutOfRangeException_WhenDiscountIsGreaterThanOne()
    {
        // Arrange
        const decimal price = 100m;
        const decimal invalidDiscount = 1.01m;

        // Act
        Action act = () => _sut.Calculate(price, invalidDiscount);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("discountPercentage")
           .WithMessage("*Discount percentage must be between 0.0 and 1.0*");
    }

    #endregion
}
```

---

### Step 5.4: Execute Tests and Observe Results

Run tests from the terminal:
```bash
dotnet test --filter FullyQualifiedName~PriceCalculatorTests
```

**Expected Output:**
```text
Passed!  - Failed: 0, Passed: 8, Skipped: 0, Total: 8
```

---

## 📝 6. Hands-on Challenge

Extend `PriceCalculator` by adding:
`decimal CalculateWithTax(decimal originalPrice, decimal discountPercentage, decimal taxRate)`

**Rules:**
1. Tax is calculated on the price **after discount is applied**:
   `FinalPrice = (OriginalPrice - DiscountAmount) * (1 + taxRate)`.
2. `taxRate` must be between `0.0` and `0.5` (up to 50%). If negative or > 0.5 → throw `ArgumentOutOfRangeException`.
3. Write at least **5 unit tests** covering:
   - Happy path: price 100, discount 20%, tax 10% (0.1) => result 88.
   - Zero tax.
   - Negative tax => throws exception.
   - Tax exceeding 0.5 => throws exception.
   - Zero original price with tax => result 0.

---

## ✅ Lesson 01 Completion Checklist
- [ ] Understand and apply the AAA structure across every test.
- [ ] Follow the naming standard `Method_ShouldExpectedBehavior_WhenCondition`.
- [ ] Use `FluentAssertions` for both returned values and exceptions (`Throw<T>`).
- [ ] Implemented unit tests for `PriceCalculator` and completed the `CalculateWithTax` challenge.

👉 **Next Step:** Proceed to [Lesson 02: Test Design & Test Quality](./02-test-design-and-quality.md)!
