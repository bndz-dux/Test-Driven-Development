# Lesson 00: Environment Setup & Project Structure

## 🎯 Lesson Objectives
- Install and prepare the development environment (.NET SDK, IDE/Editor).
- Initialize the Solution and establish a standard folder structure separating `src/` (application source code) and `tests/` (test source code).
- Install industry-standard testing libraries: **xUnit**, **FluentAssertions**, **Moq**, and **Coverlet**.
- Execute the first test suite run using the .NET CLI.

---

## 📋 1. Prerequisites

1. **.NET SDK:** Version .NET 8.0 or newer.
   - Verify installation with:
     ```bash
     dotnet --version
     ```
2. **IDE / Code Editor:**
   - Visual Studio 2022 (with ".NET desktop" or "ASP.NET and web development" workload), or
   - JetBrains Rider, or
   - Visual Studio Code with extensions:
     - *C# Dev Kit* or *OmniSharp*
     - *.NET Core Test Explorer*

---

## 🏗️ 2. Target Directory Structure

We will build a reference project named `InsuranceQuoteEngine` (used throughout the entire course):

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

## 🚀 3. Step-by-Step Solution & Project Setup Guide

Open your terminal at the root project directory (`d:/LEARN BY MYSELF/TDD`) and run the following commands in order:

### Step 3.1: Initialize the Solution File
```bash
dotnet new sln -n InsuranceQuoteEngine
```

### Step 3.2: Create the Source Class Library Project
```bash
dotnet new classlib -o src/InsuranceQuoteEngine -n InsuranceQuoteEngine -f net8.0
```

### Step 3.3: Create the xUnit Test Project
```bash
dotnet new xunit -o tests/InsuranceQuoteEngine.UnitTests -n InsuranceQuoteEngine.UnitTests -f net8.0
```

### Step 3.4: Add Projects to the Solution
```bash
dotnet sln add src/InsuranceQuoteEngine/InsuranceQuoteEngine.csproj
dotnet sln add tests/InsuranceQuoteEngine.UnitTests/InsuranceQuoteEngine.UnitTests.csproj
```

### Step 3.5: Reference the Source Project from the Test Project
To allow the test project to invoke and verify types in `InsuranceQuoteEngine`:
```bash
dotnet add tests/InsuranceQuoteEngine.UnitTests/InsuranceQuoteEngine.UnitTests.csproj reference src/InsuranceQuoteEngine/InsuranceQuoteEngine.csproj
```

---

## 📦 4. Install Essential Testing Packages

Add the required testing packages to the test project:

```bash
# 1. FluentAssertions: Fluent, highly readable assertion style with rich failure messages
dotnet add tests/InsuranceQuoteEngine.UnitTests package FluentAssertions

# 2. Moq: Powerful and popular mocking framework for .NET
dotnet add tests/InsuranceQuoteEngine.UnitTests package Moq

# 3. Coverlet Collector: Cross-platform code coverage collection
dotnet add tests/InsuranceQuoteEngine.UnitTests package coverlet.collector
```

Your `tests/InsuranceQuoteEngine.UnitTests/InsuranceQuoteEngine.UnitTests.csproj` should look similar to:
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

## 🧪 5. Verify Setup with a Sanity Test

Create a simple sanity test to verify that `xUnit` and `FluentAssertions` are functioning correctly.

Create file `tests/InsuranceQuoteEngine.UnitTests/SanityCheckTests.cs`:
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

Run tests from the terminal:
```bash
dotnet test
```

**Expected output:**
```text
Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: ...ms
```

---

## ✅ Lesson 00 Completion Checklist
- [ ] Installed .NET 8 SDK and confirmed version.
- [ ] Created `InsuranceQuoteEngine.sln`.
- [ ] Created `InsuranceQuoteEngine` project in `src/` and `InsuranceQuoteEngine.UnitTests` in `tests/`.
- [ ] Installed `FluentAssertions`, `Moq`, and `coverlet.collector`.
- [ ] Executed `dotnet test` successfully and verified the sanity test passes.

👉 **Next Step:** Proceed to [Lesson 01: Unit Testing Fundamentals & xUnit](./01-unit-testing-fundamentals.md) to begin writing structured unit tests!
