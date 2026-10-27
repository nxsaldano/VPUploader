namespace VPUploader.Core;

public interface ITokenProvider
{
    bool HasSavedLogin { get; }
    Task SignInAsync();
    Task<string> GetValidAccessTokenAsync();
}