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
        const decimal discount = 0.15m; // 15% of 99.99 = 14.9985 => remaining 84.9915

        // Act
        var result = _sut.Calculate(price, discount);

        // Assert
        result.Should().Be(84.9915m);
    }

    // Calculate with tax
    [Fact]
    public void CalculateWithTax_ShouldReturnOriginalPrice_WhenDiscountIsZeroAndTaxIsZero()  
    {
        // Arrange
        const decimal price = 100m;
        const decimal discount = 0m;
        const decimal taxRate = 0m;

        // Act
        var result = _sut.CalculateWithTax(price, discount, taxRate);

        // Assert
        result.Should().Be(100m);
    }

    [Fact]
    public void CalculateWithTax_ShouldReturnDiscountedPrice_WhenDiscountIsTwentyPercentAndTaxIsZero()  
    {
        // Arrange
        const decimal price = 100m;
        const decimal discount = 0.20m;
        const decimal taxRate = 0m;

        // Act
        var result = _sut.CalculateWithTax(price, discount, taxRate);

        // Assert
        result.Should().Be(80m);
    }

    [Fact]
    public void CalculateWithTax_ShouldReturnZero_WhenDiscountIsOneHundredPercentAndTaxIsZero()  
    {
        // Arrange
        const decimal price = 100m;
        const decimal discount = 1.0m;
        const decimal taxRate = 0m;

        // Act
        var result = _sut.CalculateWithTax(price, discount, taxRate);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateWithTax_ShouldReturnZero_WhenOriginalPriceIsZeroAndTaxIsZero()  
    {
        // Arrange
        const decimal price = 0m;
        const decimal discount = 0.50m;
        const decimal taxRate = 0m;

        // Act
        var result = _sut.CalculateWithTax(price, discount, taxRate);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateWithTax_ShouldHandleFractionalAmounts_WhenDiscountProducesDecimalsAndTaxIsZero()  
    {
        // Arrange
        const decimal price = 99.99m;
        const decimal discount = 0.15m; // 15% of 99.99 = 14.9985 => remaining 84.9915
        const decimal taxRate = 0m;

        // Act
        var result = _sut.CalculateWithTax(price, discount, taxRate);

        // Assert
        result.Should().Be(84.9915m);
    }

    [Fact]
    public void CalculateWithTax_ShouldReturnOriginalPrice_WhenDiscountIsZeroAndTaxIsTenPercent()
    {
        // Arrange
        const decimal price = 100m;
        const decimal discount = 0m;
        const decimal taxRate = 0.10m;

        // Act
        var result = _sut.CalculateWithTax(price, discount, taxRate);

        // Assert
        result.Should().Be(110m);
    }

    [Fact]
    public void CalculateWithTax_ShouldReturnDiscountedPriceWithTax_WhenDiscountIsTwentyPercentAndTaxIsTenPercent()  
    {
        // Arrange
        const decimal price = 100m;
        const decimal discount = 0.20m;
        const decimal taxRate = 0.10m;

        // Act
        var result = _sut.CalculateWithTax(price, discount, taxRate);

        // Assert
        result.Should().Be(88m);
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

    // CalculateWithTax
    [Fact]
    public void CalculateWithTax_ShouldThrowArgumentOutOfRangeException_WhenOriginalPriceIsNegative()
    {
        // Arrange
        const decimal negativePrice = -100m;
        const decimal discount = 0.20m;
        const decimal taxRate = 0.10m;

        // Act
        Action act = () => _sut.CalculateWithTax(negativePrice, discount, taxRate);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("originalPrice")
           .WithMessage("*Price cannot be negative*");
    }

    [Fact]
    public void CalculateWithTax_ShouldThrowArgumentOutOfRangeException_WhenDiscountIsLessThanZero()
    {
        // Arrange
        const decimal price = 100m;
        const decimal negativeDiscount = -0.01m;
        const decimal taxRate = 0.10m;

        // Act
        Action act = () => _sut.CalculateWithTax(price, negativeDiscount, taxRate);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("discountPercentage")
           .WithMessage("*Discount percentage must be between 0.0 and 1.0*");
    }

    [Fact]
    public void CalculateWithTax_ShouldThrowArgumentOutOfRangeException_WhenDiscountIsGreaterThanOne()
    {
        // Arrange
        const decimal price = 100m;
        const decimal invalidDiscount = 1.01m;
        const decimal taxRate = 0.10m;

        // Act
        Action act = () => _sut.CalculateWithTax(price, invalidDiscount, taxRate);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("discountPercentage")
           .WithMessage("*Discount percentage must be between 0.0 and 1.0*");
    }

    [Fact]
    public void CalculateWithTax_ShouldThrowArgumentOutOfRangeException_WhenTaxRateIsLessThanZero()
    {
        // Arrange
        const decimal price = 100m;
        const decimal discount = 0.20m;
        const decimal negativeTaxRate = -0.01m;

        // Act
        Action act = () => _sut.CalculateWithTax(price, discount, negativeTaxRate);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("taxRate")
           .WithMessage("*Tax rate must be between 0.0 and 0.5*");
    }

    [Fact]
    public void CalculateWithTax_ShouldThrowArgumentOutOfRangeException_WhenTaxRateIsGreaterThanOne()
    {
        // Arrange
        const decimal price = 100m;
        const decimal discount = 0.20m;
        const decimal invalidTaxRate = 0.51m;

        // Act
        Action act = () => _sut.CalculateWithTax(price, discount, invalidTaxRate);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("taxRate")
           .WithMessage("*Tax rate must be between 0.0 and 0.5*");
    }

    #endregion
}