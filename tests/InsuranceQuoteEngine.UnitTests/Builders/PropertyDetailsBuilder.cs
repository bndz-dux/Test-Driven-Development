using InsuranceQuoteEngine.Domain;

namespace InsuranceQuoteEngine.UnitTests.Builders;

public class PropertyDetailsBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _address = "123 Nguyen Hue, Ben Nghe, Q1";
    private int _yearBuilt = 2020;
    private decimal _estimatedValue = 1_000_000m;
    private bool _isInFloodZone = false;

    public PropertyDetailsBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public PropertyDetailsBuilder WithAddress(string address)
    {
        _address = address;
        return this;
    }

    public PropertyDetailsBuilder WithYearBuilt(int year)
    {
        _yearBuilt = year;
        return this;
    }

    public PropertyDetailsBuilder WithEstimatedValue(decimal value)
    {
        _estimatedValue = value;
        return this;
    }

    public PropertyDetailsBuilder InFloodZone(bool inFloodZone = true)
    {
        _isInFloodZone = inFloodZone;
        return this;
    }

    public PropertyDetails Build() => new()
    {
        Id = _id,
        Address = _address,
        YearBuilt = _yearBuilt,
        EstimatedValue = _estimatedValue,
        IsInFloodZone = _isInFloodZone
    };

    public static implicit operator PropertyDetails(PropertyDetailsBuilder builder) => builder.Build();
}
