namespace InsuranceQuoteEngine.Domain;

public record Customer(Guid Id, string FullName, bool IsBlocked);

public record Order(Guid Id, Guid CustomerId, decimal Amount, DateTime CreatedAtUtc);

public record CreateOrderRequest(Guid CustomerId, decimal Amount);

public record PaymentResult(bool Success, string TransactionId, string? ErrorMessage);
