# Lesson 01: Nền tảng Unit Testing & xUnit (Fundamentals)

## 🎯 Mục tiêu bài học
- Hiểu rõ định nghĩa "Unit" (Đơn vị kiểm thử) và phân biệt Unit Test vs Integration Test vs E2E Test.
- Nắm vững đặc tính của một Unit Test tốt: **Tính độc lập (Isolation)** và **Tính xác định (Determinism)**.
- Áp dụng mẫu cấu trúc kinh điển **Arrange – Act – Assert (AAA)**.
- Làm chủ quy chuẩn đặt tên test rõ nghĩa theo chuẩn công nghiệp: `Method_ShouldExpectedBehavior_WhenCondition`.
- Sử dụng xUnit `[Fact]` và thư viện kiểm tra biểu thức `FluentAssertions`.
- **Thực hành:** Xây dựng module `PriceCalculator` và viết bộ 10–15 Unit Tests toàn diện.

---

## 📖 1. Khái niệm cốt lõi (Core Concepts)

### 1.1. "Unit" trong Unit Test là gì?
- "Unit" không nhất thiết là 1 method hay 1 class đơn lẻ. "Unit" là **một đơn vị hành vi logic độc lập (unit of behavior)** có thể kiểm chứng được mà không cần phụ thuộc vào cơ sở dữ liệu thật, network, hệ thống file hay đồng hồ hệ thống.
- **Kim tự tháp kiểm thử (Test Pyramid):**
  - **Unit Tests:** Rất nhanh (vài mili-giây), chạy hoàn toàn trên RAM, số lượng nhiều nhất.
  - **Integration Tests:** Kiểm tra sự tương tác giữa code với Database, External API, File System. Chậm hơn, số lượng vừa phải.
  - **End-to-End (E2E) Tests:** Kiểm tra toàn bộ luồng từ giao diện UI/API đến DB. Chậm nhất, dễ gãy (brittle), số lượng ít nhất.

### 1.2. Hai đặc tính sống còn của Unit Test
1. **Isolated (Cô lập):** Test A chạy không làm ảnh hưởng hay phụ thuộc vào kết quả của Test B. Thứ tự chạy test không quan trọng.
2. **Deterministic (Xác định):** Cùng một input, test phải luôn cho ra cùng một kết quả dù chạy ở máy Dev, CI server, lúc nửa đêm hay sáng sớm.

---

## 📐 2. Mô hình cấu trúc AAA (Arrange – Act – Assert)

Mỗi bài test nên được phân tách thành 3 phần rõ ràng:

```csharp
[Fact]
public void CalculateDiscount_ShouldReturnDiscountedPrice_WhenValidDiscountApplied()
{
    // 1. Arrange (Chuẩn bị): Thiết lập dữ liệu đầu vào, đối tượng cần test (SUT - System Under Test)
    var calculator = new PriceCalculator();
    const decimal originalPrice = 100m;
    const decimal discountPercentage = 0.20m; // 20%

    // 2. Act (Thực thi): Gọi duy nhất 1 hành động/phương thức cần kiểm tra
    var result = calculator.Calculate(originalPrice, discountPercentage);

    // 3. Assert (Kiểm chứng): Kiểm tra kết quả trả về hoặc trạng thái có đúng kỳ vọng không
    result.Should().Be(80m);
}
```

---

## 🏷️ 3. Quy chuẩn đặt tên Test (Naming Convention)

Tên bài test phải là **tài liệu sống (living documentation)** cho nghiệp vụ phần mềm. Khi test bị fail, bạn phải đọc hiểu ngay lỗi ở đâu mà không cần nhìn vào code.

### ✅ Chuẩn khuyến nghị:
```text
[Tên_Phương_Thức]_[Nên_Có_Hành_Vi_Gì]_[Khi_Có_Điều_Kiện_Nào]
Method_ShouldExpectedBehavior_WhenCondition
```

**Ví dụ chuẩn:**
- `Calculate_ShouldReturnOriginalPrice_WhenDiscountIsZero`
- `Calculate_ShouldReturnZero_WhenDiscountIsOneHundredPercent`
- `Calculate_ShouldThrowArgumentOutOfRangeException_WhenPriceIsNegative`
- `Calculate_ShouldThrowArgumentOutOfRangeException_WhenDiscountIsGreaterThanOne`

