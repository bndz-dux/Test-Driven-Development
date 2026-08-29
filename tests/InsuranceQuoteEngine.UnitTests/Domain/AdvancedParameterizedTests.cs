using FluentAssertions;
using InsuranceQuoteEngine.Domain;
using InsuranceQuoteEngine.UnitTests.Domain.TestData;
using Xunit;

namespace InsuranceQuoteEngine.UnitTests.Domain;

public class AdvancedParameterizedTests
{
    #region 1. Theory with InlineData

    [Theory]
    [InlineData(100, 0.0, 100)]
    [InlineData(100, 0.2, 80)]
    [InlineData(100, 1.0, 0)]
    [InlineData(200, 0.5, 100)]
    public void Calculate_ShouldReturnExpectedPrice_ForVariousDiscounts(
        decimal price, 
        decimal discount, 
        decimal expected)
    {
        // Arrange
        var calculator = new PriceCalculator();

        // Act
        var result = calculator.Calculate(price, discount);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region 2. Theory with MemberData

    [Theory]
    [MemberData(nameof(QuoteTestData.GetDiscountScenarios), MemberType = typeof(QuoteTestData))]
    public void CalculateDiscount_ShouldMatchExpectedScenario_WhenUsingMemberData(
        CustomerMembership membership, 
        decimal amount, 
        decimal expectedDiscount)
    {
        // Arrange
        var calculator = new DiscountCalculator();

        // Act
        var result = calculator.CalculateDiscount(membership, amount);

        // Assert
        result.Should().Be(expectedDiscount);
    }

    #endregion

    #region 3. Theory with ClassData

    [Theory]
    [ClassData(typeof(InvalidPostalCodesClassData))]
    public void Validate_ShouldRejectInvalidPostalCodes_WhenUsingClassData(string invalidPostalCode)
    {
        // Arrange
        var validator = new CustomerEligibilityValidator();
        var applicant = new CustomerApplicant("John Doe", 30, invalidPostalCode);

        // Act
        var result = validator.Validate(applicant);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Postal code"));
    }

    #endregion
}
