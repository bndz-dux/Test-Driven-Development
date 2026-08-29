# Lesson 11: Test Smells & Test Refactoring

## 🎯 Lesson Objectives
- Identify the **10 Most Dangerous Test Smells** that turn test suites into maintenance liabilities.
- Master test refactoring techniques to treat test code with the same engineering rigor as production code.
- Transform verbose, brittle, order-dependent tests into expressive tests that can be understood in 10 seconds and withstand routine refactoring.
- Apply the **5 Golden Review Questions** when reviewing unit test code.
- Conclude and review the entire TDD Mastery curriculum.

---

## 👃 1. 10 Common Test Smells & Refactoring Strategies

### 1.1. Smell 1: The Mystery Guest (Magic Values)
- **Symptom:** Tests rely on unexplained magic literals (e.g. `42`, `"X"`, `ID: 99`) or implicit database seeds without context.
- **Problem:** Readers cannot discern what triggers the expected outcome.
- **Remedy:** Define explicit domain constants or use Test Data Builders with self-documenting parameters:
  ```csharp
  // ❌ Mystery Guest
  var result = calculator.Calculate(100, 42); // What does 42 represent?
  
  // ✅ Explicit & Expressive
  const decimal highRiskCustomerScore = 42m;
  var result = calculator.Calculate(originalPrice: 100m, riskScore: highRiskCustomerScore);
  ```

---

### 1.2. Smell 2: Logic in Tests (Conditional Branching in Tests)
- **Symptom:** Including `for` loops, `if...else` statements, `switch`, or manual `try...catch` blocks within test methods.
- **Problem:** Tests become complex and may conceal bugs within the test code itself.
- **Remedy:** Remove all branching logic. Split into separate, focused test methods or use `[Theory]` with `[InlineData]`.

---

### 1.3. Smell 3: Over-Mocking & Implementation Detail Verification
- **Symptom:** Mocking internal domain classes, or using `Verify` to assert the internal sequence of private/internal method invocations.
- **Problem:** Produces brittle tests that break upon routine internal refactoring even when functional behavior remains correct.
- **Remedy:** Only mock out-of-process boundaries (Database, external APIs). Verify observable outputs and state mutations rather than internal control flow.

---

### 1.4. Smell 4: Flaky Tests
- **Symptom:** Tests intermittently pass and fail without code changes. Often caused by ambient calls to `DateTime.Now`, `Random`, `Thread.Sleep()`, or network latency.
- **Remedy:** Replace `DateTime.Now` with `IClock` / `TimeProvider`. Avoid `Thread.Sleep()`; use deterministic asynchronous synchronization.

---

### 1.5. Smell 5: Interdependent Tests (Order-Dependent Execution)
- **Symptom:** Test B only passes if Test A executed first (due to static state, lingering temporary files, or uncleaned database records).
- **Remedy:** Ensure each test is completely isolated. Re-instantiate the SUT in the constructor or use clean fixture instances per test.

---

### 1.6. Smell 6: Assert Roulette
- **Symptom:** A single test method executes 20 consecutive assertions across unrelated behaviors. When an early assertion fails, subsequent assertions are aborted, hiding the full failure context.
- **Remedy:** Focus each test on a **single unit of observable behavior**. When verifying multiple fields of the same returned object, use FluentAssertions `using (new AssertionScope())`:
  ```csharp
  using (new AssertionScope())
  {
      quote.Status.Should().Be(QuoteStatus.Approved);
      quote.BasePremium.Should().Be(1000m);
      quote.FinalPremium.Should().Be(800m);
  }
  ```

---

## 🛠️ 2. Hands-on Refactoring: Before vs. After

