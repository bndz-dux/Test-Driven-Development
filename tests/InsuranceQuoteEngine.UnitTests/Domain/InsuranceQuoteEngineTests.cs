using FluentAssertions;
using InsuranceQuoteEngine.Domain;
using InsuranceQuoteEngine.UnitTests.Builders;
using Moq;
using Xunit;

namespace InsuranceQuoteEngine.UnitTests.Domain;

public class InsuranceQuoteEngineTests
{
    private readonly Mock<IClock> _clockMock = new();
    private readonly InsuranceQuoteEngineService _sut;
    private readonly DateTime _fixedNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public InsuranceQuoteEngineTests()
    {
        _clockMock.Setup(c => c.UtcNow).Returns(_fixedNow);
        _sut = new InsuranceQuoteEngineService(_clockMock.Object);
    }

    #region Guard Clauses & Constructor

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenClockIsNull()
    {
        // Act
        var act = () => new InsuranceQuoteEngineService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("clock");
    }

    [Fact]
    public void GenerateQuote_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Act
        var act = () => _sut.GenerateQuote(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region BR-01 & BR-04: Decline Rules

    [Theory]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(76)]
    [InlineData(85)]
    public void GenerateQuote_ShouldDecline_WhenCustomerAgeIsOutsidePermittedRange(int invalidAge)
    {
        // Arrange
        var customer = new CustomerProfileBuilder()
            .WithAge(invalidAge)
            .Build();
        var property = new PropertyDetailsBuilder()
            .WithEstimatedValue(1_000_000_000m)
            .Build();
        var request = new GenerateQuoteRequest(customer, property, CoverageTier.Basic);

        // Act
        var result = _sut.GenerateQuote(request);

        // Assert
        result.Status.Should().Be(QuoteStatus.Declined);
        result.DecisionReason.Should().Contain("Customer age is ineligible for insurance.");
        result.BasePremium.Should().Be(0m);
        result.FinalPremium.Should().Be(0m);
        result.GeneratedAtUtc.Should().Be(_fixedNow);
        result.ExpiresAtUtc.Should().Be(_fixedNow);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50_000_000)]
    [InlineData(99_999_999)]
    public void GenerateQuote_ShouldDecline_WhenPropertyValueIsBelowMinimumThreshold(decimal invalidValue)
    {
        // Arrange
        var customer = new CustomerProfileBuilder().WithAge(30).Build();
        var property = new PropertyDetailsBuilder()
            .WithEstimatedValue(invalidValue)
            .Build();
        var request = new GenerateQuoteRequest(customer, property, CoverageTier.Basic);

        // Act
        var result = _sut.GenerateQuote(request);

        // Assert
        result.Status.Should().Be(QuoteStatus.Declined);
        result.DecisionReason.Should().Contain("Property value is below insurable limit");
        result.BasePremium.Should().Be(0m);
        result.FinalPremium.Should().Be(0m);
        result.ExpiresAtUtc.Should().Be(_fixedNow);
    }

    #endregion

    #region BR-02 & BR-03: Referral Rules

    [Fact]
    public void GenerateQuote_ShouldRefer_WhenPropertyIsInFloodZone()
    {
        // Arrange
        var customer = new CustomerProfileBuilder().WithAge(30).Build();
        var property = new PropertyDetailsBuilder()
            .WithEstimatedValue(1_000_000_000m)
            .InFloodZone(true)
            .Build();
        var request = new GenerateQuoteRequest(customer, property, CoverageTier.Basic);

        // Act
        var result = _sut.GenerateQuote(request);

        // Assert
        result.Status.Should().Be(QuoteStatus.Referred);
        result.DecisionReason.Should().Contain("Property is in a designated flood hazard zone.");
        result.BasePremium.Should().Be(0m);
        result.FinalPremium.Should().Be(0m);
        result.ExpiresAtUtc.Should().Be(_fixedNow.AddDays(30));
    }

    [Theory]
    [InlineData(1900)]
    [InlineData(1948)]
    [InlineData(1949)]
    public void GenerateQuote_ShouldRefer_WhenPropertyWasBuiltBefore1950(int oldYear)
    {
        // Arrange
        var customer = new CustomerProfileBuilder().WithAge(30).Build();
        var property = new PropertyDetailsBuilder()
            .WithEstimatedValue(1_000_000_000m)
            .WithYearBuilt(oldYear)
            .Build();
        var request = new GenerateQuoteRequest(customer, property, CoverageTier.Basic);

        // Act
        var result = _sut.GenerateQuote(request);

        // Assert
        result.Status.Should().Be(QuoteStatus.Referred);
        result.DecisionReason.Should().Contain("Property built prior to 1950 requires structural underwriting.");
        result.BasePremium.Should().Be(0m);
        result.FinalPremium.Should().Be(0m);
        result.ExpiresAtUtc.Should().Be(_fixedNow.AddDays(30));
    }

    #endregion

    #region Boundary Values (Acceptance Boundaries)

    [Theory]
    [InlineData(18)]
    [InlineData(75)]
    public void GenerateQuote_ShouldApprove_WhenAgeIsExactlyOnPermittedBoundary(int boundaryAge)
    {
        // Arrange
        var customer = new CustomerProfileBuilder().WithAge(boundaryAge).Build();
        var property = new PropertyDetailsBuilder()
            .WithEstimatedValue(1_000_000_000m)
            .WithYearBuilt(2020)
            .Build();
        var request = new GenerateQuoteRequest(customer, property, CoverageTier.Basic);

        // Act
        var result = _sut.GenerateQuote(request);

        // Assert
        result.Status.Should().Be(QuoteStatus.Approved);
    }

    [Fact]
    public void GenerateQuote_ShouldApprove_WhenPropertyValueIsExactlyAtThreshold()
    {
        // Arrange: 100,000,000 VND
        var customer = new CustomerProfileBuilder().WithAge(30).Build();
        var property = new PropertyDetailsBuilder()
            .WithEstimatedValue(100_000_000m)
            .WithYearBuilt(2020)
            .Build();
        var request = new GenerateQuoteRequest(customer, property, CoverageTier.Basic);

        // Act
        var result = _sut.GenerateQuote(request);

        // Assert
        result.Status.Should().Be(QuoteStatus.Approved);
        result.BasePremium.Should().Be(100_000m); // 100tr * 0.1% = 100k
        result.FinalPremium.Should().Be(100_000m);
    }

    [Fact]
    public void GenerateQuote_ShouldApprove_WhenPropertyYearBuiltIsExactly1950()
    {
        // Arrange: 1950 is not < 1950
        var customer = new CustomerProfileBuilder().WithAge(30).Build();
        var property = new PropertyDetailsBuilder()
            .WithEstimatedValue(1_000_000_000m)
            .WithYearBuilt(1950)
            .Build();
        var request = new GenerateQuoteRequest(customer, property, CoverageTier.Basic);

        // Act
        var result = _sut.GenerateQuote(request);

        // Assert
        result.Status.Should().Be(QuoteStatus.Approved);
    }

    #endregion

    #region BR-05, BR-06, BR-07, BR-08: Premium Calculations & Discounts

    [Fact]
    public void GenerateQuote_ShouldCalculateCorrectPremium_ForStandardCustomerWithBasicCoverage()
    {
        // Arrange: Nhà 1,000,000,000 -> Base rate 0.1% = 1,000,000. Basic tier = 1.0x. Không giảm giá.
        var customer = new CustomerProfileBuilder().WithType(CustomerType.Standard).Build();
        var property = new PropertyDetailsBuilder().WithEstimatedValue(1_000_000_000m).WithYearBuilt(2015).Build();
        var request = new GenerateQuoteRequest(customer, property, CoverageTier.Basic);

        // Act
        var result = _sut.GenerateQuote(request);

        // Assert
        result.Status.Should().Be(QuoteStatus.Approved);
        result.BasePremium.Should().Be(1_000_000m);
        result.FinalPremium.Should().Be(1_000_000m);
        result.ExpiresAtUtc.Should().Be(_fixedNow.AddDays(30));
        result.GeneratedAtUtc.Should().Be(_fixedNow);
        result.QuoteId.Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateQuote_ShouldApplyStandardCoverageMultiplier_Of125Percent()
    {
        // Arrange: Nhà 1 tỷ -> Base 1,000,000. Standard tier = 1.25x => 1,250,000.
        var customer = new CustomerProfileBuilder().WithType(CustomerType.Standard).Build();
        var property = new PropertyDetailsBuilder().WithEstimatedValue(1_000_000_000m).WithYearBuilt(2015).Build();
        var request = new GenerateQuoteRequest(customer, property, CoverageTier.Standard);

        // Act
        var result = _sut.GenerateQuote(request);

        // Assert
        result.Status.Should().Be(QuoteStatus.Approved);
        result.BasePremium.Should().Be(1_000_000m);
        result.FinalPremium.Should().Be(1_250_000m);
    }

    [Fact]
    public void GenerateQuote_ShouldApplyComprehensiveCoverageMultiplier_Of160Percent()
    {
        // Arrange: Nhà 1 tỷ -> Base 1,000,000. Comprehensive tier = 1.60x => 1,600,000.
        var customer = new CustomerProfileBuilder().WithType(CustomerType.Standard).Build();
        var property = new PropertyDetailsBuilder().WithEstimatedValue(1_000_000_000m).WithYearBuilt(2015).Build();
        var request = new GenerateQuoteRequest(customer, property, CoverageTier.Comprehensive);

        // Act
        var result = _sut.GenerateQuote(request);

        // Assert
        result.Status.Should().Be(QuoteStatus.Approved);
        result.BasePremium.Should().Be(1_000_000m);
        result.FinalPremium.Should().Be(1_600_000m);
    }

    [Fact]
    public void GenerateQuote_ShouldApplyPremiumCustomerDiscount_Of10Percent()
    {
        // Arrange: Nhà 1 tỷ, gói Basic (1,000,000). Khách Premium giảm 10% => còn 900,000.
        var customer = new CustomerProfileBuilder().AsPremium().Build();
        var property = new PropertyDetailsBuilder().WithEstimatedValue(1_000_000_000m).WithYearBuilt(2015).Build();
        var request = new GenerateQuoteRequest(customer, property, CoverageTier.Basic);

        // Act
        var result = _sut.GenerateQuote(request);

        // Assert
        result.Status.Should().Be(QuoteStatus.Approved);
        result.BasePremium.Should().Be(1_000_000m);
        result.FinalPremium.Should().Be(900_000m);
    }

    [Fact]
    public void GenerateQuote_ShouldApplyVipDiscount_Of20Percent()
    {
        // Arrange: Nhà 1 tỷ, gói Comprehensive (1,600,000). Khách VIP được giảm 20% => còn 1,280,000.
        var customer = new CustomerProfileBuilder().AsVip().Build();
        var property = new PropertyDetailsBuilder().WithEstimatedValue(1_000_000_000m).WithYearBuilt(2015).Build();
        var request = new GenerateQuoteRequest(customer, property, CoverageTier.Comprehensive);

        // Act
        var result = _sut.GenerateQuote(request);

        // Assert
        result.Status.Should().Be(QuoteStatus.Approved);
        result.BasePremium.Should().Be(1_000_000m);
        result.FinalPremium.Should().Be(1_280_000m);
    }

    [Fact]
    public void GenerateQuote_ShouldNotApplyDiscount_WhenCustomerHasPastClaims()
    {
        // Arrange: Khách VIP (đáng lẽ giảm 20%) nhưng có tiền sử bồi thường (HasPastClaims = true) => Mất quyền giảm giá
        var customer = new CustomerProfileBuilder()
            .AsVip()
            .WithPastClaims(true)
            .Build();
        var property = new PropertyDetailsBuilder().WithEstimatedValue(1_000_000_000m).WithYearBuilt(2015).Build();
        var request = new GenerateQuoteRequest(customer, property, CoverageTier.Basic);

        // Act
        var result = _sut.GenerateQuote(request);

        // Assert
        result.Status.Should().Be(QuoteStatus.Approved);
        result.FinalPremium.Should().Be(1_000_000m); // Không được giảm, giữ nguyên 1,000,000
    }

    #endregion
}
