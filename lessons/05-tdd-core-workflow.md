# Lesson 05: Thực hành TDD Cốt lõi (Red – Green – Refactor)

## 🎯 Mục tiêu bài học
- Nắm vững triết lý và 3 định luật TDD của Uncle Bob (Robert C. Martin).
- Làm chủ chu kỳ TDD kinh điển: **🔴 RED → 🟢 GREEN → 🔵 REFACTOR**.
- Thay đổi tư duy: Từ "Viết code xong rồi viết test đối phó" sang **"Dùng test để đặc tả và dẫn dắt thiết kế phần mềm"**.
- Trải nghiệm từng bước nhỏ (Baby Steps) để kiểm soát 100% logic và không bao giờ bị rối.
- **Thực hành Test-First:** Xây dựng tính năng tính toán chiết khấu và xử lý đơn hàng từ con số 0.

---

## 🔄 1. Chu trình TDD (The Red – Green – Refactor Cycle)

```text
               ┌──────────────────────────────────────────────┐
               │                                              │
               ▼                                              │
         ┌───────────┐                                        │
         │  🔴 RED   │  Viết 1 test miêu tả hành vi mong muốn  │
         └─────┬─────┘  (Test phải BÁO ĐỎ / FAIL)             │
               │                                              │
               ▼                                              │
        ┌─────────────┐                                       │
        │  🟢 GREEN   │  Viết lượng code TỐI THIỂU để test qua │
        └──────┬──────┘  (Làm cho Test BÁO XANH / PASS)        │
               │                                              │
               ▼                                              │
       ┌───────────────┐                                      │
       │  🔵 REFACTOR  │  Làm sạch code, xóa bỏ trùng lặp,     │
       └───────┬───────┘  tối ưu thiết kế mà VẪN XANH TEST   │
               │                                              │
               └──────────────────────────────────────────────┘
```

### 📜 3 Định luật TDD của Uncle Bob (3 Laws of TDD):
1. **Định luật 1:** Bạn không được phép viết bất kỳ dòng mã nguồn sản phẩm (Production Code) nào trừ khi nó dùng để làm cho một bài Unit Test đang đỏ trở nên xanh.
2. **Định luật 2:** Bạn chỉ được phép viết vừa đủ một bài Unit Test đến khi nó bị Fail (lỗi không biên dịch được cũng tính là Fail).
3. **Định luật 3:** Bạn chỉ được phép viết vừa đủ dòng Production Code tối thiểu để làm bài test đang Fail đó Pass.

---

## 🛠️ 2. Thực hành Step-by-Step: Xây dựng `DiscountCalculator` bằng TDD

Chúng ta cùng đi qua từng chu kỳ **RED → GREEN → REFACTOR** thực tế để hiểu sức mạnh của TDD.

### Yêu cầu nghiệp vụ (Requirement):
Hệ thống tính chiết khấu cho khách hàng khi mua hàng:
1. Khách hàng thường (Normal) → 0% giảm giá.
2. Khách hàng thân thiết (Premium) → 10% giảm giá.
3. Khách hàng VIP → 20% giảm giá.
4. Đơn hàng trên 5,000,000 VND → Thêm 5% giảm giá (áp dụng cộng dồn cho tất cả loại khách hàng).
5. Mức giảm giá tối đa không bao giờ được vượt quá 30%.

---

### 🟢 Chu kỳ 1: Khách hàng Normal nhận 0% giảm giá

#### Bước 1.1: 🔴 RED (Viết Test đầu tiên)
Tạo file `tests/InsuranceQuoteEngine.UnitTests/Domain/TddDiscountCalculatorTests.cs`:

```csharp
using FluentAssertions;
using Xunit;

namespace InsuranceQuoteEngine.UnitTests.Domain;

public class TddDiscountCalculatorTests
{
    [Fact]
    public void CalculateDiscount_ShouldReturnZero_ForNormalCustomer()
    {
        // Arrange
        var calculator = new DiscountCalculator();

        // Act
        var discount = calculator.CalculateDiscount(CustomerMembership.Normal, orderAmount: 1000m);

        // Assert
        discount.Should().Be(0.0m);
    }
}
```
*Trạng thái:* **Lỗi biên dịch** (`DiscountCalculator` và `CustomerMembership` chưa tồn tại). Đây chính là trạng thái **🔴 RED** hợp lệ theo TDD!

---

