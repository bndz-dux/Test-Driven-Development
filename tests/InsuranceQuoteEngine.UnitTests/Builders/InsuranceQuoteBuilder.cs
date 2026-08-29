using InsuranceQuoteEngine.Domain;

namespace InsuranceQuoteEngine.UnitTests.Builders;

public class InsuranceQuoteBuilder
{
    private Guid _id = Guid.NewGuid();
    private CustomerProfile _customer = new CustomerProfileBuilder().Build();
    private PropertyDetails _property = new PropertyDetailsBuilder().Build();
    private decimal _basePremium = 1000m;
    private decimal _finalPremium = 1000m;
    private bool _isReferred = false;
    private bool _isDeclined = false;
    private DateTime _expiresAtUtc = DateTime.UtcNow.AddDays(30);

    public InsuranceQuoteBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public InsuranceQuoteBuilder WithCustomer(CustomerProfile customer)
    {
        _customer = customer;
        return this;
    }

    public InsuranceQuoteBuilder WithCustomer(Action<CustomerProfileBuilder> configure)
    {
        var builder = new CustomerProfileBuilder();
        configure(builder);
        _customer = builder.Build();
        return this;
    }

    public InsuranceQuoteBuilder WithProperty(PropertyDetails property)
    {
        _property = property;
        return this;
    }

    public InsuranceQuoteBuilder WithProperty(Action<PropertyDetailsBuilder> configure)
    {
        var builder = new PropertyDetailsBuilder();
        configure(builder);
        _property = builder.Build();
        return this;
    }

    public InsuranceQuoteBuilder WithBasePremium(decimal premium)
    {
        _basePremium = premium;
        return this;
    }

    public InsuranceQuoteBuilder WithFinalPremium(decimal premium)
    {
        _finalPremium = premium;
        return this;
    }

    public InsuranceQuoteBuilder AsReferred(bool isReferred = true)
    {
        _isReferred = isReferred;
        return this;
    }

    public InsuranceQuoteBuilder AsDeclined(bool isDeclined = true)
    {
        _isDeclined = isDeclined;
        return this;
    }

    public InsuranceQuoteBuilder Expired()
    {
        _expiresAtUtc = DateTime.UtcNow.AddDays(-1);
        return this;
    }

    public InsuranceQuoteBuilder WithExpiresAtUtc(DateTime expiresAtUtc)
    {
        _expiresAtUtc = expiresAtUtc;
        return this;
    }

    public InsuranceQuote Build() => new()
    {
        Id = _id,
        Customer = _customer,
        Property = _property,
        BasePremium = _basePremium,
        FinalPremium = _finalPremium,
        IsReferredToUnderwriter = _isReferred,
        IsDeclined = _isDeclined,
        ExpiresAtUtc = _expiresAtUtc
    };

    public static implicit operator InsuranceQuote(InsuranceQuoteBuilder builder) => builder.Build();
}
