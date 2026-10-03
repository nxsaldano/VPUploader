namespace VPUploader.Core.Tests.Testing;

public class FakeTokenProvider : ITokenProvider
{
    private readonly string _token;

    public FakeTokenProvider(string token = "fake-access-token")
    {
        _token = token;
    }

    public Task<string> GetValidAccessTokenAsync() => Task.FromResult(_token);
    
}