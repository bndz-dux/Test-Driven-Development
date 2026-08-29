namespace InsuranceQuoteEngine.Domain;

public class QuoteExpiredException : Exception
{
    public QuoteExpiredException(Guid quoteId) 
        : base($"Quote '{quoteId}' has expired and cannot be activated.") { }
}
