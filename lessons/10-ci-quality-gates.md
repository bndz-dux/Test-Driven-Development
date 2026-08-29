# Lesson 10: Automated CI/CD Quality Gates

## 🎯 Lesson Objectives
- Understand the role of **Quality Gates** in modern continuous delivery workflows.
- Implement automated checks to block sub-standard Pull Requests (PRs) before merging into `main`.
- Construct complete, production-ready CI workflow definitions for:
  1. **GitHub Actions (`.github/workflows/ci.yml`)**
  2. **Azure DevOps Pipelines (`azure-pipelines.yml`)**
- Automate the enforcement pipeline:
  - Restore & Build
  - Execute Unit Tests
  - Enforce Code Coverage thresholds (≥ 80%)
  - Execute Mutation Testing with Stryker (Score ≥ 80%)
  - Publish Test Results and Coverage artifacts directly to PR dashboards.

---

## 🛡️ 1. Pipeline Architecture & Quality Gates

Whenever a developer opens or updates a Pull Request, the CI pipeline triggers an automated verification chain:

```text
                     [ Developer Opens Pull Request ]
                                    │
                                    ▼
                             ┌──────────────┐
                             │    Build     │
                             └──────┬───────┘
                                    │
                               Pass │
                                    ▼
                             ┌──────────────┐
                             │  Unit Tests  │
                             └──────┬───────┘
                                    │
                         0 Failed   │
                                    ▼
                             ┌──────────────┐
                             │   Coverage   │ ───► Fail if Line < 80% or Branch < 75%
                             └──────┬───────┘
                                    │
                               Pass │
                                    ▼
                             ┌──────────────┐
                             │   Mutation   │ ───► Fail if Mutation Score < 80%
                             └──────┬───────┘
                                    │
                               Pass │
                                    ▼
                        ┌───────────────────────┐
                        │  APPROVED TO MERGE ✅ │
                        └───────────────────────┘
```

---

## 🐙 2. GitHub Actions Configuration (`.github/workflows/ci.yml`)

Create file `.github/workflows/ci.yml` at the root of the repository:

```yaml
name: Continuous Integration & Quality Gates

on:
  push:
    branches: [ "main" ]
  pull_request:
    branches: [ "main" ]

jobs:
  build-and-test:
    name: Build, Test, Coverage & Mutation Gates
    runs-on: ubuntu-latest

    steps:
      # 1. Checkout repository source code
      - name: Checkout Repository
        uses: actions/checkout@v4

      # 2. Setup .NET SDK
      - name: Setup .NET SDK
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      # 3. Restore dependencies
      - name: Restore Dependencies
        run: dotnet restore

      # 4. Build solution
      - name: Build Solution
        run: dotnet build --no-restore --configuration Release

      # 5. Run Unit Tests & Collect Coverage
      - name: Run Unit Tests & Collect Coverage
        run: >
          dotnet test tests/InsuranceQuoteEngine.UnitTests/InsuranceQuoteEngine.UnitTests.csproj
          --no-build
          --configuration Release
          --collect:"XPlat Code Coverage"
          --results-directory ./TestResults
          --logger "trx;LogFileName=test_results.trx"

      # 6. Install global CLI tools (ReportGenerator & Stryker)
      - name: Install Global Tools (ReportGenerator & Stryker)
        run: |
          dotnet tool install -g dotnet-reportgenerator-globaltool
          dotnet tool install -g dotnet-stryker

      # 7. Generate Code Coverage HTML Report & Summary
      - name: Generate Coverage Report
        run: >
          reportgenerator
          -reports:"./TestResults/**/coverage.cobertura.xml"
          -targetdir:"./CoverageReport"
          -reporttypes:"HtmlInline_AzurePipelines;Badges;Cobertura"

      # 8. Run Mutation Testing Gate (Stryker)
      - name: Run Stryker Mutation Testing Gate
        working-directory: ./tests/InsuranceQuoteEngine.UnitTests
        run: dotnet stryker --config-file stryker-config.json

      # 9. Upload Test Results Artifact
      - name: Upload Test Results Artifact
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: unit-test-results
          path: ./TestResults

      # 10. Upload Coverage Report Artifact
      - name: Upload Coverage Report Artifact
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: code-coverage-report
          path: ./CoverageReport
```

---

## 🔷 3. Azure DevOps Pipelines Configuration (`azure-pipelines.yml`)

For teams utilizing Azure DevOps, create `azure-pipelines.yml`:

```yaml
trigger:
  - main

pr:
  - main

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'

steps:
  - task: UseDotNet@2
    displayName: 'Setup .NET 8 SDK'
    inputs:
      packageType: 'sdk'
      version: '8.0.x'

  - script: dotnet restore
    displayName: 'Restore NuGet Packages'

  - script: dotnet build --no-restore --configuration $(buildConfiguration)
    displayName: 'Build Solution'

  - script: >
      dotnet test tests/InsuranceQuoteEngine.UnitTests/InsuranceQuoteEngine.UnitTests.csproj
      --configuration $(buildConfiguration)
      --no-build
      --collect:"XPlat Code Coverage"
      --results-directory $(Agent.TempDirectory)/TestResults
      --logger trx
    displayName: 'Execute Unit Tests & Measure Coverage'

  - task: PublishTestResults@2
    displayName: 'Publish Test Results to Azure DevOps'
    condition: succeededOrFailed()
    inputs:
      testResultsFormat: 'VSTest'
      testResultsFiles: '$(Agent.TempDirectory)/TestResults/*.trx'

  - task: PublishCodeCoverageResults@2
    displayName: 'Publish Code Coverage Results'
    inputs:
      summaryFileLocation: '$(Agent.TempDirectory)/TestResults/**/coverage.cobertura.xml'

  - script: |
      dotnet tool install -g dotnet-stryker
      cd tests/InsuranceQuoteEngine.UnitTests
      dotnet stryker --config-file stryker-config.json
    displayName: 'Execute Stryker Mutation Testing Gate'
```

---

## 🚨 4. Remediation Guide for Failed Quality Gates

When a PR triggers a red build in CI:

1. **Unit Test Failure:**
   - Open the **Test Results / Summary** tab to identify the failing method and assertion mismatch.
   - Reproduce and debug locally using `dotnet test --filter ...`.
2. **Coverage Gate Failure (< 80%):**
   - Download the `code-coverage-report` artifact, open `index.html`, and identify untested lines and uncovered branches.
3. **Mutation Gate Failure (< 80%):**
   - Inspect Stryker build logs to view surviving mutants.
   - Add boundary tests (BVA) or tighten assertions.

---

## ✅ Lesson 10 Completion Checklist
- [ ] Understand automated Quality Gates in continuous integration pipelines.
- [ ] Established `.github/workflows/ci.yml` / `azure-pipelines.yml`.
- [ ] Integrated the full verification chain: Build → Test → Coverage → Stryker.
- [ ] Configured automated merge blocking on quality regressions.

👉 **Next Step:** Proceed to [Lesson 11: Test Smells & Test Refactoring](./11-test-refactoring-and-smells.md)!
