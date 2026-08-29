using FluentAssertions;
using InsuranceQuoteEngine.Domain;
using Xunit;

namespace InsuranceQuoteEngine.UnitTests.Domain;

public class CustomerEligibilityValidatorTests
{
    private readonly CustomerEligibilityValidator _sut = new();

    [Fact]
    public void Validate_ShouldReturnSuccess_WhenAllInputsAreValid()
    {
        // Arrange
        var applicant = new CustomerApplicant("Nguyen Van A", 30, "70000");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldThrowArgumentNullException_WhenApplicantIsNull()
    {
        // Act
        Action act = () => _sut.Validate(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #region Age Boundary Tests (BVA)

    [Fact]
    public void Validate_ShouldFail_WhenAgeIs17_JustBelowLowerBoundary()
    {
        // Arrange
        var applicant = new CustomerApplicant("Nguyen Van A", 17, "70000");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Applicant must be at least 18 years old.");
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenAgeIs18_ExactLowerBoundary()
    {
        // Arrange
        var applicant = new CustomerApplicant("Nguyen Van A", 18, "70000");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenAgeIs65_ExactUpperBoundary()
    {
        // Arrange
        var applicant = new CustomerApplicant("Nguyen Van A", 65, "70000");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenAgeIs66_JustAboveUpperBoundary()
    {
        // Arrange
        var applicant = new CustomerApplicant("Nguyen Van A", 66, "70000");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Applicant cannot be older than 65 years old.");
    }

    #endregion

    #region Name Boundary & Edge Cases

    [Fact]
    public void Validate_ShouldFail_WhenFullNameIsNull()
    {
        // Arrange
        var applicant = new CustomerApplicant(null!, 25, "70000");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Full name is required.");
    }

    [Fact]
    public void Validate_ShouldFail_WhenFullNameIsWhitespace()
    {
        // Arrange
        var applicant = new CustomerApplicant("   ", 25, "70000");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Full name is required.");
    }

    [Fact]
    public void Validate_ShouldFail_WhenFullNameIs1Character_BelowMinimumLength()
    {
        // Arrange
        var applicant = new CustomerApplicant("A", 25, "70000");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Full name must be between 2 and 100 characters.");
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenFullNameIs2Characters_ExactMinimumLength()
    {
        // Arrange
        var applicant = new CustomerApplicant("An", 25, "70000");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenFullNameIs100Characters_ExactMaximumLength()
    {
        // Arrange
        var maxName = new string('A', 100);
        var applicant = new CustomerApplicant(maxName, 25, "70000");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenFullNameIs101Characters_AboveMaximumLength()
    {
        // Arrange
        var tooLongName = new string('A', 101);
        var applicant = new CustomerApplicant(tooLongName, 25, "70000");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Full name must be between 2 and 100 characters.");
    }

    #endregion

    #region PostalCode Validation

    [Fact]
    public void Validate_ShouldFail_WhenPostalCodeHas4Digits()
    {
        // Arrange
        var applicant = new CustomerApplicant("Nguyen Van A", 25, "1234");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Postal code must be exactly 5 digits.");
    }

    [Fact]
    public void Validate_ShouldFail_WhenPostalCodeContainsLetters()
    {
        // Arrange
        var applicant = new CustomerApplicant("Nguyen Van A", 25, "7000A");

        // Act
        var result = _sut.Validate(applicant);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Postal code must be exactly 5 digits.");
    }

    #endregion
}