using InsuranceQuoteEngine.Domain;

namespace InsuranceQuoteEngine.UnitTests.Builders;

public class CustomerProfileBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _fullName = "Default Test Customer";
    private string _email = "test.customer@example.com";
    private int _age = 30;
    private CustomerType _type = CustomerType.Standard;
    private string _postalCode = "70000";
    private bool _hasPastClaims = false;

    public CustomerProfileBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public CustomerProfileBuilder WithFullName(string name)
    {
        _fullName = name;
        return this;
    }

    public CustomerProfileBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public CustomerProfileBuilder WithAge(int age)
    {
        _age = age;
        return this;
    }

    public CustomerProfileBuilder WithType(CustomerType type)
    {
        _type = type;
        return this;
    }

    public CustomerProfileBuilder AsPremium() => WithType(CustomerType.Premium);
    public CustomerProfileBuilder AsVip() => WithType(CustomerType.Vip);

    public CustomerProfileBuilder WithPastClaims(bool hasClaims = true)
    {
        _hasPastClaims = hasClaims;
        return this;
    }

    public CustomerProfileBuilder WithPostalCode(string postalCode)
    {
        _postalCode = postalCode;
        return this;
    }

    public CustomerProfile Build()
    {
        return new CustomerProfile
        {
            Id = _id,
            FullName = _fullName,
            Email = _email,
            Age = _age,
            Type = _type,
            PostalCode = _postalCode,
            HasPastClaims = _hasPastClaims
        };
    }

    public static implicit operator CustomerProfile(CustomerProfileBuilder builder) => builder.Build();
}
