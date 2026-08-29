# Unit Testing & Test-Driven Development (TDD) Mastery Course

Chào mừng bạn đến với lộ trình thực hành toàn diện về **Unit Testing** và **Test-Driven Development (TDD)** trên nền tảng **.NET / C#**. 

Bộ tài liệu này được chia nhỏ từ kế hoạch tổng thể thành **12 bài học chi tiết (Step-by-Step Lessons)**, đi từ nền tảng cơ bản nhất cho đến các kỹ thuật nâng cao, TDD thực chiến trên dự án Domain thực tế, Mutation Testing, Code Coverage, và thiết lập CI/CD Quality Gates.

---

## 🎯 Mục tiêu đầu ra (Learning Outcomes)

Sau khi hoàn thành toàn bộ lộ trình này, bạn sẽ làm chủ:
- [x] Tư duy viết Unit Test xoay quanh **hành vi (behavior)** và **quy tắc nghiệp vụ (business rules)** thay vì chi tiết cài đặt.
- [x] Áp dụng chuẩn chỉ mô hình **Arrange – Act – Assert (AAA)** và quy chuẩn đặt tên test rõ ràng.
- [x] Sử dụng thành thạo **xUnit**, **FluentAssertions**, và **Moq** (tránh over-mocking).
- [x] Xây dựng và tái sử dụng **Test Data Builders** để giữ test code luôn gọn gàng, dễ bảo trì.
- [x] Nhuần nhuyễn vòng lặp **RED → GREEN → REFACTOR** của TDD trên các tính năng thực tế.
- [x] Kiểm thử tự tin các tình huống phức tạp: Async, Exceptions, Time-dependent (`TimeProvider`/`IClock`), Boundary, Edge cases.
- [x] Sử dụng **Mutation Testing (Stryker.NET)** để đo lường độ nhạy thực sự của test suite.
- [x] Đo lường và thiết lập ngưỡng **Code Coverage (Coverlet & ReportGenerator)**.
- [x] Tích hợp **Quality Gates tự động trên CI/CD** (GitHub Actions / Azure DevOps).
- [x] Nhận diện và Refactor các **Test Smells** nguy hiểm.

---

## 🛠️ Công nghệ & Công cụ sử dụng

- **Ngôn ngữ / Runtime:** .NET 8 / C# 12 (hoặc .NET 9)
- **Test Framework:** xUnit
- **Assertion Library:** FluentAssertions
- **Mocking Framework:** Moq
- **Code Coverage:** Coverlet & ReportGenerator
- **Mutation Testing:** Stryker.NET
- **CI/CD:** GitHub Actions / Azure DevOps

---

## 📚 Danh mục bài học (Lesson Index)

