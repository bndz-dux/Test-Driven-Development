namespace InsuranceQuoteEngine.Domain;

public class InsuranceQuoteEngineService
{
    private const decimal MinInsurablePropertyValue = 100_000_000m; // 100 triệu VND
    private const decimal BaseRateMultiplier = 0.001m;              // 0.1%
    private const int QuoteValidityDays = 30;

    private readonly IClock _clock;

    public InsuranceQuoteEngineService(IClock clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public QuoteResult GenerateQuote(GenerateQuoteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _clock.UtcNow;
        var quoteId = Guid.NewGuid();

        // 1. Kiểm tra các điều kiện từ chối (Decline Rules - BR-01, BR-04)
        if (request.Customer.Age < 18 || request.Customer.Age > 75)
        {
            return new QuoteResult(
                quoteId,
                QuoteStatus.Declined,
                BasePremium: 0m,
                FinalPremium: 0m,
                DecisionReason: "Customer age is ineligible for insurance. Must be between 18 and 75.",
                GeneratedAtUtc: now,
                ExpiresAtUtc: now);
        }

        if (request.Property.EstimatedValue < MinInsurablePropertyValue)
        {
            return new QuoteResult(
                quoteId,
                QuoteStatus.Declined,
                BasePremium: 0m,
                FinalPremium: 0m,
                DecisionReason: $"Property value is below insurable limit of {MinInsurablePropertyValue:N0} VND.",
                GeneratedAtUtc: now,
                ExpiresAtUtc: now);
        }

        // 2. Kiểm tra các điều kiện chuyển chuyên viên duyệt (Referral Rules - BR-02, BR-03)
        if (request.Property.IsInFloodZone)
        {
            return new QuoteResult(
                quoteId,
                QuoteStatus.Referred,
                BasePremium: 0m,
                FinalPremium: 0m,
                DecisionReason: "Property is in a designated flood hazard zone.",
                GeneratedAtUtc: now,
                ExpiresAtUtc: now.AddDays(QuoteValidityDays));
        }

        if (request.Property.YearBuilt < 1950)
        {
            return new QuoteResult(
                quoteId,
                QuoteStatus.Referred,
                BasePremium: 0m,
                FinalPremium: 0m,
                DecisionReason: "Property built prior to 1950 requires structural underwriting.",
                GeneratedAtUtc: now,
                ExpiresAtUtc: now.AddDays(QuoteValidityDays));
        }

        // 3. Tính phí cơ sở (Base Premium - BR-05)
        var basePremium = request.Property.EstimatedValue * BaseRateMultiplier;

        // 4. Áp dụng hệ số gói bảo hiểm (Coverage Tier - BR-06)
        var coverageMultiplier = request.Coverage switch
        {
            CoverageTier.Standard => 1.25m,
            CoverageTier.Comprehensive => 1.60m,
            _ => 1.0m // Basic
        };

        var premiumAfterCoverage = basePremium * coverageMultiplier;

        // 5. Tính toán chiết khấu khách hàng (Customer Discounts & Claims - BR-07, BR-08)
        var discountPercentage = 0.0m;
        if (!request.Customer.HasPastClaims)
        {
            discountPercentage = request.Customer.Type switch
            {
                CustomerType.Vip => 0.20m,
                CustomerType.Premium => 0.10m,
                _ => 0.0m
            };
        }

        var finalPremium = premiumAfterCoverage * (1.0m - discountPercentage);

        return new QuoteResult(
            quoteId,
            QuoteStatus.Approved,
            BasePremium: basePremium,
            FinalPremium: Math.Round(finalPremium, 2),
            DecisionReason: "Quote automatically approved by underwriting rules.",
            GeneratedAtUtc: now,
            ExpiresAtUtc: now.AddDays(QuoteValidityDays));
    }
}
