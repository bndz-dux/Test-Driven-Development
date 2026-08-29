# Lesson 02: Test Design & Test Quality

## 🎯 Lesson Objectives
- Learn to test for **business behavior** rather than internal implementation details.
- Master two classical test design techniques in Software Engineering:
  1. **Equivalence Partitioning (EP)**
  2. **Boundary Value Analysis (BVA)**
- Perform comprehensive testing: Happy Path, Failure Path, Null/Empty/Whitespace inputs, Out-of-range values, and Exceptions.
- Prevent brittle tests that break upon routine refactoring.
- **Hands-on:** Design a high-quality test suite for `CustomerEligibilityValidator`.

---

## 📖 1. Testing Behavior vs. Implementation Details

### ❌ Common Pitfall: Testing Implementation Details
When tests are tightly coupled to internal implementation details (e.g., verifying private variable names, checking whether a private helper method was invoked, or verifying specific internal list sorting calls):
- Routine code refactoring to optimize performance causes test failures despite functional correctness.
- Developers become reluctant to refactor code because any change breaks numerous fragile tests.

### ✅ Best Practice: Testing Observable Behavior
- Treat the module/class as a **black box** or deterministic system:
  - **Inputs / Initial State:** Arguments passed to methods or initial object state.
  - **Outputs / Observable State Mutations:** Return values, thrown exceptions, or verifiable state changes.

---

## 🔬 2. Equivalence Partitioning (EP) & Boundary Value Analysis (BVA)

### 2.1. Equivalence Partitioning (EP)
Divide the entire domain of input values into partitions where the system exhibits identical behavior. Selecting **one representative value** from each partition is sufficient to validate that partition, avoiding redundant tests.

**Example:** Insurance eligibility age rule:
- Eligible: Between 18 and 65 years old.
- Ineligible (too young): < 18 years old.
- Ineligible (too old): > 65 years old.

We identify three distinct partitions:
1. `[-∞, 17]`: Ineligible (Representative: `15`)
2. `[18, 65]`: Eligible (Representative: `30`)
3. `[66, +∞]`: Ineligible (Representative: `70`)

### 2.2. Boundary Value Analysis (BVA)
Defects cluster disproportionately around **boundary points** (often caused by off-by-one errors or mixing up `>`, `>=`, `<`, `<=`).

The 3-point boundary model (Off-boundary, On-boundary, In-boundary):
- For condition `age >= 18`:
  - **17** (Off-boundary: Just below threshold → False)
  - **18** (On-boundary: Exactly on threshold → True)
  - **19** (In-boundary: Just above threshold → True)

### Common Boundary Matrix:

| Data Type | Boundary Scenarios to Test |
| :--- | :--- |
| **Numbers (int, decimal)** | `0`, `1`, `-1`, `Min`, `Max`, values immediately before/after threshold conditions (`if x > 100` -> test `99`, `100`, `101`) |
| **Strings (string)** | `null`, `""` (empty), `" "` (whitespace), `"  \t\n  "`, 1-character strings, maximum length strings, strings exceeding length |
| **Collections (IEnumerable)** | `null`, empty (`Count = 0`), 1 element, multiple elements, duplicate items |
| **Time (DateTime)** | Past, present, future, leap year leap days, timezone offsets (UTC vs. Local) |

---

## 🛠️ 3. Step-by-Step Exercise: `CustomerEligibilityValidator`

We will build an eligibility validator for prospective insurance applicants.

### Business Requirements:
1. **Full Name:** Cannot be null, empty, or whitespace-only. Length must be between 2 and 100 characters.
2. **Applicant Age:** Must be between 18 and 65 years old (inclusive).
3. **Postal Code (ZipCode):** Must match a 5-digit numerical format (e.g., `"70000"`).

---

### Step 3.1: Implement Domain Models and Validator

Create file `src/InsuranceQuoteEngine/Domain/Services/CustomerEligibilityValidator.cs`:

