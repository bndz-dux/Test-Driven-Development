using FluentAssertions;
using InsuranceQuoteEngine.Domain;
using InsuranceQuoteEngine.UnitTests.Builders;
using Moq;
using Xunit;

namespace InsuranceQuoteEngine.UnitTests.Domain;

public class PolicyLifecycleManagerTests
{
    private readonly Mock<IClock> _clockMock = new();
    private readonly PolicyLifecycleManager _sut;

    public PolicyLifecycleManagerTests()
    {
        _sut = new PolicyLifecycleManager(_clockMock.Object);
    }

    [Fact]
    public void ActivateQuote_ShouldCreateValidPolicy_WhenQuoteIsNotExpired()
    {
        // Arrange: Freeze time at 2026-01-01 10:00:00 UTC
        var fixedNow = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        _clockMock.Setup(c => c.UtcNow).Returns(fixedNow);

        var quote = new InsuranceQuoteBuilder()
            .WithFinalPremium(1200m)
            .Build(); // Default expiration is +30 days (2026-01-31)

        // Act
        var policy = _sut.ActivateQuote(quote);

        // Assert
        policy.Should().NotBeNull();
        policy.EffectiveDateUtc.Should().Be(fixedNow);
        policy.ExpiryDateUtc.Should().Be(fixedNow.AddYears(1)); // 2027-01-01
        policy.AnnualPremium.Should().Be(1200m);
        policy.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ActivateQuote_ShouldThrowQuoteExpiredException_WhenCurrentTimeIsPastExpirationDate()
    {
        // Arrange: Current time (2026-03-01) is after quote expiration date (2026-02-20)
        var fixedNow = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        _clockMock.Setup(c => c.UtcNow).Returns(fixedNow);

        var quote = new InsuranceQuoteBuilder()
            .WithExpiresAtUtc(fixedNow.AddDays(-5)) // Expired before fixedNow
            .Build();

        // Act
        Action act = () => _sut.ActivateQuote(quote);

        // Assert
        act.Should().Throw<QuoteExpiredException>()
            .WithMessage($"*{quote.Id}*");
    }

    [Fact]
    public void CalculateRefundOnCancellation_ShouldReturnFullAmount_WhenCancelledWithin14DaysCoolingOffPeriod()
    {
        // Arrange
        var effectiveDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var policy = new Policy(
            Id: Guid.NewGuid(),
            QuoteId: Guid.NewGuid(),
            EffectiveDateUtc: effectiveDate,
            ExpiryDateUtc: effectiveDate.AddYears(1),
            AnnualPremium: 1000m,
            IsActive: true
        );

        var cancellationDate = effectiveDate.AddDays(10); // Day 10 (<= 14 days)

        // Act
        var refund = _sut.CalculateRefundOnCancellation(policy, cancellationDate);

        // Assert
        refund.Should().Be(1000m); // 100% full refund
    }

    [Fact]
    public void CalculateRefundOnCancellation_ShouldApply20PercentPenalty_WhenCancelledAfter14Days()
    {
        // Arrange: 365-day policy priced at 1000, cancelled on day 100
        var effectiveDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var expiryDate = effectiveDate.AddDays(365);
        var policy = new Policy(
            Id: Guid.NewGuid(),
            QuoteId: Guid.NewGuid(),
            EffectiveDateUtc: effectiveDate,
            ExpiryDateUtc: expiryDate,
            AnnualPremium: 1000m,
            IsActive: true
        );

        var cancellationDate = effectiveDate.AddDays(100); 
        // 265 unused days left => Unearned = 1000 * (265 / 365) = 726.027
        // 20% penalty fee => 80% refund of 726.027 = 580.82

        // Act
        var refund = _sut.CalculateRefundOnCancellation(policy, cancellationDate);

        // Assert
        refund.Should().Be(580.82m);
    }

    [Fact]
    public void CalculateRefundOnCancellation_ShouldThrowArgumentException_WhenCancellationDateIsBeforeEffectiveDate()
    {
        // Arrange
        var effectiveDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var policy = new Policy(
            Id: Guid.NewGuid(),
            QuoteId: Guid.NewGuid(),
            EffectiveDateUtc: effectiveDate,
            ExpiryDateUtc: effectiveDate.AddYears(1),
            AnnualPremium: 1000m,
            IsActive: true
        );

        var invalidCancellationDate = effectiveDate.AddDays(-1);

        // Act
        Action act = () => _sut.CalculateRefundOnCancellation(policy, invalidCancellationDate);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be before*");
    }

    [Fact]
    public void CalculateRefundOnCancellation_ShouldReturnZero_WhenCancellationDateIsAfterExpiryDate()
    {
        // Arrange
        var effectiveDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var policy = new Policy(
            Id: Guid.NewGuid(),
            QuoteId: Guid.NewGuid(),
            EffectiveDateUtc: effectiveDate,
            ExpiryDateUtc: effectiveDate.AddYears(1),
            AnnualPremium: 1000m,
            IsActive: true
        );

        var expiredCancellationDate = effectiveDate.AddYears(1).AddDays(1);

        // Act
        var refund = _sut.CalculateRefundOnCancellation(policy, expiredCancellationDate);

        // Assert
        refund.Should().Be(0m);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenClockIsNull()
    {
        // Act
        Action act = () => new PolicyLifecycleManager(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ActivateQuote_ShouldThrowArgumentNullException_WhenQuoteIsNull()
    {
        // Act
        Action act = () => _sut.ActivateQuote(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CalculateRefundOnCancellation_ShouldThrowArgumentNullException_WhenPolicyIsNull()
    {
        // Act
        Action act = () => _sut.CalculateRefundOnCancellation(null!, DateTime.UtcNow);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SystemClock_UtcNow_ShouldReturnCloseToCurrentUtcTime()
    {
        // Arrange
        var clock = new SystemClock();

        // Act
        var now = clock.UtcNow;

        // Assert
        now.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }
}
