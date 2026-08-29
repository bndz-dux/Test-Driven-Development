namespace InsuranceQuoteEngine.Domain;

public enum CustomerType
{
    Standard,
    Premium,
    Vip
}

public record CustomerProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string FullName { get; init; } = "John Doe";
    public string Email { get; init; } = "john.doe@test.com";
    public int Age { get; init; } = 30;
    public CustomerType Type { get; init; } = CustomerType.Standard;
    public string PostalCode { get; init; } = "70000";
    public bool HasPastClaims { get; init; } = false;
}

public record PropertyDetails
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Address { get; init; } = "123 Main Street";
    public int YearBuilt { get; init; } = 2018;
    public decimal EstimatedValue { get; init; } = 500_000m;
    public bool IsInFloodZone { get; init; } = false;
}

public record InsuranceQuote
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public CustomerProfile Customer { get; init; } = default!;
    public PropertyDetails Property { get; init; } = default!;
    public decimal BasePremium { get; init; } = 1000m;
    public decimal FinalPremium { get; init; } = 1000m;
    public bool IsReferredToUnderwriter { get; init; } = false;
    public bool IsDeclined { get; init; } = false;
    public DateTime ExpiresAtUtc { get; init; } = DateTime.UtcNow.AddDays(30);
}
