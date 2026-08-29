# Lesson 02: Thiết kế Test & Đo lường chất lượng Test (Test Design & Quality)

## 🎯 Mục tiêu bài học
- Học cách **test theo hành vi nghiệp vụ (business behavior)** thay vì kiểm tra chi tiết cài đặt nội bộ (implementation details).
- Nắm vững 2 kỹ thuật thiết kế test kinh điển trong Software Testing:
  1. **Phân vùng tương đương (Equivalence Partitioning - EP)**
  2. **Phân tích giá trị biên (Boundary Value Analysis - BVA)**
- Kiểm thử toàn diện: Happy Path, Failure Path, Null/Empty/Whitespace, Out-of-range, Exceptions.
- Tránh các bài test "giòn dễ vỡ" (brittle tests) khi refactor mã nguồn.
- **Thực hành:** Thiết kế bộ kiểm thử chất lượng cao cho `CustomerAgeValidator` và `DiscountPolicy`.

---

## 📖 1. Kiểm thử Hành vi vs Chi tiết cài đặt

### ❌ Sai lầm phổ biến: Test chi tiết cài đặt (Implementation Details)
Khi viết test gắn chặt với cách viết code bên trong (ví dụ kiểm tra xem biến private có tên gì, danh sách nội bộ có gọi hàm `Sort()` hay không):
- Mỗi lần bạn refactor thuật toán để tối ưu tốc độ, test sẽ bị vỡ dù chức năng vẫn chạy đúng 100%.
- Lập trình viên sẽ sợ refactor code vì mỗi lần sửa là hàng loạt test báo đỏ.

### ✅ Nguyên tắc: Test hành vi bên ngoài (Observable Behavior)
- Coi module/class như một **hộp đen (black box)** hoặc hệ thống phản hồi:
  - **Đầu vào (Inputs / State):** Dữ liệu truyền vào hàm hoặc cấu hình ban đầu.
  - **Đầu ra (Outputs / Observable State):** Giá trị trả về, exception được ném ra, hoặc thay đổi trạng thái có thể quan sát được.

---

## 🔬 2. Kỹ thuật Phân vùng tương đương (EP) & Giá trị biên (BVA)

### 2.1. Phân vùng tương đương (Equivalence Partitioning)
Chia tập hợp dữ liệu đầu vào thành các nhóm (partitions) mà hệ thống xử lý **như nhau**. Ta chỉ cần chọn **1 đại diện** cho mỗi nhóm để test, tránh viết hàng trăm test trùng lặp vô nghĩa.

**Ví dụ:** Quy tắc độ tuổi mua bảo hiểm:
- Hợp lệ: Từ 18 đến 65 tuổi.
- Không hợp lệ (quá trẻ): < 18 tuổi.
- Không hợp lệ (quá già): > 65 tuổi.

Ta có 3 phân vùng:
1. `[-∞, 17]`: Không hợp lệ (Đại diện: `15`)
2. `[18, 65]`: Hợp lệ (Đại diện: `30`)
3. `[66, +∞]`: Không hợp lệ (Đại diện: `70`)

### 2.2. Phân tích giá trị biên (Boundary Value Analysis)
Lỗi lập trình (bugs) hầu hết luôn xảy ra tại các **điểm biên** (do nhầm lẫn giữa `>`, `>=`, `<`, `<=`).
Quy tắc 3 điểm biên (On, Off, In-between):
- Với điều kiện `age >= 18`:
  - **17** (Off-boundary: Ngay sát dưới biên → False)
  - **18** (On-boundary: Ngay tại biên → True)
  - **19** (In-boundary: Ngay sát trên biên → True)

### Bảng các loại giá trị biên phổ biến cần luôn luôn test:

| Kiểu dữ liệu | Các trường hợp biên cần kiểm tra |
| :--- | :--- |
| **Số (int, decimal)** | `0`, `1`, `-1`, `Min`, `Max`, giá trị ngay trước/sau ngưỡng điều kiện (`if x > 100` -> test `99`, `100`, `101`) |
| **Chuỗi (string)** | `null`, `""` (empty), `" "` (whitespace), `"  \t\n  "`, chuỗi 1 ký tự, chuỗi có độ dài tối đa cho phép, chuỗi vượt quá độ dài |
| **Danh sách (Collection)** | `null`, rỗng (`Count = 0`), 1 phần tử, nhiều phần tử, phần tử trùng lặp (duplicates) |
| **Thời gian (DateTime)** | Quá khứ, hiện tại, tương lai, giao thừa năm nhuận, lệch múi giờ (UTC vs Local) |

---

## 🛠️ 3. Thực hành Step-by-Step: Xây dựng & Kiểm thử `InsuranceEligibilityValidator`

Chúng ta cùng xây dựng một bộ xác thực tính hợp lệ của khách hàng tham gia bảo hiểm.