```csharp
using System.Text.RegularExpressions;

namespace InsuranceQuoteEngine.Domain;

public record CustomerApplicant(string FullName, int Age, string PostalCode);

public class CustomerEligibilityValidator
{
    private static readonly Regex PostalCodeRegex = new(@"^\d{5}$", RegexOptions.Compiled);

    public ValidationResult Validate(CustomerApplicant applicant)
    {
        ArgumentNullException.ThrowIfNull(applicant);

        var errors = new List<string>();

        // 1. Validate full name
        if (string.IsNullOrWhiteSpace(applicant.FullName))
        {
            errors.Add("Full name is required.");
        }
        else if (applicant.FullName.Trim().Length < 2 || applicant.FullName.Trim().Length > 100)
        {
            errors.Add("Full name must be between 2 and 100 characters.");
        }

        // 2. Validate age (18 - 65)
        if (applicant.Age < 18)
        {
            errors.Add("Applicant must be at least 18 years old.");
        }
        else if (applicant.Age > 65)
        {
            errors.Add("Applicant cannot be older than 65 years old.");
        }

        // 3. Validate postal code
        if (string.IsNullOrWhiteSpace(applicant.PostalCode) || !PostalCodeRegex.IsMatch(applicant.PostalCode))
        {
            errors.Add("Postal code must be exactly 5 digits.");
        }

        return errors.Count == 0 
            ? ValidationResult.Success() 
            : ValidationResult.Failure(errors);
    }
}

public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public IReadOnlyList<string> Errors { get; }

    private ValidationResult(IEnumerable<string> errors)
    {
        Errors = errors.ToList().AsReadOnly();
    }

    public static ValidationResult Success() => new(Enumerable.Empty<string>());
    public static ValidationResult Failure(IEnumerable<string> errors) => new(errors);
}
```

---

### Step 3.2: Design the Test Matrix

Before writing tests, map out test scenarios using EP and BVA principles:

1. **Happy Path:**
   - Standard valid applicant (Name: "Nguyen Van A", Age: 30, PostalCode: "70000") → IsValid = true, no errors.
2. **Age Boundaries (BVA):**
   - Age 17 (below lower bound) → Fail ("Applicant must be at least 18 years old.")
   - Age 18 (on lower bound) → Pass
   - Age 19 (above lower bound) → Pass
   - Age 64 (below upper bound) → Pass
   - Age 65 (on upper bound) → Pass
   - Age 66 (above upper bound) → Fail ("Applicant cannot be older than 65 years old.")
3. **Name Boundaries (String & Length BVA):**
   - `null` → Fail
   - `""` → Fail
   - `"   "` → Fail
   - 1 character (`"A"`) → Fail
   - 2 characters (`"An"`) → Pass
   - 100 characters → Pass
   - 101 characters → Fail
4. **Postal Code Format:**
   - 4 digits (`"1234"`) → Fail
   - 5 digits (`"12345"`) → Pass
   - 6 digits (`"123456"`) → Fail
   - Contains letters (`"7000A"`) → Fail
   - Null or whitespace → Fail
5. **Guard Clauses / Exceptions:**
   - Passing `applicant = null` → Throws `ArgumentNullException`.

---

### Step 3.3: Write the Complete Test Suite

Create file `tests/InsuranceQuoteEngine.UnitTests/Domain/CustomerEligibilityValidatorTests.cs`:

