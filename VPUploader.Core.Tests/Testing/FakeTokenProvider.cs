namespace VPUploader.Core.Tests.Testing;

public class FakeTokenProvider : ITokenProvider
{
    private readonly string _token;

    public FakeTokenProvider(string token = "fake-access-token")
    {
        _token = token;
    }
    
    public bool HasSavedLogin => true;
    public Task SignInAsync() => Task.CompletedTask;
    public Task<string> GetValidAccessTokenAsync() => Task.FromResult(_token);
    
}