### ❌ Tránh đặt tên mơ hồ:
- `Test1`, `TestPrice`
- `ShouldWork`, `CalculateTest`
- `Test_Discount_Success`

---

## 🔍 4. Sử dụng FluentAssertions

Thay vì dùng `Assert.Equal(expected, actual)` truyền thống của xUnit, `FluentAssertions` giúp code tự nhiên như ngôn ngữ nói và thông báo lỗi cực kỳ chi tiết:

```csharp
// xUnit Assert thông thường
Assert.Equal(80m, result);
Assert.Throws<ArgumentOutOfRangeException>(() => calculator.Calculate(-100, 0.2m));

// FluentAssertions (Khuyên dùng)
result.Should().Be(80m);
result.Should().BeGreaterThan(0);

Action act = () => calculator.Calculate(-100, 0.2m);
act.Should().Throw<ArgumentOutOfRangeException>()
   .WithMessage("*Price cannot be negative*");
```

---

## 🛠️ 5. Thực hành Step-by-Step: Xây dựng `PriceCalculator`

### Bước 5.1: Phân tích bài toán
Yêu cầu xây dựng lớp `PriceCalculator` với phương thức:
`decimal Calculate(decimal originalPrice, decimal discountPercentage)`

**Quy tắc nghiệp vụ:**
1. `discountPercentage` nhận giá trị từ `0.0` (0%) đến `1.0` (100%).
2. Nếu `discountPercentage == 0` → Giá giữ nguyên.
3. Nếu `discountPercentage == 0.2` (20%) của 100 → Giá còn 80.
4. Nếu `discountPercentage == 1.0` (100%) của 100 → Giá còn 0.
5. Nếu `originalPrice == 0` → Giá luôn là 0.
6. Nếu `originalPrice < 0` → Bắn ngoại lệ `ArgumentOutOfRangeException` ("Price cannot be negative").
7. Nếu `discountPercentage < 0` hoặc `discountPercentage > 1` → Bắn ngoại lệ `ArgumentOutOfRangeException` ("Discount must be between 0 and 1").

---

### Bước 5.2: Viết mã nguồn Domain `PriceCalculator`

Tạo file `src/InsuranceQuoteEngine/Domain/PriceCalculator.cs`:

```csharp
namespace InsuranceQuoteEngine.Domain;

public class PriceCalculator
{
    /// <summary>
    /// Tính toán giá tiền sau khi áp dụng phần trăm giảm giá.
    /// </summary>
    /// <param name="originalPrice">Giá ban đầu (>= 0)</param>
    /// <param name="discountPercentage">Phần trăm giảm giá (0.0 đến 1.0)</param>
    /// <returns>Giá sau giảm</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public decimal Calculate(decimal originalPrice, decimal discountPercentage)
    {
        if (originalPrice < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(originalPrice), 
                "Price cannot be negative.");
        }

        if (discountPercentage < 0 || discountPercentage > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discountPercentage), 
                "Discount percentage must be between 0.0 and 1.0.");
        }

        var discountAmount = originalPrice * discountPercentage;
        return originalPrice - discountAmount;
    }
}
```

---

### Bước 5.3: Viết bộ Unit Tests hoàn chỉnh

Tạo file `tests/InsuranceQuoteEngine.UnitTests/Domain/PriceCalculatorTests.cs`:

