# Lesson 09: Measuring Code Coverage with Coverlet & ReportGenerator

## 🎯 Lesson Objectives
- Understand code coverage metrics: **Line Coverage**, **Statement Coverage**, **Method Coverage**, and specifically **Branch Coverage**.
- Understand the role of Code Coverage: A diagnostic tool to detect **untested code paths**, rather than a singular measure of software quality.
- Use **Coverlet** to collect cross-platform test coverage data in Cobertura XML format.
- Use **ReportGenerator** to convert raw XML coverage files into **interactive, rich HTML visual reports**.
- Configure automated enforcement thresholds (**Coverage Thresholds**).

---

## 📊 1. Code Coverage Metrics

```text
               ┌────────────────────────────────────────────────────────┐
               │                   Code Coverage Metrics                │
               └───────────────────────────┬────────────────────────────┘
         ┌─────────────────────────────────┼────────────────────────────┐
         ▼                                 ▼                            ▼
┌──────────────────┐             ┌──────────────────┐         ┌──────────────────┐
│  Line Coverage   │             │ Branch Coverage  │         │ Method Coverage  │
├──────────────────┤             ├──────────────────┤         ├──────────────────┤
│ Percentage of    │             │ Percentage of    │         │ Percentage of    │
│ lines executed   │             │ conditional      │         │ methods invoked  │
│ by test suite.   │             │ branches taken.  │         │ at least once.   │
└──────────────────┘             └──────────────────┘         └──────────────────┘
```

### 💡 Why Branch Coverage is Superior to Line Coverage:
Consider this method:
```csharp
public decimal CalculateBonus(bool isVip, decimal amount)
{
    decimal bonus = 0;
    if (isVip && amount > 1000) // 2 boolean conditions => 4 logical branches
    {
        bonus = 100;
    }
    return bonus;
}
```
If you only write 1 test with `isVip = true` and `amount = 2000`, you achieve **100% Line Coverage**, but **Branch Coverage is only 50%** because the `false` branches were never exercised!

---

## 🛠️ 2. Step-by-Step Exercise: Measuring Coverage and Generating HTML Reports

### Step 2.1: Install the ReportGenerator Tool

Install the global CLI tool:
```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```
*(If already installed, update via: `dotnet tool update -g dotnet-reportgenerator-globaltool`)*

---

### Step 2.2: Run Tests and Collect Coverage using Coverlet

From the project root (`d:/LEARN BY MYSELF/TDD`), run:

```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

**Result:**
A directory `./TestResults/{GUID}/coverage.cobertura.xml` will be generated containing raw coverage data.

---

### Step 2.3: Generate Interactive HTML Report

Execute ReportGenerator to compile the XML data into an interactive dashboard:

```bash
reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:"./CoverageReport" -reporttypes:Html
```

---

### Step 2.4: Inspect and Analyze the Report

Open `./CoverageReport/index.html` in your browser:
- Review summary metrics: **Line coverage %**, **Branch coverage %**, and **Method coverage %**.
- Drill down into specific classes (e.g. `InsuranceQuoteEngineService.cs`):
  - 🟩 **Green:** Code fully covered by tests.
  - 🟥 **Red:** Untested code lines.
  - 🟨 **Yellow / Orange:** Partially covered conditional branches (e.g., true condition tested, but false path missed).

---

## 🛑 3. Setting Automated Coverage Thresholds

You can fail builds automatically if coverage drops below specified quality targets:

```bash
# Require a minimum of 80% Line Coverage and 75% Branch Coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total
```

If a team member commits code without tests and coverage drops below 80%, the test run exits with an error immediately!

---

## 🎯 4. Recommended Target Thresholds in Production

| Architectural Layer | Target Line Coverage | Target Branch Coverage | Rationale |
| :--- | :--- | :--- | :--- |
| **Domain Logic / Business Rules** | **90% – 100%** | **85% – 95%** | Core business intellectual property; defects here are costly |
| **Application Services** | **80% – 90%** | **75% – 85%** | Orchestration and validation logic |
| **Infrastructure / Repositories** | **60% – 80%** | **50% – 70%** | Better validated via integration tests |
| **DTOs, Enums, Boilerplate** | *Excluded (0%)* | *Excluded (0%)* | Trivial boilerplate does not warrant test maintenance |

### Excluding Boilerplate Code from Coverage:
In the test project `.csproj` file:
```xml
<PropertyGroup>
  <Exclude>[*]*.Exceptions.*,[*]*.DTOs.*</Exclude>
</PropertyGroup>
```

---

## ✅ Lesson 09 Completion Checklist
- [ ] Differentiate between Line Coverage and Branch Coverage.
- [ ] Executed `dotnet test --collect:"XPlat Code Coverage"`.
- [ ] Generated visual HTML reports using `reportgenerator`.
- [ ] Analyzed partial/untested branches to add missing test scenarios.
- [ ] Configured automated coverage threshold enforcement (80% Line, 75% Branch).

👉 **Next Step:** Proceed to [Lesson 10: Automated CI/CD Quality Gates](./10-ci-quality-gates.md)!
