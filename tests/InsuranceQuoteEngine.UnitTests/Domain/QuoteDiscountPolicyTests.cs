using FluentAssertions;
using InsuranceQuoteEngine.Domain;
using InsuranceQuoteEngine.UnitTests.Builders;
using Xunit;

namespace InsuranceQuoteEngine.UnitTests.Domain;

public class QuoteDiscountPolicyTests
{
    // Helper function to calculate discount based on customer profile
    private decimal CalculateDiscountPercentage(CustomerProfile customer)
    {
        return customer.Type switch
        {
            CustomerType.Vip => 0.20m,      // 20%
            CustomerType.Premium => 0.10m,  // 10%
            _ => 0m                         // 0%
        };
    }

    [Fact]
    public void CalculateDiscount_ShouldReturn20Percent_WhenCustomerIsVip()
    {
        // Arrange - Expressive and clean with no redundant noise
        var vipCustomer = new CustomerProfileBuilder()
            .AsVip()
            .Build();

        // Act
        var discount = CalculateDiscountPercentage(vipCustomer);

        // Assert
        discount.Should().Be(0.20m);
    }

    [Fact]
    public void CalculateDiscount_ShouldReturn10Percent_WhenCustomerIsPremium()
    {
        // Arrange
        var premiumCustomer = new CustomerProfileBuilder()
            .AsPremium()
            .Build();

        // Act
        var discount = CalculateDiscountPercentage(premiumCustomer);

        // Assert
        discount.Should().Be(0.10m);
    }

    [Fact]
    public void CalculateDiscount_ShouldReturnZero_WhenCustomerIsStandard()
    {
        // Arrange - Using default value (Standard)
        var standardCustomer = new CustomerProfileBuilder().Build();

        // Act
        var discount = CalculateDiscountPercentage(standardCustomer);

        // Assert
        discount.Should().Be(0.0m);
    }

    [Fact]
    public void CustomerProfileBuilder_ImplicitConversion_ShouldInstantiateProfileDirectly()
    {
        // Arrange & Act - Using implicit operator
        CustomerProfile customer = new CustomerProfileBuilder()
            .WithFullName("Alice Smith")
            .WithAge(28)
            .AsVip();

        // Assert
        customer.FullName.Should().Be("Alice Smith");
        customer.Age.Should().Be(28);
        customer.Type.Should().Be(CustomerType.Vip);
        customer.PostalCode.Should().Be("70000"); // Valid default
    }

    [Fact]
    public void PropertyDetailsBuilder_ShouldSetCustomValuesCorrectly()
    {
        // Arrange & Act
        PropertyDetails property = new PropertyDetailsBuilder()
            .WithAddress("456 Le Loi, District 1")
            .WithYearBuilt(2022)
            .WithEstimatedValue(2_500_000m)
            .InFloodZone(true);

        // Assert
        property.Address.Should().Be("456 Le Loi, District 1");
        property.YearBuilt.Should().Be(2022);
        property.EstimatedValue.Should().Be(2_500_000m);
        property.IsInFloodZone.Should().BeTrue();
    }

    [Fact]
    public void QuoteBuilder_NestedConfiguration_ShouldCreateComplexQuoteCleanly()
    {
        // Demonstrates expressive nested builder syntax
        InsuranceQuote quote = new InsuranceQuoteBuilder()
            .WithCustomer(c => c.AsVip().WithAge(45))
            .WithProperty(p => p.InFloodZone(true).WithEstimatedValue(2_000_000m))
            .WithBasePremium(1500m)
            .WithFinalPremium(1200m);

        quote.Customer.Type.Should().Be(CustomerType.Vip);
        quote.Customer.Age.Should().Be(45);
        quote.Property.IsInFloodZone.Should().BeTrue();
        quote.Property.EstimatedValue.Should().Be(2_000_000m);
        quote.BasePremium.Should().Be(1500m);
        quote.FinalPremium.Should().Be(1200m);
    }

    [Fact]
    public void QuoteBuilder_SpecialStatusFlags_ShouldBeReflectedInResult()
    {
        // Arrange & Act
        InsuranceQuote referredQuote = new InsuranceQuoteBuilder().AsReferred().Build();
        InsuranceQuote declinedQuote = new InsuranceQuoteBuilder().AsDeclined().Build();
        InsuranceQuote expiredQuote = new InsuranceQuoteBuilder().Expired().Build();

        // Assert
        referredQuote.IsReferredToUnderwriter.Should().BeTrue();
        declinedQuote.IsDeclined.Should().BeTrue();
        expiredQuote.ExpiresAtUtc.Should().BeBefore(DateTime.UtcNow);
    }
}
