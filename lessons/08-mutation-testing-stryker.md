# Lesson 08: Đánh giá chất lượng Test với Mutation Testing (Stryker.NET)

## 🎯 Mục tiêu bài học
- Hiểu được nghịch lý: **100% Code Coverage không có nghĩa là Test Suite của bạn chất lượng**.
- Làm quen với khái niệm **Mutation Testing (Kiểm thử đột biến)**:
  - Đột biến (Mutants) là gì?
  - Thế nào là **Killed Mutant** (Đột biến bị tiêu diệt - Tốt) vs **Survived Mutant** (Đột biến sống sót - Lỗ hổng trong Test)?
  - Công thức tính **Mutation Score**.
- Cài đặt và sử dụng công cụ **Stryker.NET** trong .NET.
- Đọc báo cáo trực quan dạng HTML của Stryker, tìm các Surviving Mutants và viết thêm test để tiêu diệt chúng.
- Thiết lập mục tiêu đạt Mutation Score **≥ 80%**.

---

## 🤯 1. Tại sao Code Coverage lại "lừa dối" chúng ta?

Hãy xem xét đoạn code sau:

```csharp
public bool IsAdult(int age)
{
    return age >= 18;
}
```

Và bạn viết bài test:
```csharp
[Fact]
public void IsAdult_Test()
{
    var result = IsAdult(25); // Thực thi qua dòng code => 100% Code Coverage!
    // Nhưng bạn QUÊN không assert gì cả, hoặc chỉ assert: result.Should().NotBeNull();
}
```
Mặc dù công cụ đo Coverage báo bạn đạt **100% Line Coverage**, nhưng nếu ai đó sửa code thành `return false;` thì test vẫn pass!

> **Mutation Testing đặt câu hỏi ngược lại:**  
> *"Nếu mã nguồn bị cố tình làm sai (gây lỗi đột biến), bộ test của bạn có phát hiện ra và báo ĐỎ hay không?"*

---

## 🧬 2. Stryker.NET hoạt động như thế nào?

Stryker tự động quét mã nguồn C# và tạo ra hàng loạt các **đột biến (mutants)** nhỏ, ví dụ:

| Mã nguồn gốc của bạn | Stryker sửa thành (Đột biến) | Loại đột biến |
| :--- | :--- | :--- |
| `if (age >= 18)` | `if (age > 18)` | Binary Expression Mutation (BVA) |
| `if (age >= 18)` | `if (age <= 18)` | Equality Mutation |
| `return price - discount;` | `return price + discount;` | Arithmetic Mutation |
| `if (hasClaims)` | `if (!hasClaims)` | Boolean Mutation |
| `discount = 0.20m;` | `discount = 0.0m;` | Constant Mutation |
| `_repo.Save(order);` | *(Xóa dòng gọi hàm này)* | Statement Removal Mutation |

Sau khi tạo đột biến:
1. Stryker chạy lại bộ Unit Test của bạn.
2. **Nếu Test BÁO FAIL (ĐỎ):** 👉 **MUTANT KILLED (Tiêu diệt thành công!)** → Bộ test rất nhạy và tốt.
3. **Nếu Test VẪN PASS (XANH):** 👉 **MUTANT SURVIVED (Đột biến sống sót!)** → Bộ test có lỗ hổng, thiếu assertion hoặc thiếu test case ở biên đó!

$$\text{Mutation Score} = \left( \frac{\text{Killed Mutants} + \text{Timeout Mutants}}{\text{Total Mutants}} \right) \times 100\%$$

---

## 🛠️ 3. Thực hành Step-by-Step: Cài đặt và Chạy Stryker.NET

### Bước 3.1: Cài đặt công cụ toàn cục `dotnet-stryker`

Mở terminal và chạy lệnh:
```bash
dotnet tool install -g dotnet-stryker
```
*(Nếu đã cài trước đó, bạn có thể cập nhật bằng: `dotnet tool update -g dotnet-stryker`)*

---

### Bước 3.2: Tạo file cấu hình `stryker-config.json`

Tạo file `tests/InsuranceQuoteEngine.UnitTests/stryker-config.json`:

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

**Giải thích cấu hình:**
- `"project"`: Chỉ định project mã nguồn cần kiểm thử đột biến.
- `"reporters"`: Xuất kết quả ra màn hình console và sinh file HTML tương tác trực quan.
- `"thresholds"`: Tuân thủ quy tắc `high >= low >= break`.
  - `high: 85`: Điểm xanh (mức chất lượng cao).
  - `low: 80`: Điểm vàng (mức cảnh báo).
  - `break: 70`: Điểm đỏ, nếu Mutation Score < 70% thì Stryker sẽ trả về mã lỗi (Exit Code != 0) để ngắt CI/CD Pipeline!
- `"mutate"`: Chỉ tập trung đột biến vào tầng logic cốt lõi `Domain/`, bỏ qua các file định nghĩa Exception đơn giản.

---

### Bước 3.3: Chạy Stryker.NET

Di chuyển vào thư mục test và thực thi:

```bash
cd tests/InsuranceQuoteEngine.UnitTests
dotnet stryker
```

Stryker sẽ:
1. Build toàn bộ dự án.
2. Chạy baseline test ban đầu (đảm bảo tất cả test đang xanh).
3. Đột biến mã nguồn và chạy các bài test liên quan song song.
4. In điểm số Mutation Score ra console và xuất file báo cáo tại `StrykerOutput/.../reports/mutation-report.html`.

---

## 🔍 4. Phân tích Báo cáo và Tiêu diệt Surviving Mutants

Giả sử trong `InsuranceQuoteEngineService.cs` có đoạn code:

```csharp
if (request.Customer.Age < 18 || request.Customer.Age > 75)
```

Stryker đột biến thành:
```csharp
if (request.Customer.Age <= 18 || request.Customer.Age > 75)
```

Nếu trong test bạn chỉ test tuổi `17` và `30` mà **chưa test tuổi đúng `18`**, mutant này sẽ **SỐNG SÓT (Survived)**!

### Cách tiêu diệt Mutant:
Thêm test case điểm biên chính xác cho tuổi `18`:
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

Chạy lại `dotnet stryker` → Mutant ngay lập tức bị **KILLED** và điểm số tăng lên!

---

## 🎯 5. Mục tiêu điểm số chuẩn mực

- **Dưới 60%:** Test suite kém, chỉ mang tính hình thức lấy coverage.
- **70% – 79%:** Mức khá, đã kiểm tra được phần lớn happy path và lỗi lớn.
- **80% – 90%:** **Mức chuẩn chuyên nghiệp (Recommended Target)**. Bao quát tốt các biên, điều kiện logic và nhánh rẽ.
- **100%:** Thường không cần thiết vì có những đột biến tương đương (Equivalent Mutants) không thể tiêu diệt hoặc không có giá trị kinh tế.

---

## ✅ Check-list hoàn thành Lesson 08
- [ ] Hiểu rõ tại sao 100% Code Coverage vẫn có thể lọt lỗi nếu thiếu Mutation Testing.
- [ ] Cài đặt thành công `dotnet-stryker` và tạo file cấu hình `stryker-config.json`.
- [ ] Chạy Stryker và mở xem file báo cáo HTML `mutation-report.html`.
- [ ] Tìm ra ít nhất 1 Surviving Mutant và viết thêm bài test để Kill mutant đó.
- [ ] Đưa Mutation Score của dự án đạt **≥ 80%**.

👉 **Tiếp theo:** Chuyển sang [Lesson 09: Đo lường Code Coverage với Coverlet & ReportGenerator](./09-code-coverage-coverlet.md)!
