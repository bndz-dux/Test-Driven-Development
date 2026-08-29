namespace InsuranceQuoteEngine.UnitTests.Fixtures;

/// <summary>
/// Shared fixture for test suites requiring expensive one-time resource initialization (in-memory database, serializer configs, etc.).
/// </summary>
public class SharedEngineFixture : IDisposable
{
    public DateTime InitializedAtUtc { get; }
    public string EnvironmentName { get; }
    public bool IsDisposed { get; private set; }

    public SharedEngineFixture()
    {
        // Simulate expensive one-time setup for the entire test class
        InitializedAtUtc = DateTime.UtcNow;
        EnvironmentName = "TestEnvironment_Isolated";
        IsDisposed = false;
    }

    public void Dispose()
    {
        // Clean up resources after all tests in the class have finished
        IsDisposed = true;
        GC.SuppressFinalize(this);
    }
}
