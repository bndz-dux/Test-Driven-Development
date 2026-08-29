using InsuranceQuoteEngine.Domain;

namespace InsuranceQuoteEngine.UnitTests.Builders;

public class OrderBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _customerId = Guid.NewGuid();
    private decimal _amount = 100m;
    private DateTime _createdAtUtc = DateTime.UtcNow;

    public OrderBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public OrderBuilder WithCustomerId(Guid customerId)
    {
        _customerId = customerId;
        return this;
    }

    public OrderBuilder WithAmount(decimal amount)
    {
        _amount = amount;
        return this;
    }

    public OrderBuilder WithCreatedAtUtc(DateTime createdAtUtc)
    {
        _createdAtUtc = createdAtUtc;
        return this;
    }

    public Order Build() => new(_id, _customerId, _amount, _createdAtUtc);

    public static implicit operator Order(OrderBuilder builder) => builder.Build();
}
