# Lesson 00: Thiết lập môi trường & Cấu trúc dự án chuẩn

## 🎯 Mục tiêu bài học
- Cài đặt và chuẩn bị môi trường phát triển (.NET SDK, IDE/Editor).
- Khởi tạo Solution và phân chia cấu trúc thư mục chuẩn giữa `src/` (mã nguồn ứng dụng) và `tests/` (mã nguồn kiểm thử).
- Cài đặt các thư viện kiểm thử chuẩn mực: **xUnit**, **FluentAssertions**, **Moq**, **Coverlet**.
- Chạy thử nghiệm lệnh kiểm thử đầu tiên qua .NET CLI.

---

## 📋 1. Chuẩn bị môi trường (Prerequisites)

1. **.NET SDK:** Phiên bản .NET 8.0 hoặc mới hơn.
   - Kiểm tra bằng lệnh:
     ```bash
     dotnet --version
     ```
2. **IDE / Code Editor:**
   - Visual Studio 2022 (với workload ".NET desktop" hoặc "ASP.NET and web development"), hoặc
   - JetBrains Rider, hoặc
   - Visual Studio Code với các extension:
     - *C# Dev Kit* hoặc *OmniSharp*
     - *.NET Core Test Explorer*

---

## 🏗️ 2. Cấu trúc thư mục mục tiêu

Chúng ta sẽ xây dựng cấu trúc dự án mẫu có tên `InsuranceQuoteEngine` (sẽ dùng xuyên suốt khóa học):

```text
TDD/
├── src/
│   └── InsuranceQuoteEngine/
│       ├── Domain/
│       │   ├── Models/
│       │   └── Exceptions/
│       ├── Application/
│       │   ├── Services/
│       │   └── Interfaces/
│       └── Infrastructure/
│
├── tests/
│   ├── InsuranceQuoteEngine.UnitTests/
│   │   ├── Domain/
│   │   ├── Application/
│   │   ├── Builders/
│   │   └── Fixtures/
│   │
│   └── InsuranceQuoteEngine.IntegrationTests/
│
├── InsuranceQuoteEngine.sln
└── README.md
```

---

## 🚀 3. Hướng dẫn Step-by-Step tạo Solution & Projects

Mở Terminal tại thư mục gốc của dự án (`d:/LEARN BY MYSELF/TDD`) và thực hiện lần lượt các bước sau:

### Bước 3.1: Khởi tạo Solution File
```bash
dotnet new sln -n InsuranceQuoteEngine
```

### Bước 3.2: Tạo Project mã nguồn (Class Library)
```bash
dotnet new classlib -o src/InsuranceQuoteEngine -n InsuranceQuoteEngine -f net8.0
```

### Bước 3.3: Tạo Project Unit Tests (xUnit)
```bash
dotnet new xunit -o tests/InsuranceQuoteEngine.UnitTests -n InsuranceQuoteEngine.UnitTests -f net8.0
```

### Bước 3.4: Thêm các Projects vào Solution
```bash
dotnet sln add src/InsuranceQuoteEngine/InsuranceQuoteEngine.csproj
dotnet sln add tests/InsuranceQuoteEngine.UnitTests/InsuranceQuoteEngine.UnitTests.csproj
```

### Bước 3.5: Tham chiếu Project Unit Tests sang Project mã nguồn
Để dự án Unit Test có thể gọi và kiểm thử các class trong `InsuranceQuoteEngine`:
```bash
dotnet add tests/InsuranceQuoteEngine.UnitTests/InsuranceQuoteEngine.UnitTests.csproj reference src/InsuranceQuoteEngine/InsuranceQuoteEngine.csproj
```

---

## 📦 4. Cài đặt các thư viện hỗ trợ Test cần thiết

Chuyển vào thư mục test hoặc thêm trực tiếp các package thiết yếu:

```bash
# 1. FluentAssertions: Giúp viết assertion theo phong cách fluent, dễ đọc, báo lỗi chi tiết
dotnet add tests/InsuranceQuoteEngine.UnitTests package FluentAssertions

# 2. Moq: Thư viện tạo Mock/Stub mạnh mẽ và phổ biến nhất trong .NET
dotnet add tests/InsuranceQuoteEngine.UnitTests package Moq

# 3. Coverlet Collector: Hỗ trợ đo lường Code Coverage
dotnet add tests/InsuranceQuoteEngine.UnitTests package coverlet.collector
```

Kiểm tra file `tests/InsuranceQuoteEngine.UnitTests/InsuranceQuoteEngine.UnitTests.csproj` sẽ có nội dung tương tự như sau:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="xunit" Version="2.5.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\InsuranceQuoteEngine\InsuranceQuoteEngine.csproj" />
  </ItemGroup>

</Project>
```

---

## 🧪 5. Kiểm tra thiết lập bằng Sanity Test

Hãy tạo một test đơn giản để đảm bảo `xUnit` và `FluentAssertions` hoạt động bình thường.

Tạo file `tests/InsuranceQuoteEngine.UnitTests/SanityCheckTests.cs`:
```csharp
using FluentAssertions;
using Xunit;

namespace InsuranceQuoteEngine.UnitTests;

public class SanityCheckTests
{
    [Fact]
    public void Environment_ShouldBeProperlyConfigured()
    {
        // Arrange
        const int a = 10;
        const int b = 20;

        // Act
        const int sum = a + b;

        // Assert
        sum.Should().Be(30);
    }
}
```

Chạy lệnh test trên Terminal:
```bash
dotnet test
```

**Kết quả mong đợi:**
```text
Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: ...ms
```

---

## ✅ Check-list hoàn thành Lesson 00
- [ ] Đã cài đặt .NET 8 SDK và kiểm tra phiên bản thành công.
- [ ] Đã tạo Solution `InsuranceQuoteEngine.sln`.
- [ ] Đã tạo project `InsuranceQuoteEngine` trong `src/` và project `InsuranceQuoteEngine.UnitTests` trong `tests/`.
- [ ] Đã cài đặt `FluentAssertions`, `Moq`, `coverlet.collector`.
- [ ] Chạy lệnh `dotnet test` thành công và pass bài test kiểm tra.

👉 **Tiếp theo:** Chuyển sang [Lesson 01: Unit Testing Fundamentals & xUnit](./01-unit-testing-fundamentals.md) để bắt đầu học viết Unit Test bài bản!
