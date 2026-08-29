# Lesson 11: Nhận diện Test Smells & Nghệ thuật Refactor Test (Test Refactoring)

## 🎯 Mục tiêu bài học
- Nhận diện **10 "Mùi hôi" trong mã kiểm thử (Test Smells)** nguy hiểm nhất khiến test suite trở thành gánh nặng bảo trì.
- Nắm vững các kỹ thuật Refactor Test Code tương tự như Refactor Production Code.
- Chuyển đổi các bài test dài dòng, dễ gãy, phụ thuộc trật tự thành các bài test sạch đẹp, dễ đọc trong 10 giây và bền vững trước mọi thay đổi thuật toán.
- Áp dụng **Quy tắc 5 câu hỏi vàng** khi review bất kỳ bài Unit Test nào.
- Tổng kết toàn bộ lộ trình TDD Mastery.

---

## 👃 1. Danh mục 10 Test Smells phổ biến & Cách khắc phục

### 1.1. Smell 1: The Mystery Guest (Vị khách bí ẩn / Magic Values)
- **Dấu hiệu:** Bài test sử dụng các giá trị bí ẩn (ví dụ: `42`, `"X"`, `ID: 99`) mà không giải thích tại sao lại dùng số đó, hoặc dựa vào file ngoài hay dữ liệu seed ngầm định trong DB.
- **Tác hại:** Người đọc test không hiểu logic vì sao kết quả lại ra như vậy.
- **Khắc phục:** Định nghĩa rõ ràng hằng số hoặc dùng Test Data Builder với tên biến tường minh:
  ```csharp
  // ❌ Mystery Guest
  var result = calculator.Calculate(100, 42); // 42 là gì?
  
  // ✅ Tường minh
  const decimal highRiskCustomerScore = 42m;
  var result = calculator.Calculate(originalPrice: 100m, riskScore: highRiskCustomerScore);
  ```

---

### 1.2. Smell 2: Logic in Tests (Chứa IF / ELSE / FOR trong Test)
- **Dấu hiệu:** Viết vòng lặp `for`, câu lệnh `if...else`, `switch`, `try...catch` thủ công bên trong method test.
- **Tác hại:** Test code quá phức tạp, có thể chính bài test lại chứa bug tiềm ẩn!
- **Khắc phục:** Loại bỏ mọi câu lệnh điều kiện. Tách thành nhiều bài test riêng biệt hoặc dùng `[Theory]` với `[InlineData]`.

---

### 1.3. Smell 3: Over-Mocking & Implementation Detail Verification
- **Dấu hiệu:** Mock quá nhiều class nội bộ, hoặc gọi `Verify` kiểm tra từng method private/internal được gọi theo thứ tự nào.
- **Tác hại:** Test cực kỳ "giòn" (Brittle). Chỉ cần đổi tên một method nội bộ hoặc thay đổi cấu trúc class là hàng chục test đỏ dù chức năng vẫn đúng.
- **Khắc phục:** Chỉ mock các I/O boundary (Database, Third-party API). Kiểm tra kết quả đầu ra (State/Output) thay vì kiểm tra luồng nội bộ.

---

### 1.4. Smell 4: Flaky Tests (Test chập chờn)
- **Dấu hiệu:** Lúc xanh, lúc đỏ mà không thay đổi bất kỳ dòng code nào. Thường do dùng `DateTime.Now`, `Random`, `Thread.Sleep()`, hoặc phụ thuộc vào tốc độ mạng.
- **Khắc phục:** Thay `DateTime.Now` bằng `IClock` / `TimeProvider`. Tuyệt đối không dùng `Thread.Sleep()`, thay bằng cơ chế điều khiển ảo hoặc đồng bộ rõ ràng.

---

### 1.5. Smell 5: Interdependent Tests (Test phụ thuộc thứ tự chạy)
- **Dấu hiệu:** Test B chỉ pass nếu Test A chạy trước (do dùng chung biến `static`, file tạm, hoặc bản ghi DB chưa dọn dẹp).
- **Khắc phục:** Đảm bảo mỗi bài test hoàn toàn độc lập (Isolated). Khởi tạo mới SUT trong constructor hoặc dùng Fixture sạch cho từng test.

---

### 1.6. Smell 6: Assert Roulette (Quá nhiều Assert không rõ lý do)
- **Dấu hiệu:** Một test method gọi 20 lệnh `Assert` liên tiếp cho nhiều hành vi khác nhau. Khi dòng thứ 3 fail, các dòng sau không được chạy và không biết toàn cảnh lỗi ra sao.
- **Khắc phục:** Một bài test chỉ nên kiểm chứng **một hành vi logic duy nhất**. Nếu cần kiểm tra nhiều trường của cùng 1 đối tượng, hãy dùng `using (new AssertionScope())` của FluentAssertions:
  ```csharp
  using (new AssertionScope())
  {
      quote.Status.Should().Be(QuoteStatus.Approved);
      quote.BasePremium.Should().Be(1000m);
      quote.FinalPremium.Should().Be(800m);
  }
  ```

---

## 🛠️ 2. Thực hành Refactor: Trước và Sau (Before vs After)

