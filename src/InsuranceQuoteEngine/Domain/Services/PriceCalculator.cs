namespace InsuranceQuoteEngine.Domain;

public class PriceCalculator
{
    /// <summary>
    /// Calculates the discounted price after applying a percentage discount.
    /// </summary>
    /// <param name="originalPrice">Original price (>= 0)</param>
    /// <param name="discountPercentage">Discount percentage (0.0 to 1.0)</param>
    /// <returns>Price after discount</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public decimal Calculate(decimal originalPrice, decimal discountPercentage)
    {
        if (originalPrice < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(originalPrice), 
                "Price cannot be negative.");
        }

        if (discountPercentage < 0 || discountPercentage > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discountPercentage), 
                "Discount percentage must be between 0.0 and 1.0.");
        }

        var discountAmount = originalPrice * discountPercentage;
        return originalPrice - discountAmount;
    }

    /// <summary>
    /// Calculates the final price with tax applied after discount.
    /// </summary>
    /// <param name="originalPrice">Original price (>= 0)</param>
    /// <param name="discountPercentage">Discount percentage (0.0 to 1.0)</param>
    /// <param name="taxRate">Tax rate (0.0 to 0.5)</param>
    /// <returns>Price after discount and tax</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public decimal CalculateWithTax(decimal originalPrice, decimal discountPercentage, decimal taxRate)
    {
        if (originalPrice < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(originalPrice), 
                "Price cannot be negative.");
        }

        if (discountPercentage < 0 || discountPercentage > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discountPercentage), 
                "Discount percentage must be between 0.0 and 1.0.");
        }

        if (taxRate < 0 || taxRate > 0.50m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(taxRate), 
                "Tax rate must be between 0.0 and 0.5.");
        }

        var discountAmount = originalPrice * discountPercentage;
        var finalPrice = (originalPrice - discountAmount) * (1 + taxRate);
        
        return finalPrice;
    }
}
