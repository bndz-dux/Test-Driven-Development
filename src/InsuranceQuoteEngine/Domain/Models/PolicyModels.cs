namespace InsuranceQuoteEngine.Domain;

public record Policy(
    Guid Id, 
    Guid QuoteId, 
    DateTime EffectiveDateUtc, 
    DateTime ExpiryDateUtc, 
    decimal AnnualPremium, 
    bool IsActive);
