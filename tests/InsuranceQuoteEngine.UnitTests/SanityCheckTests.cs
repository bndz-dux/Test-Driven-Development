using FluentAssertions;
using Xunit;
using InsuranceQuoteEngine.Domain;

namespace InsuranceQuoteEngine.UnitTests;

public class SanityCheckTests
{
    [Fact]
    public void Environment_ShouldBeProperlyConfigured()
    {
        // Arrange
        const int a = 10;
        const int b = 20;

        // Act
        const int sum = a + b;

        // Assert
        sum.Should().Be(30);
    }
}