| Bài học | Chủ đề | Mô tả tóm tắt | Link |
| :--- | :--- | :--- | :--- |
| **Lesson 00** | **Environment & Project Setup** | Khởi tạo Solution, cấu trúc thư mục chuẩn Domain/Tests, cài đặt các packages cần thiết | [Xem bài học](./lessons/00-introduction-and-setup.md) |
| **Lesson 01** | **Unit Testing Fundamentals** | Khái niệm Unit, mô hình AAA, quy ước đặt tên, `[Fact]`, FluentAssertions, bài tập `PriceCalculator` | [Xem bài học](./lessons/01-unit-testing-fundamentals.md) |
| **Lesson 02** | **Test Design & Test Quality** | Phân tích phân vùng tương đương (EP), phân tích giá trị biên (BVA), Happy/Failure path, kiểm thử Exception | [Xem bài học](./lessons/02-test-design-and-quality.md) |
| **Lesson 03** | **Mocking Strategies with Moq** | Stub vs Mock, cô lập phụ thuộc, Setup, Verify, Async Mocking, tránh Over-Mocking với `OrderService` | [Xem bài học](./lessons/03-mocking-strategies.md) |
| **Lesson 04** | **Test Data Builders & Fixtures** | Giải quyết vấn đề Object Mother / Test Clutter bằng Pattern Builder, chia sẻ Fixtures | [Xem bài học](./lessons/04-test-data-builders.md) |
| **Lesson 05** | **TDD Core Workflow (Red-Green-Refactor)** | 3 định luật TDD, quy trình TDD từng bước, thực hành xây dựng Order & Discount Engine hoàn toàn Test-First | [Xem bài học](./lessons/05-tdd-core-workflow.md) |
| **Lesson 06** | **Advanced Unit Testing Techniques** | Parameterized Tests (`[Theory]`, `[InlineData]`, `[MemberData]`, `[ClassData]`), xử lý thời gian (`IClock`/`TimeProvider`) | [Xem bài học](./lessons/06-advanced-unit-testing.md) |
| **Lesson 07** | **Capstone Project: Insurance Quote Engine** | Thực chiến TDD xây dựng hệ thống tính phí bảo hiểm hoàn chỉnh: Risk, Premium, Discounts, Referral/Decline | [Xem bài học](./lessons/07-insurance-quote-engine-capstone.md) |
| **Lesson 08** | **Mutation Testing with Stryker.NET** | Khái niệm Mutants, Mutant Killed vs Survived, cấu hình Stryker.NET, phân tích báo cáo và diệt mutants | [Xem bài học](./lessons/08-mutation-testing-stryker.md) |
| **Lesson 09** | **Code Coverage (Coverlet & ReportGenerator)** | Line / Branch / Method coverage, xuất file `coverage.cobertura.xml`, tạo HTML Report trực quan | [Xem bài học](./lessons/09-code-coverage-coverlet.md) |
| **Lesson 10** | **Automated CI Quality Gates** | Tích hợp Build, Test, Coverage Gate (>=80%), Stryker Gate vào GitHub Actions / Azure DevOps | [Xem bài học](./lessons/10-ci-quality-gates.md) |
| **Lesson 11** | **Test Smells & Test Refactoring** | Nhận diện 10 Test Smells thường gặp (Flaky, Fragile, Mystery Guest...) và hướng dẫn Refactor từng case | [Xem bài học](./lessons/11-test-refactoring-and-smells.md) |

---

## 📅 Lộ trình học đề xuất theo tuần (6-Week Roadmap)

- **Tuần 1 (Nền tảng):** Lesson 00, 01, 02 (Viết 30–40 unit tests chất lượng với AAA và Boundary Analysis).
- **Tuần 2 (Cô lập & Mocking):** Lesson 03, 04 (Làm chủ Moq và xây dựng thư viện Test Data Builder tái sử dụng).
- **Tuần 3 (Tư duy TDD):** Lesson 05, 06 (Luyện tập phản xạ RED → GREEN → REFACTOR và xử lý kịch bản nâng cao: Time/Theories).
- **Tuần 4 (Dự án thực chiến Capstone):** Lesson 07 (Phát triển hoàn chỉnh `InsuranceQuoteEngine` thuần TDD).
- **Tuần 5 (Đo lường chất lượng & Đột biến):** Lesson 08, 09 (Thực hiện Mutation Testing và phân tích Code Coverage).
- **Tuần 6 (CI Quality Gates & Refactoring):** Lesson 10, 11 (Tự động hóa trên CI Pipeline và chuẩn hóa toàn bộ Test Suite).

---

## 🏁 Tiêu chuẩn hoàn thành (Definition of Done)

Một lập trình viên làm chủ TDD khi nhận một yêu cầu nghiệp vụ mới:
1. Phân tích requirement thành các business rules & acceptance criteria.
2. Thiết kế test cases (Happy path, Edge cases, Failure path).
3. Viết Test đỏ (RED) → Viết code tối thiểu (GREEN) → Tối ưu hóa (REFACTOR).
4. Đảm bảo toàn bộ test suite xanh, deterministic (không flaky), chạy nhanh (< vài giây).
5. Kiểm tra Code Coverage (Line >= 80%, Branch >= 75%) và Mutation Score (>= 80%).
6. Push PR và tự tin vượt qua toàn bộ Automated Quality Gates trên CI!
