namespace VPUploader.Core;

public interface ITokenProvider
{
    Task<string> GetValidAccessTokenAsync();
}