#### Bước 1.2: 🟢 GREEN (Viết Code tối thiểu nhất để Pass)
Tạo file `src/InsuranceQuoteEngine/Domain/DiscountCalculator.cs`:

```csharp
namespace InsuranceQuoteEngine.Domain;

public enum CustomerMembership
{
    Normal,
    Premium,
    Vip
}

public class DiscountCalculator
{
    public decimal CalculateDiscount(CustomerMembership membership, decimal orderAmount)
    {
        // Trả về kết quả cứng (Hardcode) tối thiểu để bài test 1 PASS!
        return 0.0m;
    }
}
```
*Chạy test:* `dotnet test` → **🟢 GREEN (PASS)!**

---

#### Bước 1.3: 🔵 REFACTOR
Hiện tại code rất đơn giản, chưa có gì cần dọn dẹp. Sang chu kỳ tiếp theo!

---

### 🟢 Chu kỳ 2: Khách hàng Premium nhận 10% giảm giá

#### Bước 2.1: 🔴 RED (Thêm Test mới)
Thêm vào `tests/InsuranceQuoteEngine.UnitTests/Domain/TddDiscountCalculatorTests.cs`:

```csharp
    [Fact]
    public void CalculateDiscount_ShouldReturn10Percent_ForPremiumCustomer()
    {
        // Arrange
        var calculator = new DiscountCalculator();

        // Act
        var discount = calculator.CalculateDiscount(CustomerMembership.Premium, orderAmount: 1000m);

        // Assert
        discount.Should().Be(0.10m);
    }
```
*Chạy test:* Bài test này **🔴 FAIL** (Kỳ vọng `0.10m` nhưng thực tế nhận `0.0m`).

---

#### Bước 2.2: 🟢 GREEN (Sửa Production Code tối thiểu)
Sửa `src/InsuranceQuoteEngine/Domain/DiscountCalculator.cs`:

```csharp
public class DiscountCalculator
{
    public decimal CalculateDiscount(CustomerMembership membership, decimal orderAmount)
    {
        if (membership == CustomerMembership.Premium)
        {
            return 0.10m;
        }

        return 0.0m;
    }
}
```
*Chạy test:* Cả 2 bài test đều **🟢 GREEN!**

---

### 🟢 Chu kỳ 3: Khách hàng VIP nhận 20% giảm giá

#### Bước 3.1: 🔴 RED
```csharp
    [Fact]
    public void CalculateDiscount_ShouldReturn20Percent_ForVipCustomer()
    {
        // Arrange
        var calculator = new DiscountCalculator();

        // Act
        var discount = calculator.CalculateDiscount(CustomerMembership.Vip, orderAmount: 1000m);

        // Assert
        discount.Should().Be(0.20m);
    }
```
*Chạy test:* **🔴 FAIL!**

---

#### Bước 3.2: 🟢 GREEN & 🔵 REFACTOR
Sửa `DiscountCalculator.cs` bằng cấu trúc `switch expression` sạch đẹp:

```csharp
public class DiscountCalculator
{
    public decimal CalculateDiscount(CustomerMembership membership, decimal orderAmount)
    {
        return membership switch
        {
            CustomerMembership.Vip => 0.20m,
            CustomerMembership.Premium => 0.10m,
            _ => 0.0m
        };
    }
}
```
*Chạy test:* Toàn bộ 3 bài test đều **🟢 GREEN!**

---

### 🟢 Chu kỳ 4: Đơn hàng > 5,000,000 nhận thêm 5% chiết khấu

#### Bước 4.1: 🔴 RED
```csharp
    [Fact]
    public void CalculateDiscount_ShouldAdd5PercentBonus_WhenOrderAmountIsGreaterThan5Million()
    {
        // Arrange
        var calculator = new DiscountCalculator();

        // Act - Khách hàng thường nhưng mua 6,000,000 -> nhận 5% (0 + 0.05)
        var discount = calculator.CalculateDiscount(CustomerMembership.Normal, orderAmount: 6_000_000m);

        // Assert
        discount.Should().Be(0.05m);
    }

    [Fact]
    public void CalculateDiscount_ShouldAdd5PercentBonusToVip_WhenOrderAmountIsGreaterThan5Million()
    {
        // Arrange
        var calculator = new DiscountCalculator();

        // Act - VIP (20%) + Đơn lớn (5%) = 25%
        var discount = calculator.CalculateDiscount(CustomerMembership.Vip, orderAmount: 6_000_000m);

        // Assert
        discount.Should().Be(0.25m);
    }
```
*Chạy test:* Cả 2 bài test mới đều **🔴 FAIL!**

