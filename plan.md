# Unit Testing & TDD Mastery Plan

## 1. Goal

Build strong practical skills in .NET unit testing and test-driven development.

By the end of this plan, I should be able to:

- Write maintainable unit tests with xUnit.
- Apply Arrange → Act → Assert consistently.
- Design tests around behavior and business rules.
- Identify good unit-test boundaries.
- Use FluentAssertions effectively.
- Use Moq without over-mocking.
- Build reusable Test Data Builders.
- Write parameterized and edge-case tests.
- Apply TDD using RED → GREEN → REFACTOR.
- Test asynchronous code, exceptions, time, and external dependencies.
- Use mutation testing to measure test effectiveness.
- Configure code-coverage thresholds.
- Add automated quality gates to CI.
- Understand why code coverage alone does not equal test quality.

---

# 2. Technology Stack

Use the following stack for the practical exercises:

- .NET / C#
- xUnit
- FluentAssertions
- Moq
- Coverlet
- ReportGenerator
- Stryker.NET
- GitHub Actions or Azure DevOps Pipelines

Recommended project structure:

```text
src/
└── InsuranceQuoteEngine/
    ├── Domain/
    ├── Application/
    ├── Infrastructure/
    └── ...

tests/
├── InsuranceQuoteEngine.UnitTests/
│   ├── Domain/
│   ├── Application/
│   ├── Builders/
│   └── Fixtures/
│
└── InsuranceQuoteEngine.IntegrationTests/
```

---

# 3. Learning Roadmap

## Phase 1 — Unit Testing Fundamentals

### Objectives

Understand what a unit test is and how to write a clean, deterministic test.

### Topics

- What is a unit?
- Unit test vs integration test vs end-to-end test.
- Test isolation.
- Deterministic tests.
- AAA pattern:
  - Arrange
  - Act
  - Assert
- Assertions.
- Test naming.
- xUnit `[Fact]`.
- xUnit `[Theory]`.
- Test fixtures.
- Basic async tests.

### Test naming convention

Use:

```text
Method_ShouldExpectedBehavior_WhenCondition
```

Examples:

```text
CreateOrder_ShouldThrow_WhenCustomerDoesNotExist

CalculatePremium_ShouldReturnZero_WhenRiskScoreIsZero

SubmitPayment_ShouldNotCreatePolicy_WhenPaymentFails
```

Avoid:

```text
Test1
ShouldWork
CreateOrderTest
TestCreateOrder
```

### Exercise

Create a simple `PriceCalculator`.

Requirements:

```text
Calculate(100, 0)       → 100
Calculate(100, 0.2)     → 80
Calculate(100, 1)       → 0
Calculate(0, 0.2)       → 0
Calculate(-100, 0.2)    → Define expected business behavior
```

### Deliverable

- 10–15 unit tests.
- Meaningful test names.
- AAA structure.
- Edge-case tests.

---

# 4. Phase 2 — Test Design & Test Quality

## Objectives

Learn how to test behavior rather than implementation details.

### Topics

- Testing business behavior.
- Happy path.
- Failure path.
- Boundary testing.
- Equivalence partitions.
- Null / empty / whitespace inputs.
- Invalid values.
- Exception testing.
- Testing side effects.
- Avoiding brittle tests.
- Avoiding implementation-detail assertions.

### Boundary testing

For:

```csharp
if (age >= 18)
```

Test:

```text
17
18
19
```

For collections:

```text
0 items
1 item
many items
```

For strings:

```text
null
empty
whitespace
normal value
very long value
```

### Exercise

Take the tests from Phase 1 and review them.

For every method, ask:

1. What is the happy path?
2. What can go wrong?
3. What are the boundaries?
4. What invalid inputs are possible?
5. What business rules must remain true?

### Deliverable

Improve the existing tests instead of simply adding more tests.

Target:

- Better coverage of behavior.
- Better boundary coverage.
- Fewer redundant tests.
- Clearer test names.

---

# 5. Phase 3 — Mocking Strategies

## Objectives

Learn when to mock dependencies and when not to.

### Topics

- Why mock?
- Stub vs mock.
- Dependency isolation.
- `Setup`.
- `Verify`.
- `Times.Once`.
- `Times.Never`.
- Argument matching.
- Async mocking.
- Exception mocking.
- Avoiding over-mocking.
- Mocking repositories.
- Mocking external services.

### Example

Production code:

