namespace InsuranceQuoteEngine.Domain;

public enum CoverageTier
{
    Basic,
    Standard,
    Comprehensive
}

public enum QuoteStatus
{
    Approved,
    Referred,
    Declined
}

public record GenerateQuoteRequest(
    CustomerProfile Customer,
    PropertyDetails Property,
    CoverageTier Coverage);

public record QuoteResult(
    Guid QuoteId,
    QuoteStatus Status,
    decimal BasePremium,
    decimal FinalPremium,
    string? DecisionReason,
    DateTime GeneratedAtUtc,
    DateTime ExpiresAtUtc);
