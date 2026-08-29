using InsuranceQuoteEngine.Domain;

namespace InsuranceQuoteEngine.UnitTests.Domain.TestData;

public class QuoteTestData
{
    public static IEnumerable<object[]> GetDiscountScenarios()
    {
        yield return new object[] { CustomerMembership.Normal, 1000m, 0.0m };
        yield return new object[] { CustomerMembership.Premium, 1000m, 0.10m };
        yield return new object[] { CustomerMembership.Vip, 1000m, 0.20m };
        yield return new object[] { CustomerMembership.Normal, 6_000_000m, 0.05m }; // 5% bonus for order > 5M
        yield return new object[] { CustomerMembership.Premium, 6_000_000m, 0.15m }; // 10% + 5%
        yield return new object[] { CustomerMembership.Vip, 6_000_000m, 0.25m }; // 20% + 5%
    }
}
