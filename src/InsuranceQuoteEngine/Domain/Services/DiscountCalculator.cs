namespace InsuranceQuoteEngine.Domain;

public enum CustomerMembership
{
    Normal,
    Premium,
    Vip
}

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