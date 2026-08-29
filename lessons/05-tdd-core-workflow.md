# Lesson 05: TDD Core Workflow (Red – Green – Refactor)

## 🎯 Lesson Objectives
- Master the philosophy and the Three Laws of TDD by Uncle Bob (Robert C. Martin).
- Master the classic TDD cycle: **🔴 RED → 🟢 GREEN → 🔵 REFACTOR**.
- Transform your mindset: From "Writing code first and tests as an afterthought" to **"Using tests to specify requirements and drive software architecture"**.
- Experience small, incremental baby steps to maintain complete control over logic.
- **Hands-on Test-First:** Build a discount calculation engine from scratch purely driven by tests.

---

## 🔄 1. The Red – Green – Refactor Cycle

```text
               ┌──────────────────────────────────────────────┐
               │                                              │
               ▼                                              │
         ┌───────────┐                                        │
         │  🔴 RED   │  Write a test describing desired       │
         └─────┬─────┘  behavior (Test must FAIL)             │
               │                                              │
               ▼                                              │
        ┌─────────────┐                                       │
        │  🟢 GREEN   │  Write the MINIMAL production code    │
        └──────┬──────┘  to make the test PASS                │
               │                                              │
               ▼                                              │
       ┌───────────────┐                                      │
       │  🔵 REFACTOR  │  Clean code, remove duplication,     │
       └───────┬───────┘  improve design while tests STAY GREEN│
               │                                              │
               └──────────────────────────────────────────────┘
```

### 📜 Uncle Bob's Three Laws of TDD:
1. **First Law:** You may not write any production code until you have written a failing unit test.
2. **Second Law:** You may not write more of a unit test than is sufficient to fail, and not compiling is failing.
3. **Third Law:** You may not write more production code than is sufficient to pass the currently failing unit test.

---

## 🛠️ 2. Step-by-Step Exercise: Building `DiscountCalculator` via TDD

We will walk through practical **RED → GREEN → REFACTOR** iterations to experience the power of TDD.

### Business Requirements:
A retail pricing system calculates customer discounts based on:
1. Standard customer (Normal) → 0% base discount.
2. Loyalty customer (Premium) → 10% base discount.
3. VIP customer → 20% base discount.
4. Orders exceeding 5,000,000 VND → Add 5% bonus discount (stacks on all tiers).
5. The maximum cumulative discount cap must never exceed 30%.

---

### 🟢 Cycle 1: Normal Customer Receives 0% Discount

#### Step 1.1: 🔴 RED (Write the First Test)
Create file `tests/InsuranceQuoteEngine.UnitTests/Domain/TddDiscountCalculatorTests.cs`:

```csharp
using FluentAssertions;
using Xunit;

namespace InsuranceQuoteEngine.UnitTests.Domain;

public class TddDiscountCalculatorTests
{
    [Fact]
    public void CalculateDiscount_ShouldReturnZero_ForNormalCustomer()
    {
        // Arrange
        var calculator = new DiscountCalculator();

        // Act
        var discount = calculator.CalculateDiscount(CustomerMembership.Normal, orderAmount: 1000m);

        // Assert
        discount.Should().Be(0.0m);
    }
}
```
*Status:* **Compilation Error** (`DiscountCalculator` and `CustomerMembership` do not exist yet). This constitutes a valid **🔴 RED** state in TDD!

---

#### Step 1.2: 🟢 GREEN (Minimal Code to Pass)
Create file `src/InsuranceQuoteEngine/Domain/DiscountCalculator.cs`:

```csharp
namespace InsuranceQuoteEngine.Domain;

public enum CustomerMembership
{
    Normal,
    Premium,
    Vip
}

public class DiscountCalculator
{
    public decimal CalculateDiscount(CustomerMembership membership, decimal orderAmount)
    {
        // Return minimal hardcoded value to make test 1 PASS!
        return 0.0m;
    }
}
```
*Run tests:* `dotnet test` → **🟢 GREEN (PASS)!**

---

#### Step 1.3: 🔵 REFACTOR
The implementation is trivial; no refactoring is required yet. Proceed to Cycle 2!

---

### 🟢 Cycle 2: Premium Customer Receives 10% Discount

#### Step 2.1: 🔴 RED (Add Next Test)
Add to `tests/InsuranceQuoteEngine.UnitTests/Domain/TddDiscountCalculatorTests.cs`:

```csharp
    [Fact]
    public void CalculateDiscount_ShouldReturn10Percent_ForPremiumCustomer()
    {
        // Arrange
        var calculator = new DiscountCalculator();

        // Act
        var discount = calculator.CalculateDiscount(CustomerMembership.Premium, orderAmount: 1000m);

        // Assert
        discount.Should().Be(0.10m);
    }
```
*Run tests:* The new test **🔴 FAILS** (Expected `0.10m`, but received `0.0m`).

---

#### Step 2.2: 🟢 GREEN (Minimal Production Code)
Update `src/InsuranceQuoteEngine/Domain/DiscountCalculator.cs`:

```csharp
public class DiscountCalculator
{
    public decimal CalculateDiscount(CustomerMembership membership, decimal orderAmount)
    {
        if (membership == CustomerMembership.Premium)
        {
            return 0.10m;
        }

        return 0.0m;
    }
}
```
*Run tests:* Both tests **🟢 PASS!**

---

### 🟢 Cycle 3: VIP Customer Receives 20% Discount

