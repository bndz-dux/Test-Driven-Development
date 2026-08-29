using FluentAssertions;
using Xunit;
using InsuranceQuoteEngine.Domain;

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

    [Fact]
    public void CalculateDiscount_ShouldCapDiscountAt30Percent_WhenTotalDiscountExceedsThreshold()
    {
        // Giả sử ta thêm tham số extraPromotion = 0.15m (VIP 0.20 + bonus 0.05 + extra 0.15 = 0.40 => Phải bị giới hạn ở 0.30)
        var calculator = new DiscountCalculator();

        var discount = calculator.CalculateDiscount(CustomerMembership.Vip, orderAmount: 6_000_000m, extraCouponDiscount: 0.15m);

        discount.Should().Be(0.30m);
    }
}