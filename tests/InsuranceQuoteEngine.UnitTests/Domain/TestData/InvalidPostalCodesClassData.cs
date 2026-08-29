using System.Collections;

namespace InsuranceQuoteEngine.UnitTests.Domain.TestData;

public class InvalidPostalCodesClassData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { null! };
        yield return new object[] { "" };
        yield return new object[] { "   " };
        yield return new object[] { "123" };
        yield return new object[] { "123456" };
        yield return new object[] { "7000A" };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
