using FluentAssertions;
using Xunit;

namespace InsuranceQuoteEngine.UnitTests.Fixtures;

public class EngineFixtureTests : IClassFixture<SharedEngineFixture>
{
    private readonly SharedEngineFixture _fixture;

    public EngineFixtureTests(SharedEngineFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Fixture_ShouldBeInitializedProperly()
    {
        _fixture.Should().NotBeNull();
        _fixture.EnvironmentName.Should().Be("TestEnvironment_Isolated");
        _fixture.IsDisposed.Should().BeFalse();
    }

    [Fact]
    public void Fixture_Timestamp_ShouldBeValidUtcDate()
    {
        _fixture.InitializedAtUtc.Should().BeBefore(DateTime.UtcNow.AddMinutes(1));
    }
}
