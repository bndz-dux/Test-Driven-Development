# Unit Testing & Test-Driven Development (TDD) Mastery Course

Welcome to the comprehensive hands-on roadmap for **Unit Testing** and **Test-Driven Development (TDD)** on the **.NET / C#** platform.

This curriculum is structured into **12 detailed, step-by-step lessons**, guiding you from fundamental testing principles to advanced mocking, practical TDD on real-world domain architectures, Mutation Testing, Code Coverage analysis, and CI/CD Quality Gates.

---

## 🎯 Outcomes

Upon completing this curriculum, you will master:
- [x] Designing unit tests around **observable behavior** and **business rules** rather than implementation details.
- [x] Consistently applying the **Arrange – Act – Assert (AAA)** pattern and clean test naming conventions.
- [x] Proficiently utilizing **xUnit**, **FluentAssertions**, and **Moq** (while avoiding over-mocking).
- [x] Designing and reusing **Test Data Builders** to keep test suites clean and maintainable.
- [x] Mastering the **RED → GREEN → REFACTOR** cycle across realistic domain features.
- [x] Confidently testing complex scenarios: asynchronous operations, exceptions, time dependencies (`TimeProvider` / `IClock`), boundary conditions, and edge cases.
- [x] Measuring true test effectiveness using **Mutation Testing (Stryker.NET)**.
- [x] Measuring, analyzing, and enforcing **Code Coverage thresholds (Coverlet & ReportGenerator)**.
- [x] Setting up **Automated Quality Gates** in CI/CD pipelines (GitHub Actions / Azure DevOps).
- [x] Detecting, analyzing, and refactoring dangerous **Test Smells**.

---

## 🛠️ Technology Stack & Tools

- **Language / Runtime:** .NET 8 / C# 12 (or .NET 9)
- **Test Framework:** xUnit
- **Assertion Library:** FluentAssertions
- **Mocking Framework:** Moq
- **Code Coverage:** Coverlet & ReportGenerator
- **Mutation Testing:** Stryker.NET
- **CI/CD:** GitHub Actions / Azure DevOps

---

## 📚 Guidance

| Lesson | Topic | Summary | Link |
| :--- | :--- | :--- | :--- |
| **Lesson 00** | **Environment & Project Setup** | Initialize solution, establish Domain/Tests folder structure, configure essential packages | [View Lesson](./lessons/00-introduction-and-setup.md) |
| **Lesson 01** | **Unit Testing Fundamentals** | Unit testing concepts, AAA pattern, naming conventions, `[Fact]`, FluentAssertions, `PriceCalculator` exercise | [View Lesson](./lessons/01-unit-testing-fundamentals.md) |
| **Lesson 02** | **Test Design & Test Quality** | Equivalence Partitioning (EP), Boundary Value Analysis (BVA), Happy/Failure paths, Exception testing | [View Lesson](./lessons/02-test-design-and-quality.md) |
| **Lesson 03** | **Mocking Strategies with Moq** | Stubs vs Mocks, dependency isolation, Setup, Verify, Async Mocking, avoiding Over-Mocking with `OrderService` | [View Lesson](./lessons/03-mocking-strategies.md) |
| **Lesson 04** | **Test Data Builders & Fixtures** | Overcoming Object Mother & Test Clutter with the Builder Pattern, sharing fixtures | [View Lesson](./lessons/04-test-data-builders.md) |
| **Lesson 05** | **TDD Core Workflow (Red-Green-Refactor)** | Three Laws of TDD, step-by-step TDD cycle, building an Order & Discount Engine test-first | [View Lesson](./lessons/05-tdd-core-workflow.md) |
| **Lesson 06** | **Advanced Unit Testing Techniques** | Parameterized tests (`[Theory]`, `[InlineData]`, `[MemberData]`, `[ClassData]`), handling time (`IClock`/`TimeProvider`) | [View Lesson](./lessons/06-advanced-unit-testing.md) |
| **Lesson 07** | **Capstone Project: Insurance Quote Engine** | Practical TDD building a full-fledged insurance quoting engine: Risk, Premium, Discounts, Referral/Decline | [View Lesson](./lessons/07-insurance-quote-engine-capstone.md) |
| **Lesson 08** | **Mutation Testing with Stryker.NET** | Mutants, Mutant Killed vs Survived, Stryker.NET configuration, report analysis, and killing surviving mutants | [View Lesson](./lessons/08-mutation-testing-stryker.md) |
| **Lesson 09** | **Code Coverage (Coverlet & ReportGenerator)** | Line / Branch / Method coverage metrics, generating `coverage.cobertura.xml`, HTML report visualization | [View Lesson](./lessons/09-code-coverage-coverlet.md) |
| **Lesson 10** | **Automated CI Quality Gates** | Integrating Build, Test, Coverage Gate (>=80%), and Stryker Gate into GitHub Actions / Azure DevOps | [View Lesson](./lessons/10-ci-quality-gates.md) |
| **Lesson 11** | **Test Smells & Test Refactoring** | Identifying 10 common test smells (Flaky, Fragile, Mystery Guest, etc.) with actionable refactoring recipes | [View Lesson](./lessons/11-test-refactoring-and-smells.md) |

---

## 🏁 Definition of Done

A developer has mastered TDD when receiving a new business requirement and reliably:
1. Decomposing the requirement into business rules and acceptance criteria.
2. Designing comprehensive test cases (Happy path, Edge cases, Failure path).
3. Writing a failing test (RED) → Writing minimal production code (GREEN) → Optimizing design (REFACTOR).
4. Ensuring the entire test suite is green, deterministic (zero flakiness), and executes quickly (< few seconds).
5. Validating Code Coverage (Line >= 80%, Branch >= 75%) and Mutation Score (>= 80%).
6. Opening a Pull Request and confidently passing all Automated Quality Gates in CI!