### Yêu cầu nghiệp vụ (Business Requirements):
1. **Tên khách hàng:** Không được null, rỗng hoặc chỉ toàn khoảng trắng. Độ dài từ 2 đến 100 ký tự.
2. **Tuổi khách hàng:** Phải từ đủ 18 đến 65 tuổi (tính đến ngày sinh nhật).
3. **Mã bưu điện (ZipCode):** Phải đúng định dạng 5 chữ số (ví dụ: `"70000"`).

---

### Bước 3.1: Viết Model và Validator trong Domain

Tạo file `src/InsuranceQuoteEngine/Domain/CustomerEligibilityValidator.cs`:

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

        // 1. Kiểm tra họ tên
        if (string.IsNullOrWhiteSpace(applicant.FullName))
        {
            errors.Add("Full name is required.");
        }
        else if (applicant.FullName.Trim().Length < 2 || applicant.FullName.Trim().Length > 100)
        {
            errors.Add("Full name must be between 2 and 100 characters.");
        }

        // 2. Kiểm tra độ tuổi (18 - 65)
        if (applicant.Age < 18)
        {
            errors.Add("Applicant must be at least 18 years old.");
        }
        else if (applicant.Age > 65)
        {
            errors.Add("Applicant cannot be older than 65 years old.");
        }

        // 3. Kiểm tra PostalCode
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

### Bước 3.2: Thiết kế Ma trận Test Cases bài bản

Trước khi gõ code test, hãy liệt kê các test cases theo nguyên tắc EP và BVA:

1. **Happy Path:**
   - Khách hàng chuẩn (Tên hợp lệ, Tuổi 30, PostalCode "70000") → IsValid = true, không có lỗi.
2. **Biên độ tuổi (BVA):**
   - 17 tuổi (dưới biên) → Fail ("Applicant must be at least 18 years old.")
   - 18 tuổi (tại biên dưới) → Pass
   - 19 tuổi (trên biên dưới) → Pass
   - 64 tuổi (dưới biên trên) → Pass
   - 65 tuổi (tại biên trên) → Pass
   - 66 tuổi (vượt biên trên) → Fail ("Applicant cannot be older than 65 years old.")
3. **Biên chuỗi họ tên (String & Length BVA):**
   - null → Fail
   - `""` → Fail
   - `"   "` → Fail
   - 1 ký tự (`"A"`) → Fail
   - 2 ký tự (`"An"`) → Pass
   - 100 ký tự → Pass
   - 101 ký tự → Fail
4. **Biên mã bưu điện (ZipCode):**
   - 4 chữ số (`"1234"`) → Fail
   - 5 chữ số (`"12345"`) → Pass
   - 6 chữ số (`"123456"`) → Fail
   - Chứa chữ cái (`"7000A"`) → Fail
   - Null hoặc khoảng trắng → Fail
5. **Ngoại lệ:**
   - Truyền `applicant = null` → Throw `ArgumentNullException`.

---

### Bước 3.3: Viết bộ Unit Tests hoàn chỉnh với FluentAssertions

Tạo file `tests/InsuranceQuoteEngine.UnitTests/Domain/CustomerEligibilityValidatorTests.cs`:

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

## 📝 4. 5 câu hỏi tự vấn khi thiết kế Test (Check-list tư duy)

Khi viết test cho bất kỳ method nghiệp vụ nào, hãy luôn đặt 5 câu hỏi:
1. **Happy Path:** Dữ liệu chuẩn mực nhất sẽ trả về kết quả gì?
2. **What Can Go Wrong (Failure Path):** Người dùng nhập sai cái gì, hệ thống báo lỗi ra sao?
3. **Boundaries:** Có điều kiện `>=`, `<=`, `Length`, `Count` nào không? Đã test các giá trị biên `n - 1`, `n`, `n + 1` chưa?
4. **Invalid / Extreme Inputs:** Đã thử với `null`, `""`, số âm, danh sách rỗng, ngày trong quá khứ chưa?
5. **Business Invariants:** Quy tắc bất biến nào của hệ thống không bao giờ được phép vi phạm?

---

## ✅ Check-list hoàn thành Lesson 02
- [ ] Phân biệt được Test hành vi (Behavior) và Test chi tiết cài đặt (Implementation details).
- [ ] Thành thạo vẽ phân vùng tương đương (EP) và phân tích các điểm biên (BVA).
- [ ] Viết đầy đủ các case cho String (null, empty, whitespace, min/max length).
- [ ] Chạy thành công toàn bộ test suite của `CustomerEligibilityValidator`.

👉 **Tiếp theo:** Chuyển sang [Lesson 03: Chiến lược Mocking & Cô lập phụ thuộc với Moq](./03-mocking-strategies.md)!