### ❌ Mã nguồn Test ban đầu (Chứa đầy Test Smells):
```csharp
[Fact]
public void Test1()
{
    // Smell: Tên vô nghĩa, Magic numbers, Clutter dữ liệu, Test logic phức tạp
    var c = new Customer { Id = Guid.NewGuid(), Name = "A", Email = "a@b.com", Address = "xyz", Age = 30, Status = CustomerStatus.Active };
    var p = new Property { Id = Guid.NewGuid(), Address = "HN", Value = 1000000000m, Year = 2010, InFlood = false };
    
    var repoMock = new Mock<ICustomerRepository>();
    repoMock.Setup(r => r.Get(c.Id)).ReturnsAsync(c);
    var emailMock = new Mock<IEmailSender>();

    var service = new LegacyQuoteService(repoMock.Object, emailMock.Object);
    var res = service.Process(c.Id, p, 1);

    if (res.Amount > 1000)
    {
        Assert.True(res.Success);
    }
    
    repoMock.Verify(r => r.Get(c.Id), Times.Once);
    emailMock.Verify(e => e.Send(It.IsAny<string>()), Times.Once);
}
```

---

### ✅ Mã nguồn Test sau khi Refactor chuẩn mực:
```csharp
[Fact]
public async Task ProcessQuoteAsync_ShouldApproveAndCalculatePremium_WhenCustomerAndPropertyAreStandard()
{
    // Arrange: Tường minh, dùng Builder, chỉ giữ dữ liệu mấu chốt
    var customer = new CustomerProfileBuilder()
        .WithAge(30)
        .Build();

    var property = new PropertyDetailsBuilder()
        .WithEstimatedValue(1_000_000_000m)
        .Build();

    var customerRepoMock = new Mock<ICustomerRepository>();
    customerRepoMock
        .Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(customer);

    var sut = new QuoteApplicationService(customerRepoMock.Object);

    // Act: Thực thi rõ ràng
    var result = await sut.ProcessQuoteAsync(customer.Id, property, CoverageTier.Basic);

    // Assert: Gom nhóm kiểm tra hành vi, không còn IF/ELSE, không over-mock
    using (new AssertionScope())
    {
        result.Status.Should().Be(QuoteStatus.Approved);
        result.FinalPremium.Should().Be(1_000_000m);
    }
}
```

---

## 🔍 3. Quy tắc 5 câu hỏi vàng khi Code Review Test

Trước khi chấp thuận bất kỳ bài test nào vào codebase, hãy tự hỏi 5 câu:

```text
1. ⏱️ 10 SECONDS RULE: Tôi có thể đọc hiểu mục đích của bài test này trong vòng 10 giây không?
2. 📖 BUSINESS DOCUMENTATION: Tên test và các bước AAA có mô tả chính xác quy tắc nghiệp vụ không?
3. 🎲 DETERMINISTIC: Bài test này có đảm bảo luôn luôn cho cùng 1 kết quả ở mọi môi trường không?
4. 🎯 RIGHT REASON: Khi test bị đỏ, nó có báo đúng lỗi nghiệp vụ vừa bị phá vỡ không?
5. 🛡️ REFACTOR RESILIENT: Nếu tôi đổi thuật toán nội bộ mà không đổi đầu ra, bài test này có bị vỡ vô lý không?
```

---

## 🏆 4. Bảng kiểm duyệt tổng kết (Definition of Done Mastery)

Chúc mừng bạn đã hoàn thành toàn bộ 12 bài học của lộ trình TDD Mastery! Hãy tự đối chiếu bản thân với bảng tiêu chuẩn cuối cùng:

- [x] **xUnit & AAA:** Thành thạo cấu trúc Arrange – Act – Assert và `[Fact]`, `[Theory]`.
- [x] **Naming Standard:** 100% test đặt tên theo chuẩn `Method_ShouldExpectedBehavior_WhenCondition`.
- [x] **Boundary Coverage:** Luôn kiểm tra các điểm biên và phân vùng tương đương.
- [x] **Moq Mastery:** Cô lập đúng I/O dependencies, không over-mock domain models.
- [x] **Test Data Builders:** Tạo thư viện Builder sạch đẹp, loại bỏ hoàn toàn Test Clutter.
- [x] **TDD Cycle:** Phản xạ tự nhiên với chu trình RED → GREEN → REFACTOR.
- [x] **Time Control:** Kiểm soát 100% logic phụ thuộc thời gian bằng `IClock` / `TimeProvider`.
- [x] **Mutation Testing:** Sử dụng Stryker.NET và đạt Mutation Score ≥ 80%.
- [x] **Code Coverage:** Đo độ phủ với Coverlet, xem báo cáo ReportGenerator, đạt Line ≥ 80% & Branch ≥ 75%.
- [x] **CI/CD Quality Gates:** Tự động hóa kiểm tra chất lượng trên GitHub Actions / Azure DevOps.
- [x] **Clean Test Code:** Nhận diện và loại bỏ hoàn toàn các Test Smells.

---

> 🚀 **Lời kết:**  
> *"Mục tiêu tối thượng của TDD và Unit Testing không phải là để viết thật nhiều test đối phó.  
> Mục tiêu là tạo ra một bộ khung bảo hiểm vững chắc giúp bạn và đồng đội **luôn tự tin thay đổi, nâng cấp và bàn giao phần mềm** mà không bao giờ phải lo sợ làm gãy hệ thống!"*
