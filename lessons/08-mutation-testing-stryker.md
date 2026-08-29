# Lesson 08: Mutation Testing with Stryker.NET

## 🎯 Lesson Objectives
- Understand the testing paradox: **100% Code Coverage does not guarantee high test quality**.
- Master the principles of **Mutation Testing**:
  - What are Mutants?
  - **Killed Mutants** (defect successfully detected by tests - Good) vs. **Survived Mutants** (defect undetected - Gap in test suite).
  - Calculating the **Mutation Score**.
- Install, configure, and execute **Stryker.NET** for .NET solutions.
- Analyze interactive HTML reports, locate surviving mutants, and write targeted assertions to eliminate them.
- Establish an industry-standard Mutation Score threshold of **≥ 80%**.

---

## 🤯 1. Why Code Coverage Can Be Deceptive

Consider the following method:

```csharp
public bool IsAdult(int age)
{
    return age >= 18;
}
```

And a poorly asserted test:
```csharp
[Fact]
public void IsAdult_Test()
{
    var result = IsAdult(25); // Executes the line => 100% Code Coverage!
    // But contains NO assertions, or a trivial assertion like: result.Should().NotBeNull();
}
```
Although coverage tools report **100% Line Coverage**, if someone alters the code to `return false;`, this test will still pass.

> **Mutation Testing asks the reverse question:**  
> *"If the production code is deliberately mutated to introduce artificial defects, will your test suite catch the bugs and report a RED failure?"*

---

## 🧬 2. How Stryker.NET Works

Stryker scans your C# syntax tree and injects artificial code modifications (**mutants**):

| Original Source Code | Mutated Code (Mutant) | Mutation Type |
| :--- | :--- | :--- |
| `if (age >= 18)` | `if (age > 18)` | Binary Expression Mutation (BVA) |
| `if (age >= 18)` | `if (age <= 18)` | Equality Mutation |
| `return price - discount;` | `return price + discount;` | Arithmetic Mutation |
| `if (hasClaims)` | `if (!hasClaims)` | Boolean Mutation |
| `discount = 0.20m;` | `discount = 0.0m;` | Constant Mutation |
| `_repo.Save(order);` | *(Remove this method call)* | Statement Removal Mutation |

After creating mutants:
1. Stryker re-runs relevant unit tests against each mutation.
2. **If the test FAILS (RED):** 👉 **MUTANT KILLED (Success!)** → The test suite is sensitive and caught the bug.
3. **If tests PASS (GREEN):** 👉 **MUTANT SURVIVED (Failure!)** → The test suite missed the mutation due to missing edge cases or weak assertions.

$$\text{Mutation Score} = \left( \frac{\text{Killed Mutants} + \text{Timeout Mutants}}{\text{Total Mutants}} \right) \times 100\%$$

---

## 🛠️ 3. Step-by-Step Exercise: Installing & Running Stryker.NET

### Step 3.1: Install Global Tool `dotnet-stryker`

Open your terminal and run:
```bash
dotnet tool install -g dotnet-stryker
```
*(If already installed, update via: `dotnet tool update -g dotnet-stryker`)*

---

### Step 3.2: Create Configuration File `stryker-config.json`

Create file `tests/InsuranceQuoteEngine.UnitTests/stryker-config.json`:

```json
{
  "stryker-config": {
    "project": "InsuranceQuoteEngine.csproj",
    "reporters": [
      "progress",
      "html",
      "cleartext"
    ],
    "thresholds": {
      "high": 85,
      "low": 80,
      "break": 70
    },
    "mutate": [
      "Domain/**/*.cs",
      "!Domain/Exceptions/**/*.cs"
    ]
  }
}
```

**Configuration Breakdown:**
- `"project"`: Specifies the target class library project under test.
- `"reporters"`: Outputs progress to the terminal and generates an interactive HTML report.
- `"thresholds"`: Enforces quality gates (`high >= low >= break`).
  - `high: 85`: High-quality target.
  - `low: 80`: Warning threshold.
  - `break: 70`: Pipeline break threshold. If Mutation Score drops below 70%, Stryker exits with a non-zero exit code to fail CI.
- `"mutate"`: Restricts mutation analysis to domain logic while excluding boilerplate exceptions.

---

### Step 3.3: Execute Stryker.NET

Navigate to the test directory and execute:

```bash
cd tests/InsuranceQuoteEngine.UnitTests
dotnet stryker
```

Stryker will:
1. Compile the solution.
2. Run baseline unit tests.
3. Inject mutations and run impacted tests in parallel.
4. Output the Mutation Score and write the HTML report to `StrykerOutput/.../reports/mutation-report.html`.

---

## 🔍 4. Analyzing Reports and Killing Surviving Mutants

Suppose `InsuranceQuoteEngineService.cs` contains:

```csharp
if (request.Customer.Age < 18 || request.Customer.Age > 75)
```

Stryker mutates this to:
```csharp
if (request.Customer.Age <= 18 || request.Customer.Age > 75)
```

If the test suite tests age `17` and age `30` but **omits testing age `18` exactly**, this mutant will **SURVIVE**!

### Killing the Mutant:
Add an explicit boundary test for age `18`:
```csharp
[Fact]
public void GenerateQuote_ShouldApprove_WhenAgeIsExactLowerBoundary18()
{
    var customer = new CustomerProfileBuilder().WithAge(18).Build();
    var property = new PropertyDetailsBuilder().WithEstimatedValue(500_000_000m).WithYearBuilt(2010).Build();
    var request = new GenerateQuoteRequest(customer, property, CoverageTier.Basic);

    var result = _sut.GenerateQuote(request);

    result.Status.Should().Be(QuoteStatus.Approved);
}
```

Re-running `dotnet stryker` will now report the mutant as **KILLED**, increasing the overall score!

---

## 🎯 5. Standard Score Targets

- **< 60%:** Weak test suite with superficial assertions.
- **70% – 79%:** Acceptable baseline covering primary paths and major defects.
- **80% – 90%:** **Professional Standard (Recommended Target)**. Strong boundary coverage and rigorous assertions.
- **100%:** Rarely practical due to equivalent mutants that cannot be killed without unnecessary overhead.

---

## ✅ Lesson 08 Completion Checklist
- [ ] Understand why 100% Code Coverage alone is insufficient without mutation validation.
- [ ] Successfully installed `dotnet-stryker` and configured `stryker-config.json`.
- [ ] Executed Stryker and reviewed `mutation-report.html`.
- [ ] Identified and killed surviving mutants by strengthening boundary tests.
- [ ] Achieved a Mutation Score **≥ 80%**.

👉 **Next Step:** Proceed to [Lesson 09: Measuring Code Coverage with Coverlet & ReportGenerator](./09-code-coverage-coverlet.md)!