```csharp
public class OrderService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IPaymentService _paymentService;

    public OrderService(
        ICustomerRepository customerRepository,
        IPaymentService paymentService)
    {
        _customerRepository = customerRepository;
        _paymentService = paymentService;
    }

    public async Task CreateOrder(Order order)
    {
        var customer =
            await _customerRepository.Get(order.CustomerId);

        if (customer == null)
            throw new CustomerNotFoundException();

        await _paymentService.Pay(order.Amount);
    }
}
```

Test scenarios:

```text
Customer exists
Customer does not exist
Payment succeeds
Payment fails
Payment should not execute when customer does not exist
```

### Mocking checklist

Practice:

```text
[ ] Setup return value
[ ] Setup async return value
[ ] Setup exception
[ ] Verify method called once
[ ] Verify method never called
[ ] Verify arguments
[ ] Verify behavior instead of implementation details
```

### Important rule

Do not automatically mock everything.

Prefer:

```text
Mock dependencies
Test the system under test
```

Avoid:

```text
Mock every class
Verify every internal method call
Test implementation details
```

### Deliverable

Build 15–20 tests around `OrderService`.

---

# 6. Phase 4 — Test Data Builders

## Objectives

Make tests readable and reduce duplicated test setup.

### Problem

Avoid repeatedly creating large objects:

```csharp
var customer = new Customer
{
    Id = Guid.NewGuid(),
    Name = "John",
    Email = "john@test.com",
    Address = "...",
    Phone = "...",
    Status = CustomerStatus.Active
};
```

### Builder approach

```csharp
var customer = new CustomerBuilder()
    .WithName("John")
    .WithStatus(CustomerStatus.Active)
    .Build();
```

### Builder responsibilities

A builder should:

- Provide valid defaults.
- Allow relevant properties to be overridden.
- Keep test setup readable.
- Avoid unnecessary configuration.

### Builders to create

```text
CustomerBuilder
ProductBuilder
OrderBuilder
OrderItemBuilder
QuoteBuilder
RiskBuilder
CoverageBuilder
```

### Exercise

Refactor existing tests to use builders.

Before:

```text
Large object setup
```

After:

```text
Only business-relevant values
```

### Deliverable

A reusable test-data builder library for the project.

---

# 7. Phase 5 — TDD

## Objectives

Learn to implement a feature by writing tests first.

### TDD cycle

```text
RED
 ↓
Write failing test
 ↓
GREEN
 ↓
Write minimum implementation
 ↓
REFACTOR
 ↓
Repeat
```

### Rules

During TDD:

1. Start from a business requirement.
2. Write one failing test.
3. Write the minimum production code.
4. Make the test pass.
5. Refactor.
6. Add the next behavior.
7. Repeat.

### Example feature

Requirement:

```text
Normal customer → 0% discount
Premium customer → 10%
VIP customer → 20%
```

Start with:

```csharp
[Fact]
public void CalculateDiscount_ShouldReturn10Percent_ForPremiumCustomer()
```

Make it fail.

Implement the minimum code.

Make it pass.

Then add:

```csharp
CalculateDiscount_ShouldReturn20Percent_ForVipCustomer
```

Then:

```csharp
CalculateDiscount_ShouldReturnZero_ForNormalCustomer
```

### TDD exercise

Implement these features entirely test-first:

```text
1. Create Order
2. Calculate Order Total
3. Apply Discount
4. Premium Customer Discount
5. VIP Customer Discount
6. Validate Inventory
7. Process Payment
8. Cancel Order
```

### Deliverable

For each feature, keep the development history showing:

```text
RED → GREEN → REFACTOR
```

---

# 8. Phase 6 — Advanced Unit Testing

## Objectives

Handle realistic application scenarios.

### Topics

- Parameterized tests.
- `[Theory]`.
- `[InlineData]`.
- `MemberData`.
- `ClassData`.
- Custom test data.
- Shared fixtures.
- Async tests.
- Exception testing.
- Time-dependent logic.
- Randomness.
- External dependencies.
- Retry logic.
- Idempotency.

### Parameterized test example

```csharp
[Theory]
[InlineData(100, 0.2, 80)]
[InlineData(200, 0.2, 160)]
[InlineData(500, 0.2, 400)]
public void Calculate_ShouldApplyDiscount(
    decimal price,
    decimal discount,
    decimal expected)
{
    var result =
        calculator.Calculate(price, discount);

    result.Should().Be(expected);
}
```

### Testing time

Avoid:

```csharp
DateTime.UtcNow
```

directly inside business logic.

Use an abstraction:

```csharp
public interface IClock
{
    DateTime UtcNow { get; }
}
```

Then mock the clock in tests.

### Exercise

