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

        // Within 14 days (Cooling-off period): 100% full refund
        var daysActive = (cancellationDateUtc - policy.EffectiveDateUtc).TotalDays;
        if (daysActive <= 14)
        {
            return policy.AnnualPremium;
        }

        // Already expired policy
        if (cancellationDateUtc >= policy.ExpiryDateUtc)
        {
            return 0m;
        }

        // After 14 days: Pro-rata refund on unused days minus 20% administrative cancellation fee
        var totalDays = (policy.ExpiryDateUtc - policy.EffectiveDateUtc).TotalDays;
        var unusedDays = (policy.ExpiryDateUtc - cancellationDateUtc).TotalDays;
        var unearnedPremium = policy.AnnualPremium * (decimal)(unusedDays / totalDays);

        // 20% penalty fee (retain 80%)
        var refund = unearnedPremium * 0.80m;
        return Math.Round(refund, 2);
    }
}