```csharp
using FluentAssertions;
using InsuranceQuoteEngine.Domain;
using Xunit;

namespace InsuranceQuoteEngine.UnitTests.Domain;

public class PriceCalculatorTests
{
    private readonly PriceCalculator _sut; // SUT = System Under Test

    public PriceCalculatorTests()
    {
        _sut = new PriceCalculator();
    }

    #region Happy Path Tests

    [Fact]
    public void Calculate_ShouldReturnOriginalPrice_WhenDiscountIsZero()
    {
        // Arrange
        const decimal price = 100m;
        const decimal discount = 0m;

        // Act
        var result = _sut.Calculate(price, discount);

        // Assert
        result.Should().Be(100m);
    }

    [Fact]
    public void Calculate_ShouldReturnDiscountedPrice_WhenDiscountIsTwentyPercent()
    {
        // Arrange
        const decimal price = 100m;
        const decimal discount = 0.20m;

        // Act
        var result = _sut.Calculate(price, discount);

        // Assert
        result.Should().Be(80m);
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenDiscountIsOneHundredPercent()
    {
        // Arrange
        const decimal price = 100m;
        const decimal discount = 1.0m;

        // Act
        var result = _sut.Calculate(price, discount);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public void Calculate_ShouldReturnZero_WhenOriginalPriceIsZero()
    {
        // Arrange
        const decimal price = 0m;
        const decimal discount = 0.50m;

        // Act
        var result = _sut.Calculate(price, discount);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public void Calculate_ShouldHandleFractionalAmounts_WhenDiscountProducesDecimals()
    {
        // Arrange
        const decimal price = 99.99m;
        const decimal discount = 0.15m; // 15% của 99.99 = 14.9985 => còn 84.9915

        // Act
        var result = _sut.Calculate(price, discount);

        // Assert
        result.Should().Be(84.9915m);
    }

    #endregion

    #region Failure Path & Boundary Validation Tests

    [Fact]
    public void Calculate_ShouldThrowArgumentOutOfRangeException_WhenOriginalPriceIsNegative()
    {
        // Arrange
        const decimal negativePrice = -100m;
        const decimal discount = 0.20m;

        // Act
        Action act = () => _sut.Calculate(negativePrice, discount);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("originalPrice")
           .WithMessage("*Price cannot be negative*");
    }

    [Fact]
    public void Calculate_ShouldThrowArgumentOutOfRangeException_WhenDiscountIsLessThanZero()
    {
        // Arrange
        const decimal price = 100m;
        const decimal negativeDiscount = -0.01m;

        // Act
        Action act = () => _sut.Calculate(price, negativeDiscount);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("discountPercentage")
           .WithMessage("*Discount percentage must be between 0.0 and 1.0*");
    }

    [Fact]
    public void Calculate_ShouldThrowArgumentOutOfRangeException_WhenDiscountIsGreaterThanOne()
    {
        // Arrange
        const decimal price = 100m;
        const decimal invalidDiscount = 1.01m;

        // Act
        Action act = () => _sut.Calculate(price, invalidDiscount);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("discountPercentage")
           .WithMessage("*Discount percentage must be between 0.0 and 1.0*");
    }

    #endregion
}
```

---

### Bước 5.4: Chạy kiểm thử và quan sát kết quả

Chạy lệnh trong terminal:
```bash
dotnet test --filter FullyQualifiedName~PriceCalculatorTests
```

**Kết quả thành công:**
```text
Passed!  - Failed: 0, Passed: 8, Skipped: 0, Total: 8
```

---

## 📝 6. Bài tập tự luyện (Hands-on Challenge)

Mở rộng `PriceCalculator` thêm phương thức:
`decimal CalculateWithTax(decimal originalPrice, decimal discountPercentage, decimal taxRate)`

**Quy tắc:**
1. Thuế được tính trên giá **sau khi đã giảm giá**.
   `FinalPrice = (OriginalPrice - DiscountAmount) * (1 + taxRate)`.
2. `taxRate` phải từ `0.0` đến `0.5` (tối đa 50%). Nếu âm hoặc > 0.5 → ném `ArgumentOutOfRangeException`.
3. Viết ít nhất **5 unit tests** cho phương thức này bao gồm:
   - Happy path: giá 100, giảm 20%, thuế 10% (0.1) => kết quả 88.
   - Thuế bằng 0.
   - Thuế âm => ném ngoại lệ.
   - Thuế vượt quá 0.5 => ném ngoại lệ.
   - Giá 0, có thuế => kết quả 0.

---

## ✅ Check-list hoàn thành Lesson 01
- [ ] Hiểu rõ cấu trúc AAA trong từng bài test.
- [ ] Tuân thủ quy chuẩn đặt tên `Method_ShouldExpectedBehavior_WhenCondition`.
- [ ] Dùng `FluentAssertions` để kiểm tra cả giá trị trả về và ngoại lệ (`Throw<T>`).
- [ ] Viết thành công bộ test cho `PriceCalculator` và hoàn thành bài tập mở rộng `CalculateWithTax`.

👉 **Tiếp theo:** Chuyển sang [Lesson 02: Thiết kế Test & Đo lường chất lượng Test](./02-test-design-and-quality.md)!