Implement and test:

```text
Quote expiration
Payment timeout
Policy effective date
Policy cancellation date
```

All tests must be deterministic.

---

# 9. Phase 7 — Insurance Quote Engine

## Objectives

Use everything learned so far in a realistic business domain.

Build a small insurance quote engine.

### Domain

```text
Quote
 ├── Customer
 ├── Property
 ├── Risk
 ├── Coverage
 ├── Premium
 └── Referral / Decline
```

### Quote flow

```text
Address validation
       ↓
Risk validation
       ↓
Base premium calculation
       ↓
Customer factors
       ↓
Coverage calculation
       ↓
Discounts
       ↓
Referral check
       ↓
Decline check
       ↓
Generate Quote
```

### Business rules

Start with simple rules.

Example:

```text
Invalid address → Decline
High-risk property → Refer
Standard risk → Calculate premium
Premium customer → Discount
VIP customer → Higher discount
Unsupported coverage → Reject
```

### TDD requirement

Every business rule must be implemented using:

```text
Requirement
    ↓
Failing test
    ↓
Minimum implementation
    ↓
Refactor
```

### Deliverable

A working quote engine with a strong unit-test suite.

---

# 10. Phase 8 — Mutation Testing

## Objectives

Measure whether tests can actually detect defects.

### Core concept

Normal testing asks:

```text
Does the code pass my tests?
```

Mutation testing asks:

```text
Would my tests detect if the code were wrong?
```

### Example

Original:

```csharp
if (age >= 18)
{
    return true;
}
```

Mutation:

```csharp
if (age > 18)
{
    return true;
}
```

If tests fail:

```text
Mutation killed
```

Good.

If tests still pass:

```text
Mutation survived
```

The test suite needs investigation.

### Tool

Use:

```text
Stryker.NET
```

### Mutation targets

Run mutation testing against:

```text
Domain logic
Premium calculation
Discount calculation
Risk rules
Referral rules
Decline rules
```

### Target

Initial goal:

```text
80%+ mutation score
```

Then improve toward:

```text
85–90%+
```

Do not blindly target 100%.

Some mutations may be equivalent or irrelevant.

### Deliverable

Generate mutation reports and investigate surviving mutations.

---

# 11. Phase 9 — Code Coverage

## Objectives

Measure which parts of the code are executed by tests.

### Understand

```text
Line coverage
Statement coverage
Method coverage
Branch coverage
```

### Important principle

```text
Coverage percentage != test quality
```

Example:

```text
100% line coverage
```

does not necessarily mean:

```text
100% business behavior coverage
```

### Tools

Use:

```text
Coverlet
ReportGenerator
```

### Initial targets

```text
Line coverage    ≥ 80%
Branch coverage  ≥ 75%
```

Treat these as starting thresholds, not universal standards.

### Deliverable

Generate an HTML coverage report.

Review:

- Uncovered lines.
- Uncovered branches.
- Business-critical logic.
- Low-value code that should not be tested just to increase the percentage.

---

# 12. Phase 10 — CI Quality Gates

## Objectives

Prevent low-quality changes from being merged.

### Pipeline

```text
Pull Request
      ↓
Restore
      ↓
Build
      ↓
Unit Tests
      ↓
Coverage
      ↓
Mutation Testing
      ↓
Integration Tests
      ↓
Build Artifact
```

### Quality gates

Start with:

```text
Unit tests       → Must pass
Line coverage    → ≥ 80%
Branch coverage  → ≥ 75%
Mutation score   → ≥ 80%
```

### Pipeline behavior

```text
Tests fail
    → Pipeline fails

Coverage below threshold
    → Pipeline fails

Mutation score below threshold
    → Pipeline fails
```

### Deliverable

Create CI configuration that automatically validates every Pull Request.

---

# 13. Phase 11 — Test Refactoring

## Objectives

Learn how to maintain tests as the codebase grows.

### Smells to identify

```text
Huge test methods
Duplicated setup
Too many mocks
Too many Verify calls
Tests depending on execution order
Random test failures
Tests using current time
Tests relying on external systems
Tests with unclear assertions
Tests testing implementation details
```

### Refactoring techniques

```text
Extract Builder
Extract Fixture
Parameterized tests
Shared setup
Better naming
Reduce unnecessary mocks
Test behavior instead of internals
```

### Exercise

Take 20 existing tests and refactor them.

For every test ask:

```text
Can I understand the purpose in 10 seconds?
Does the test explain the business rule?
Is it deterministic?
Does it fail for the right reason?
Would a refactor unnecessarily break this test?
```

