namespace InsuranceQuoteEngine.UnitTests.Fixtures;

/// <summary>
/// Fixture dùng chung cho các test suite cần khởi tạo tài nguyên nặng một lần duy nhất (Database in-memory, Serializer configs, v.v.).
/// </summary>
public class SharedEngineFixture : IDisposable
{
    public DateTime InitializedAtUtc { get; }
    public string EnvironmentName { get; }
    public bool IsDisposed { get; private set; }

    public SharedEngineFixture()
    {
        // Giả lập khởi tạo tài nguyên tốn kém 1 lần duy nhất cho toàn bộ test class
        InitializedAtUtc = DateTime.UtcNow;
        EnvironmentName = "TestEnvironment_Isolated";
        IsDisposed = false;
    }

    public void Dispose()
    {
        // Dọn dẹp tài nguyên khi tất cả các test trong class kết thúc
        IsDisposed = true;
        GC.SuppressFinalize(this);
    }
}