```csharp
using FluentAssertions;
using InsuranceQuoteEngine.Domain;
using Xunit;

namespace InsuranceQuoteEngine.UnitTests.Domain;

public class CustomerEligibilityValidatorTests
{
    private readonly CustomerEligibilityValidator _sut = new();

    [Fact]
    public void Validate_ShouldReturnSuccess_WhenAllInputsAreValid()
    {
        // Arrange
        var applicant = new CustomerApplicant("Nguyen Van A", 30, "70000");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldThrowArgumentNullException_WhenApplicantIsNull()
    {
        // Act
        Action act = () => _sut.Validate(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #region Age Boundary Tests (BVA)

    [Fact]
    public void Validate_ShouldFail_WhenAgeIs17_JustBelowLowerBoundary()
    {
        // Arrange
        var applicant = new CustomerApplicant("Nguyen Van A", 17, "70000");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Applicant must be at least 18 years old.");
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenAgeIs18_ExactLowerBoundary()
    {
        // Arrange
        var applicant = new CustomerApplicant("Nguyen Van A", 18, "70000");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenAgeIs65_ExactUpperBoundary()
    {
        // Arrange
        var applicant = new CustomerApplicant("Nguyen Van A", 65, "70000");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenAgeIs66_JustAboveUpperBoundary()
    {
        // Arrange
        var applicant = new CustomerApplicant("Nguyen Van A", 66, "70000");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Applicant cannot be older than 65 years old.");
    }

    #endregion

    #region Name Boundary & Edge Cases

    [Fact]
    public void Validate_ShouldFail_WhenFullNameIsNull()
    {
        // Arrange
        var applicant = new CustomerApplicant(null!, 25, "70000");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Full name is required.");
    }

    [Fact]
    public void Validate_ShouldFail_WhenFullNameIsWhitespace()
    {
        // Arrange
        var applicant = new CustomerApplicant("   ", 25, "70000");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Full name is required.");
    }

    [Fact]
    public void Validate_ShouldFail_WhenFullNameIs1Character_BelowMinimumLength()
    {
        // Arrange
        var applicant = new CustomerApplicant("A", 25, "70000");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Full name must be between 2 and 100 characters.");
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenFullNameIs2Characters_ExactMinimumLength()
    {
        // Arrange
        var applicant = new CustomerApplicant("An", 25, "70000");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenFullNameIs100Characters_ExactMaximumLength()
    {
        // Arrange
        var maxName = new string('A', 100);
        var applicant = new CustomerApplicant(maxName, 25, "70000");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenFullNameIs101Characters_AboveMaximumLength()
    {
        // Arrange
        var tooLongName = new string('A', 101);
        var applicant = new CustomerApplicant(tooLongName, 25, "70000");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Full name must be between 2 and 100 characters.");
    }

    #endregion

    #region PostalCode Validation

    [Fact]
    public void Validate_ShouldFail_WhenPostalCodeHas4Digits()
    {
        // Arrange
        var applicant = new CustomerApplicant("Nguyen Van A", 25, "1234");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Postal code must be exactly 5 digits.");
    }

    [Fact]
    public void Validate_ShouldFail_WhenPostalCodeContainsLetters()
    {
        // Arrange
        var applicant = new CustomerApplicant("Nguyen Van A", 25, "7000A");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Postal code must be exactly 5 digits.");
    }

    #endregion
}
```

---

## 📝 4. 5 Essential Questions for Designing Test Cases

Whenever designing tests for a business method, ask these five questions:
1. **Happy Path:** What is the outcome for pristine, standard valid inputs?
2. **What Can Go Wrong (Failure Path):** What happens when users provide invalid data or operations fail?
3. **Boundaries:** Are there `>=`, `<=`, `Length`, or `Count` constraints? Have you tested `n - 1`, `n`, and `n + 1`?
4. **Invalid / Extreme Inputs:** Have you tested `null`, `""`, negative numbers, empty collections, or past/future timestamps?
5. **Business Invariants:** What core business rule must never be violated under any circumstance?

---

## ✅ Lesson 02 Completion Checklist
- [ ] Differentiate between behavior-driven testing and implementation testing.
- [ ] Map out equivalence partitions (EP) and identify boundary values (BVA).
- [ ] Write boundary cases for string constraints (null, empty, whitespace, min/max length).
- [ ] Execute and pass the complete test suite for `CustomerEligibilityValidator`.

👉 **Next Step:** Proceed to [Lesson 03: Mocking Strategies & Isolating Dependencies with Moq](./03-mocking-strategies.md)!
