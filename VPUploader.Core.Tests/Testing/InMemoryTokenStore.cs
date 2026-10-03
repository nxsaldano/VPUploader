namespace VPUploader.Core.Tests.Testing;

public class InMemoryTokenStore : ITokenStore
{
    public string? SavedRefreshToken { get; private set; }
    public int ClearCallCount { get; private set; }

    public InMemoryTokenStore(string? seedRefreshToken = null)
    {
        SavedRefreshToken = seedRefreshToken;
    }

    public void SaveRefreshToken(string refreshToken) => SavedRefreshToken = refreshToken;

    public string? LoadRefreshToken() => SavedRefreshToken;

    public void Clear()
    {
        SavedRefreshToken = null;
        ClearCallCount++;
    }
}