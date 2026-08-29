# Lesson 10: Tự động hóa CI/CD Quality Gates (Continuous Integration Quality Gates)

## 🎯 Mục tiêu bài học
- Hiểu khái niệm **Quality Gate (Cổng chất lượng)** trong quy trình CI/CD hiện đại.
- Thiết lập quy trình tự động chặn (Block) các Pull Request (PR) kém chất lượng trước khi được phép merge vào nhánh `main`.
- Xây dựng file cấu hình CI hoàn chỉnh cho:
  1. **GitHub Actions (`.github/workflows/ci.yml`)**
  2. **Azure DevOps Pipelines (`azure-pipelines.yml`)**
- Tự động thực thi:
  - Restore & Build
  - Chạy Unit Tests
  - Kiểm tra Code Coverage (Ngưỡng ≥ 80%)
  - Chạy Mutation Testing với Stryker (Ngưỡng ≥ 80%)
  - Xuất bản Test Results và Báo cáo trực tiếp lên PR.

---

## 🛡️ 1. Kiến trúc Pipeline & Quality Gates

Mỗi khi một lập trình viên tạo hoặc cập nhật một Pull Request, hệ thống CI sẽ tự động kích hoạt chuỗi kiểm soát:

```text
                     [ Developer tạo Pull Request ]
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
                            │   Coverage   │ ───► Fail nếu Line < 80% hoặc Branch < 75%
                            └──────┬───────┘
                                   │
                              Pass │
                                   ▼
                            ┌──────────────┐
                            │   Mutation   │ ───► Fail nếu Mutation Score < 80%
                            └──────┬───────┘
                                   │
                              Pass │
                                   ▼
                       ┌───────────────────────┐
                       │  APPROVED TO MERGE ✅ │
                       └───────────────────────┘
```

---

## 🐙 2. Thực hành cấu hình GitHub Actions (`.github/workflows/ci.yml`)

Tạo file `.github/workflows/ci.yml` tại thư mục gốc của repository:

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
      # 1. Checkout mã nguồn
      - name: Checkout Repository
        uses: actions/checkout@v4

      # 2. Cài đặt .NET SDK 8
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

      # 5. Chạy Unit Tests & Đo Code Coverage
      - name: Run Unit Tests & Collect Coverage
        run: >
          dotnet test tests/InsuranceQuoteEngine.UnitTests/InsuranceQuoteEngine.UnitTests.csproj
          --no-build
          --configuration Release
          --collect:"XPlat Code Coverage"
          --results-directory ./TestResults
          --logger "trx;LogFileName=test_results.trx"

      # 6. Cài đặt các công cụ CLI
      - name: Install Global Tools (ReportGenerator & Stryker)
        run: |
          dotnet tool install -g dotnet-reportgenerator-globaltool
          dotnet tool install -g dotnet-stryker

      # 7. Sinh báo cáo Code Coverage HTML & Summary
      - name: Generate Coverage Report
        run: >
          reportgenerator
          -reports:"./TestResults/**/coverage.cobertura.xml"
          -targetdir:"./CoverageReport"
          -reporttypes:"HtmlInline_AzurePipelines;Badges;Cobertura"

      # 8. Chạy Mutation Testing (Stryker)
      - name: Run Stryker Mutation Testing Gate
        working-directory: ./tests/InsuranceQuoteEngine.UnitTests
        run: dotnet stryker --config-file stryker-config.json

      # 9. Đăng tải Test Results lên giao diện GitHub Actions
      - name: Upload Test Results Artifact
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: unit-test-results
          path: ./TestResults

      # 10. Đăng tải Coverage Report lên giao diện GitHub Actions
      - name: Upload Coverage Report Artifact
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: code-coverage-report
          path: ./CoverageReport
```

---

## 🔷 3. Thực hành cấu hình Azure DevOps Pipelines (`azure-pipelines.yml`)

Nếu tổ chức của bạn sử dụng Azure DevOps, tạo file `azure-pipelines.yml`:

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

## 🚨 4. Cách xử lý khi CI Quality Gate bị chặn (Fail)

Khi Pull Request bị báo đỏ trên GitHub hoặc Azure DevOps:

1. **Nếu Unit Test bị Fail:**
   - Mở tab **Summary / Test Results** xem cụ thể tên method nào bị fail, assertion nào không khớp.
   - Chạy lại test đó trên máy local để debug và sửa.
2. **Nếu Coverage Gate bị Fail (< 80%):**
   - Tải artifact `code-coverage-report` về, mở `index.html` xem class/method nào có vệt màu đỏ (chưa được test) và bổ sung test case.
3. **Nếu Stryker Gate bị Fail (< 80%):**
   - Mở log của Stryker xem danh sách các **Survived Mutants**.
   - Thêm các test case điểm biên (BVA) hoặc bổ sung assertion đang bị thiếu.

---

## ✅ Check-list hoàn thành Lesson 10
- [ ] Hiểu rõ cách hoạt động của Quality Gate trong CI/CD.
- [ ] Tạo file `.github/workflows/ci.yml` hoặc `azure-pipelines.yml`.
- [ ] Tích hợp đầy đủ các bước: Build → Test → Coverage → Stryker.
- [ ] Hiểu cách cấu hình để pipeline tự động chặn merge nếu không đạt chỉ tiêu chất lượng.

👉 **Tiếp theo:** Chuyển sang [Lesson 11: Nhận diện Test Smells & Nghệ thuật Refactor Test](./11-test-refactoring-and-smells.md)!
