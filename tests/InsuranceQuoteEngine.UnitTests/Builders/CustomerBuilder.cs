using InsuranceQuoteEngine.Domain;

namespace InsuranceQuoteEngine.UnitTests.Builders;

public class CustomerBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _fullName = "Nguyen Van A";
    private bool _isBlocked = false;

    public CustomerBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public CustomerBuilder WithFullName(string fullName)
    {
        _fullName = fullName;
        return this;
    }

    public CustomerBuilder Blocked(bool isBlocked = true)
    {
        _isBlocked = isBlocked;
        return this;
    }

    public Customer Build() => new(_id, _fullName, _isBlocked);

    public static implicit operator Customer(CustomerBuilder builder) => builder.Build();
}