---

# 14. Weekly Schedule

## Week 1 — Fundamentals

```text
Day 1: Unit testing concepts
Day 2: xUnit + AAA
Day 3: Assertions + FluentAssertions
Day 4: Test naming
Day 5: Edge cases
Day 6: Parameterized tests
Day 7: Review + refactor
```

Goal:

```text
30–40 quality unit tests
```

---

## Week 2 — Test Design

```text
Day 1: Happy/failure paths
Day 2: Boundary testing
Day 3: Null/empty/invalid input
Day 4: Exception testing
Day 5: Behavior vs implementation
Day 6: Refactor existing tests
Day 7: Review
```

Goal:

```text
Improve test quality rather than simply increase test count.
```

---

## Week 3 — Mocking

```text
Day 1: Dependency isolation
Day 2: Moq Setup
Day 3: Moq Verify
Day 4: Argument matching
Day 5: Async + exception mocking
Day 6: Avoid over-mocking
Day 7: Review
```

Goal:

```text
15–20 dependency-focused unit tests
```

---

## Week 4 — Builders & Advanced Testing

```text
Day 1: Test Data Builder
Day 2: Customer/Product builders
Day 3: Order builders
Day 4: Fixtures
Day 5: Advanced parameterized tests
Day 6: Testing time
Day 7: Refactor
```

Goal:

```text
Reusable test infrastructure
```

---

## Week 5 — TDD

Implement the Insurance Quote Engine using TDD.

```text
Day 1: Create Quote
Day 2: Premium calculation
Day 3: Discounts
Day 4: Risk rules
Day 5: Referral rules
Day 6: Decline rules
Day 7: Refactor + review
```

Goal:

```text
Every feature follows RED → GREEN → REFACTOR.
```

---

## Week 6 — Mutation + CI

```text
Day 1: Coverlet
Day 2: Coverage reports
Day 3: Coverage thresholds
Day 4: Stryker.NET
Day 5: Mutation analysis
Day 6: CI quality gates
Day 7: Final review
```

Goal:

```text
Automated test-quality pipeline
```

---

# 15. Final CI Architecture

```text
                    Pull Request
                         │
                         ▼
                    ┌─────────┐
                    │  Build  │
                    └────┬────┘
                         │
                         ▼
                 ┌───────────────┐
                 │  Unit Tests   │
                 └───────┬───────┘
                         │
                    PASS │
                         ▼
                 ┌───────────────┐
                 │    Coverage   │
                 └───────┬───────┘
                         │
                  ≥ 80%  │
                         ▼
              ┌────────────────────┐
              │ Mutation Testing   │
              └──────────┬─────────┘
                         │
                  ≥ 80%  │
                         ▼
              ┌────────────────────┐
              │ Integration Tests  │
              └──────────┬─────────┘
                         │
                         ▼
                 ┌─────────────┐
                 │   APPROVED  │
                 └─────────────┘
```

---

# 16. Definition of Done

The project is complete when:

```text
[ ] Unit tests use xUnit.
[ ] Tests follow AAA where appropriate.
[ ] Test names describe behavior.
[ ] Happy paths are covered.
[ ] Failure paths are covered.
[ ] Boundary cases are covered.
[ ] Parameterized tests are used where appropriate.
[ ] External dependencies are isolated.
[ ] Mocking is used selectively.
[ ] Test Data Builders exist for complex entities.
[ ] Tests are deterministic.
[ ] Time-dependent logic can be controlled.
[ ] TDD was used for new features.
[ ] RED → GREEN → REFACTOR can be demonstrated.
[ ] Coverage is measured automatically.
[ ] Coverage gates exist in CI.
[ ] Mutation testing is configured.
[ ] Mutation score has an agreed threshold.
[ ] CI fails when quality gates are violated.
[ ] Test reports are available from CI.
[ ] Tests have been refactored for maintainability.
```

---

# 17. Final Success Criteria

At the end of the project, I should be able to receive a requirement such as:

> "Add a new premium calculation rule."

and naturally follow:

```text
Understand requirement
        ↓
Identify business rules
        ↓
Design test cases
        ↓
Write failing test
        ↓
Implement minimum code
        ↓
Make test pass
        ↓
Refactor
        ↓
Add edge cases
        ↓
Run full test suite
        ↓
Check coverage
        ↓
Check mutation score
        ↓
Push PR
        ↓
CI quality gates
```

The final goal is not simply:

```text
"Write more tests."
```

The goal is:

```text
Write tests that make the codebase safer to change.
```
