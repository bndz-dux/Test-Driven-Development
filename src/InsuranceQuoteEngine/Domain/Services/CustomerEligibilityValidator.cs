using System.Text.RegularExpressions;

namespace InsuranceQuoteEngine.Domain;

public record CustomerApplicant(string FullName, int Age, string PostalCode);

public class CustomerEligibilityValidator
{
    private static readonly Regex PostalCodeRegex = new(@"^\d{5}$", RegexOptions.Compiled);

    public ValidationResult Validate(CustomerApplicant applicant)
    {
        ArgumentNullException.ThrowIfNull(applicant);

        var errors = new List<string>();

        // 1. Validate full name
        if (string.IsNullOrWhiteSpace(applicant.FullName))
        {
            errors.Add("Full name is required.");
        }
        else if (applicant.FullName.Trim().Length < 2 || applicant.FullName.Trim().Length > 100)
        {
            errors.Add("Full name must be between 2 and 100 characters.");
        }

        // 2. Validate age (18 - 65)
        if (applicant.Age < 18)
        {
            errors.Add("Applicant must be at least 18 years old.");
        }
        else if (applicant.Age > 65)
        {
            errors.Add("Applicant cannot be older than 65 years old.");
        }

        // 3. Validate postal code
        if (string.IsNullOrWhiteSpace(applicant.PostalCode) || !PostalCodeRegex.IsMatch(applicant.PostalCode))
        {
            errors.Add("Postal code must be exactly 5 digits.");
        }

        return errors.Count == 0 
            ? ValidationResult.Success() 
            : ValidationResult.Failure(errors);
    }
}

public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public IReadOnlyList<string> Errors { get; }

    private ValidationResult(IEnumerable<string> errors)
    {
        Errors = errors.ToList().AsReadOnly();
    }

    public static ValidationResult Success() => new(Enumerable.Empty<string>());
    public static ValidationResult Failure(IEnumerable<string> errors) => new(errors);
}
