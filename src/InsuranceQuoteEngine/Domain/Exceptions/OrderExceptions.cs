namespace InsuranceQuoteEngine.Domain;

public class CustomerNotFoundException : Exception
{
    public CustomerNotFoundException(Guid customerId) 
        : base($"Customer with ID '{customerId}' was not found.") { }
}

public class CustomerBlockedException : Exception
{
    public CustomerBlockedException(Guid customerId) 
        : base($"Customer with ID '{customerId}' is blocked from making purchases.") { }
}

public class PaymentFailedException : Exception
{
    public PaymentFailedException(string reason) 
        : base($"Payment processing failed: {reason}") { }
}
