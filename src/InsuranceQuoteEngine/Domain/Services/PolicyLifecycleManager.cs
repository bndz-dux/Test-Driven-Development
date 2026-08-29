namespace InsuranceQuoteEngine.Domain;

public class PolicyLifecycleManager
{
    private readonly IClock _clock;

    public PolicyLifecycleManager(IClock clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public Policy ActivateQuote(InsuranceQuote quote)
    {
        ArgumentNullException.ThrowIfNull(quote);

        var now = _clock.UtcNow;

        if (now > quote.ExpiresAtUtc)
        {
            throw new QuoteExpiredException(quote.Id);
        }

        return new Policy(
            Id: Guid.NewGuid(),
            QuoteId: quote.Id,
            EffectiveDateUtc: now,
            ExpiryDateUtc: now.AddYears(1),
            AnnualPremium: quote.FinalPremium,
            IsActive: true
        );
    }

    public decimal CalculateRefundOnCancellation(Policy policy, DateTime cancellationDateUtc)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (cancellationDateUtc < policy.EffectiveDateUtc)
        {
            throw new ArgumentException("Cancellation date cannot be before policy effective date.");
        }

        // Trong vòng 14 ngày (Cooling-off period): Hoàn 100%
        var daysActive = (cancellationDateUtc - policy.EffectiveDateUtc).TotalDays;
        if (daysActive <= 14)
        {
            return policy.AnnualPremium;
        }

        // Đã hết hạn hợp đồng
        if (cancellationDateUtc >= policy.ExpiryDateUtc)
        {
            return 0m;
        }

        // Sau 14 ngày: Tính theo tỷ lệ ngày chưa sử dụng trừ 20% phí quản lý
        var totalDays = (policy.ExpiryDateUtc - policy.EffectiveDateUtc).TotalDays;
        var unusedDays = (policy.ExpiryDateUtc - cancellationDateUtc).TotalDays;
        var unearnedPremium = policy.AnnualPremium * (decimal)(unusedDays / totalDays);

        // Phạt 20%
        var refund = unearnedPremium * 0.80m;
        return Math.Round(refund, 2);
    }
}
