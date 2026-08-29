namespace InsuranceQuoteEngine.Domain;

public class PriceCalculator
{
    /// <summary>
    /// Tính toán giá tiền sau khi áp dụng phần trăm giảm giá.
    /// </summary>
    /// <param name="originalPrice">Giá ban đầu (>= 0)</param>
    /// <param name="discountPercentage">Phần trăm giảm giá (0.0 đến 1.0)</param>
    /// <returns>Giá sau giảm</returns>
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
    /// Tính thuế.
    /// </summary>
    /// <param name="originalPrice">Giá ban đầu (>= 0)</param>
    /// <param name="discountPercentage">Phần trăm giảm giá (0.0 đến 1.0)</param>
    /// <param name="taxRate">Thuế suất (0.0 đến 0.5)</param>
    /// <returns>Giá sau giảm</returns>
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