---

#### Bước 4.2: 🟢 GREEN
```csharp
public class DiscountCalculator
{
    private const decimal LargeOrderThreshold = 5_000_000m;
    private const decimal LargeOrderBonusDiscount = 0.05m;

    public decimal CalculateDiscount(CustomerMembership membership, decimal orderAmount)
    {
        var baseDiscount = membership switch
        {
            CustomerMembership.Vip => 0.20m,
            CustomerMembership.Premium => 0.10m,
            _ => 0.0m
        };

        if (orderAmount > LargeOrderThreshold)
        {
            baseDiscount += LargeOrderBonusDiscount;
        }

        return baseDiscount;
    }
}
```
*Chạy test:* Tất cả 5 bài test **🟢 GREEN!**

---

### 🟢 Chu kỳ 5: Mức giảm giá tối đa không vượt quá 30% (Max Discount Cap)

#### Bước 5.1: 🔴 RED
Giả sử trong tương lai có thêm khuyến mãi cộng dồn khiến tổng chiết khấu vượt 30%:
```csharp
    [Fact]
    public void CalculateDiscount_ShouldCapDiscountAt30Percent_WhenTotalDiscountExceedsThreshold()
    {
        // Giả sử ta thêm tham số extraPromotion = 0.15m (VIP 0.20 + bonus 0.05 + extra 0.15 = 0.40 => Phải bị giới hạn ở 0.30)
        var calculator = new DiscountCalculator();

        var discount = calculator.CalculateDiscount(CustomerMembership.Vip, orderAmount: 6_000_000m, extraCouponDiscount: 0.15m);

        discount.Should().Be(0.30m);
    }
```

---

#### Bước 5.2: 🟢 GREEN & 🔵 REFACTOR

Cập nhật `src/InsuranceQuoteEngine/Domain/DiscountCalculator.cs`:

```csharp
namespace InsuranceQuoteEngine.Domain;

public class DiscountCalculator
{
    private const decimal LargeOrderThreshold = 5_000_000m;
    private const decimal LargeOrderBonusDiscount = 0.05m;
    private const decimal MaxDiscountCap = 0.30m; // 30% tối đa

    public decimal CalculateDiscount(
        CustomerMembership membership, 
        decimal orderAmount, 
        decimal extraCouponDiscount = 0.0m)
    {
        if (orderAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(orderAmount), "Order amount cannot be negative.");
        }

        var totalDiscount = GetBaseMembershipDiscount(membership);

        if (orderAmount > LargeOrderThreshold)
        {
            totalDiscount += LargeOrderBonusDiscount;
        }

        totalDiscount += Math.Max(0m, extraCouponDiscount);

        // Áp dụng giới hạn tối đa
        return Math.Min(totalDiscount, MaxDiscountCap);
    }

    private static decimal GetBaseMembershipDiscount(CustomerMembership membership) => membership switch
    {
        CustomerMembership.Vip => 0.20m,
        CustomerMembership.Premium => 0.10m,
        _ => 0.0m
    };
}
```

---

## 🎯 3. Bài học rút ra từ thực tế TDD

1. **Không đoán mò tương lai (YAGNI - You Aren't Gonna Need It):** Chỉ viết code để phục vụ bài test hiện tại.
2. **Tự tin Refactor 100%:** Khi có mạng lưới test bảo hộ, bạn có thể tách hàm, đổi tên biến, tối ưu thuật toán mà không sợ làm gãy ứng dụng.
3. **Thiết kế API tự nhiên hơn:** Bạn đóng vai trò là "người dùng" của class trước khi bạn bắt tay vào "viết" class đó.

---

## ✅ Check-list hoàn thành Lesson 05
- [ ] Hiểu rõ 3 định luật TDD và chu kỳ Red – Green – Refactor.
- [ ] Trải nghiệm cảm giác viết test trước khi viết bất kỳ dòng production code nào.
- [ ] Hoàn thành trọn vẹn lớp `DiscountCalculator` và toàn bộ test suite.

👉 **Tiếp theo:** Chuyển sang [Lesson 06: Kỹ thuật Unit Testing Nâng cao (Parameterized, Async, Time)](./06-advanced-unit-testing.md)!