#### Step 3.1: 🔴 RED
```csharp
    [Fact]
    public void CalculateDiscount_ShouldReturn20Percent_ForVipCustomer()
    {
        // Arrange
        var calculator = new DiscountCalculator();

        // Act
        var discount = calculator.CalculateDiscount(CustomerMembership.Vip, orderAmount: 1000m);

        // Assert
        discount.Should().Be(0.20m);
    }
```
*Run tests:* **🔴 FAILS!**

---

#### Step 3.2: 🟢 GREEN & 🔵 REFACTOR
Refactor `DiscountCalculator.cs` with an expressive pattern matching `switch`:

```csharp
public class DiscountCalculator
{
    public decimal CalculateDiscount(CustomerMembership membership, decimal orderAmount)
    {
        return membership switch
        {
            CustomerMembership.Vip => 0.20m,
            CustomerMembership.Premium => 0.10m,
            _ => 0.0m
        };
    }
}
```
*Run tests:* All 3 tests **🟢 PASS!**

---

### 🟢 Cycle 4: Orders > 5,000,000 Receive 5% Bonus Discount

#### Step 4.1: 🔴 RED
```csharp
    [Fact]
    public void CalculateDiscount_ShouldAdd5PercentBonus_WhenOrderAmountIsGreaterThan5Million()
    {
        // Arrange
        var calculator = new DiscountCalculator();

        // Act - Standard customer with order of 6,000,000 receives 5% bonus (0 + 0.05)
        var discount = calculator.CalculateDiscount(CustomerMembership.Normal, orderAmount: 6_000_000m);

        // Assert
        discount.Should().Be(0.05m);
    }

    [Fact]
    public void CalculateDiscount_ShouldAdd5PercentBonusToVip_WhenOrderAmountIsGreaterThan5Million()
    {
        // Arrange
        var calculator = new DiscountCalculator();

        // Act - VIP (20%) + Large order bonus (5%) = 25%
        var discount = calculator.CalculateDiscount(CustomerMembership.Vip, orderAmount: 6_000_000m);

        // Assert
        discount.Should().Be(0.25m);
    }
```
*Run tests:* Both new tests **🔴 FAIL!**

---

#### Step 4.2: 🟢 GREEN
```csharp
public class DiscountCalculator
{
    private const decimal LargeOrderThreshold = 5_000_000m;
    private const decimal LargeOrderBonusDiscount = 0.05m;

    public decimal CalculateDiscount(CustomerMembership membership, decimal orderAmount)
    {
        var baseDiscount = membership switch
        {
            CustomerMembership.Vip => 0.20m,
            CustomerMembership.Premium => 0.10m,
            _ => 0.0m
        };

        if (orderAmount > LargeOrderThreshold)
        {
            baseDiscount += LargeOrderBonusDiscount;
        }

        return baseDiscount;
    }
}
```
*Run tests:* All 5 tests **🟢 PASS!**

---

### 🟢 Cycle 5: Maximum Cumulative Discount Capped at 30%

#### Step 5.1: 🔴 RED
```csharp
    [Fact]
    public void CalculateDiscount_ShouldCapDiscountAt30Percent_WhenTotalDiscountExceedsThreshold()
    {
        // VIP (0.20) + bonus (0.05) + extra coupon (0.15) = 0.40 => Must be capped at 0.30
        var calculator = new DiscountCalculator();

        var discount = calculator.CalculateDiscount(CustomerMembership.Vip, orderAmount: 6_000_000m, extraCouponDiscount: 0.15m);

        discount.Should().Be(0.30m);
    }
```

---

#### Step 5.2: 🟢 GREEN & 🔵 REFACTOR

Update `src/InsuranceQuoteEngine/Domain/Services/DiscountCalculator.cs`:

```csharp
namespace InsuranceQuoteEngine.Domain;

public class DiscountCalculator
{
    private const decimal LargeOrderThreshold = 5_000_000m;
    private const decimal LargeOrderBonusDiscount = 0.05m;
    private const decimal MaxDiscountCap = 0.30m; // 30% maximum cap

    public decimal CalculateDiscount(
        CustomerMembership membership, 
        decimal orderAmount, 
        decimal extraCouponDiscount = 0.0m)
    {
        if (orderAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(orderAmount), "Order amount cannot be negative.");
        }

        var totalDiscount = GetBaseMembershipDiscount(membership);

        if (orderAmount > LargeOrderThreshold)
        {
            totalDiscount += LargeOrderBonusDiscount;
        }

        totalDiscount += Math.Max(0m, extraCouponDiscount);

        // Apply maximum discount cap
        return Math.Min(totalDiscount, MaxDiscountCap);
    }

    private static decimal GetBaseMembershipDiscount(CustomerMembership membership) => membership switch
    {
        CustomerMembership.Vip => 0.20m,
        CustomerMembership.Premium => 0.10m,
        _ => 0.0m
    };
}
```

---

## 🎯 3. Practical TDD Takeaways

1. **Avoid Speculative Architecture (YAGNI):** Write code strictly in response to failing tests.
2. **Fearless Refactoring:** Comprehensive test coverage acts as a safety harness, enabling aggressive code cleanup and restructuring.
3. **Natural API Design:** You experience your classes from the consumer perspective before writing the implementation.

---

## ✅ Lesson 05 Completion Checklist
- [ ] Understand the Three Laws of TDD and the Red – Green – Refactor cycle.
- [ ] Practiced writing failing tests before producing implementation code.
- [ ] Completed the implementation of `DiscountCalculator` driven by tests.

👉 **Next Step:** Proceed to [Lesson 06: Advanced Unit Testing Techniques (Parameterized, Async, Time)](./06-advanced-unit-testing.md)!