### ❌ Legacy Test Code (Riddled with Test Smells):
```csharp
[Fact]
public void Test1()
{
    // Smells: Cryptic name, magic numbers, object clutter, logic branching in test
    var c = new Customer { Id = Guid.NewGuid(), Name = "A", Email = "a@b.com", Address = "xyz", Age = 30, Status = CustomerStatus.Active };
    var p = new Property { Id = Guid.NewGuid(), Address = "HN", Value = 1000000000m, Year = 2010, InFlood = false };
    
    var repoMock = new Mock<ICustomerRepository>();
    repoMock.Setup(r => r.Get(c.Id)).ReturnsAsync(c);
    var emailMock = new Mock<IEmailSender>();

    var service = new LegacyQuoteService(repoMock.Object, emailMock.Object);
    var res = service.Process(c.Id, p, 1);

    if (res.Amount > 1000)
    {
        Assert.True(res.Success);
    }
    
    repoMock.Verify(r => r.Get(c.Id), Times.Once);
    emailMock.Verify(e => e.Send(It.IsAny<string>()), Times.Once);
}
```

---

### ✅ Clean, Refactored Test Code:
```csharp
[Fact]
public async Task ProcessQuoteAsync_ShouldApproveAndCalculatePremium_WhenCustomerAndPropertyAreStandard()
{
    // Arrange: Expressive builder pattern isolating test-relevant parameters
    var customer = new CustomerProfileBuilder()
        .WithAge(30)
        .Build();

    var property = new PropertyDetailsBuilder()
        .WithEstimatedValue(1_000_000_000m)
        .Build();

    var customerRepoMock = new Mock<ICustomerRepository>();
    customerRepoMock
        .Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(customer);

    var sut = new QuoteApplicationService(customerRepoMock.Object);

    // Act: Clear single action
    var result = await sut.ProcessQuoteAsync(customer.Id, property, CoverageTier.Basic);

    // Assert: Expressive, cohesive assertion scope with zero branching
    using (new AssertionScope())
    {
        result.Status.Should().Be(QuoteStatus.Approved);
        result.FinalPremium.Should().Be(1_000_000m);
    }
}
```

---

## 🔍 3. The 5 Golden Review Questions for Unit Tests

Before approving any test PR, evaluate against these five questions:

```text
1. ⏱️ 10-SECOND RULE: Can a new engineer understand the test's intent and expectations within 10 seconds?
2. 📖 BUSINESS DOCUMENTATION: Do the test name and AAA phases accurately describe business requirements?
3. 🎲 DETERMINISTIC: Does this test produce identical results across all execution environments?
4. 🎯 RIGHT REASON: When this test fails, does it accurately signal broken business behavior?
5. 🛡️ REFACTOR RESILIENT: Will this test remain green during internal algorithm refactoring if external behavior is unchanged?
```

---

## 🏆 4. Definition of Done Checklist

Congratulations on completing all 12 lessons of the TDD Mastery curriculum! Review your mastery against this checklist:

- [x] **xUnit & AAA:** Mastered Arrange – Act – Assert patterns, `[Fact]`, and `[Theory]`.
- [x] **Naming Standards:** 100% adherence to `Method_ShouldExpectedBehavior_WhenCondition`.
- [x] **Boundary Analysis:** Consistent application of Equivalence Partitioning and Boundary Value Analysis.
- [x] **Moq Mastery:** Isolated I/O boundaries while eliminating over-mocking.
- [x] **Test Data Builders:** Clean builder libraries eliminating test clutter.
- [x] **TDD Cycle:** Intuitive RED → GREEN → REFACTOR discipline.
- [x] **Time Control:** Deterministic time manipulation via `IClock` / `TimeProvider`.
- [x] **Mutation Testing:** Validated test suite sensitivity using Stryker.NET (Score ≥ 80%).
- [x] **Code Coverage:** Measured via Coverlet & ReportGenerator (Line ≥ 80%, Branch ≥ 75%).
- [x] **CI/CD Quality Gates:** Enforced quality standards in automated GitHub Actions / Azure DevOps pipelines.
- [x] **Clean Test Code:** Proactively detected and eliminated test smells.

---

> 🚀 **Closing Thought:**  
> *"The ultimate goal of TDD and Unit Testing is not to accumulate test volume for compliance.  
> The goal is to build a reliable safety harness empowering you and your team to **fearlessly innovate, refactor, and deliver software** with confidence!"*
