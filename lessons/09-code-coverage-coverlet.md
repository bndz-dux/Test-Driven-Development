# Lesson 09: Đo lường Code Coverage với Coverlet & ReportGenerator

## 🎯 Mục tiêu bài học
- Phân biệt các chỉ số độ phủ mã nguồn: **Line Coverage**, **Statement Coverage**, **Method Coverage**, và đặc biệt là **Branch Coverage (Độ phủ nhánh)**.
- Hiểu đúng vị trí của Code Coverage: Là công cụ hỗ trợ tìm **vùng code bị bỏ quên**, không phải là thước đo duy nhất cho chất lượng phần mềm.
- Sử dụng **Coverlet** để thu thập dữ liệu kiểm thử dưới định dạng Cobertura XML.
- Sử dụng **ReportGenerator** để biến file XML thô thành **Báo cáo HTML trực quan, tương tác cao**.
- Thiết lập ngưỡng kiểm tra độ phủ tự động (**Coverage Thresholds**).

---

## 📊 1. Các loại Code Coverage cần biết

```text
               ┌────────────────────────────────────────────────────────┐
               │                  Các loại Code Coverage                │
               └───────────────────────────┬────────────────────────────┘
         ┌─────────────────────────────────┼────────────────────────────┐
         ▼                                 ▼                            ▼
┌──────────────────┐             ┌──────────────────┐         ┌──────────────────┐
│  Line Coverage   │             │ Branch Coverage  │         │ Method Coverage  │
├──────────────────┤             ├──────────────────┤         ├──────────────────┤
│ Tỷ lệ dòng code  │             │ Tỷ lệ các nhánh  │         │ Tỷ lệ các hàm    │
│ được chạy qua    │             │ IF / ELSE / CASE │         │ có ít nhất 1 lần │
│ bởi test suite.  │             │ được rẽ nhánh.   │         │ được gọi tới.    │
└──────────────────┘             └──────────────────┘         └──────────────────┘
```

### 💡 Tại sao Branch Coverage quan trọng hơn Line Coverage?
Xem xét đoạn code:
```csharp
public decimal CalculateBonus(bool isVip, decimal amount)
{
    decimal bonus = 0;
    if (isVip && amount > 1000) // Có 2 điều kiện boolean => Có 4 nhánh logic
    {
        bonus = 100;
    }
    return bonus;
}
```
Nếu bạn chỉ viết 1 bài test với `isVip = true` và `amount = 2000`, bạn sẽ đạt **100% Line Coverage**, nhưng **Branch Coverage mới chỉ đạt 50%** vì bạn chưa kiểm tra nhánh khi `isVip = false` hoặc `amount <= 1000`!

---

## 🛠️ 2. Thực hành Step-by-Step: Đo Coverage và Xuất Báo cáo HTML

### Bước 2.1: Cài đặt công cụ ReportGenerator

Cài đặt công cụ toàn cục trên máy tính:
```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```
*(Nếu đã cài, có thể update: `dotnet tool update -g dotnet-reportgenerator-globaltool`)*

---

### Bước 2.2: Chạy Test và Thu thập Coverage bằng Coverlet

Tại thư mục gốc của dự án (`d:/LEARN BY MYSELF/TDD`), chạy lệnh:

```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

**Kết quả:**
Một thư mục dạng `./TestResults/{GUID}/coverage.cobertura.xml` sẽ được tự động sinh ra chứa toàn bộ dữ liệu độ phủ chi tiết.

---

### Bước 2.3: Sinh báo cáo HTML tương tác bằng ReportGenerator

Chạy lệnh sau để gộp tất cả file kết quả thành giao diện HTML tuyệt đẹp:

```bash
reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:"./CoverageReport" -reporttypes:Html
```

---

### Bước 2.4: Xem và Phân tích Báo cáo

Mở file `./CoverageReport/index.html` trên trình duyệt:
- Bạn sẽ thấy tổng quan: **Line coverage %**, **Branch coverage %**, **Method coverage %**.
- Nhấp vào từng class (ví dụ `InsuranceQuoteEngineService.cs`):
  - 🟩 **Màu xanh lá cây:** Dòng code đã được test bao phủ hoàn toàn.
  - 🟥 **Màu đỏ:** Dòng code chưa từng có bài test nào chạm tới.
  - 🟨 **Màu vàng / Cam:** Nhánh rẽ điều kiện mới chỉ được test 1 nửa (chưa test nhánh `false` hoặc ngược lại).

---

## 🛑 3. Thiết lập ngưỡng chặn tự động (Coverage Thresholds)

Bạn có thể yêu cầu lệnh test tự động báo lỗi nếu độ phủ không đạt chỉ tiêu bằng cách thêm tham số vào lệnh `dotnet test`:

```bash
# Yêu cầu Line Coverage tối thiểu 80% và Branch Coverage tối thiểu 75%
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total
```

Nếu một thành viên trong nhóm viết code mới nhưng quên viết test khiến coverage tụt xuống dưới 80%, lệnh test sẽ trả về lỗi ngay lập tức!

---

## 🎯 4. Ngưỡng Coverage đề xuất trong thực tế

| Loại tầng / Module | Line Coverage mục tiêu | Branch Coverage mục tiêu | Lý do |
| :--- | :--- | :--- | :--- |
| **Domain Logic / Business Rules** | **90% – 100%** | **85% – 95%** | Nơi chứa tài sản quan trọng nhất của doanh nghiệp, không được phép có lỗi logic |
| **Application Services** | **80% – 90%** | **75% – 85%** | Điều phối luồng và validate |
| **Infrastructure / Repositories** | **60% – 80%** | **50% – 70%** | Thường được kiểm thử tốt hơn qua Integration Test |
| **DTOs, Enums, Boilerplate** | *Bỏ qua (0%)* | *Bỏ qua (0%)* | Không nên tốn thời gian test getter/setter đơn giản |

### Cách cấu hình loại trừ (Exclude) code không cần test:
Trong file `.csproj` của project Unit Test:
```xml
<PropertyGroup>
  <!-- Bỏ qua các file Auto-generated hoặc Models thuần túy -->
  <Exclude>[*]*.Exceptions.*,[*]*.DTOs.*</Exclude>
</PropertyGroup>
```

---

## ✅ Check-list hoàn thành Lesson 09
- [ ] Phân biệt rõ Line Coverage vs Branch Coverage.
- [ ] Chạy thành công lệnh `dotnet test --collect:"XPlat Code Coverage"`.
- [ ] Sinh thành công báo cáo HTML trực quan bằng `reportgenerator`.
- [ ] Xem báo cáo và kiểm tra các dòng code màu vàng/đỏ để bổ sung test.
- [ ] Thiết lập ngưỡng kiểm tra độ phủ tối thiểu 80% Line và 75% Branch.

👉 **Tiếp theo:** Chuyển sang [Lesson 10: Tự động hóa CI/CD Quality Gates](./10-ci-quality-gates.md)!
