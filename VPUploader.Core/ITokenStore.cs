namespace VPUploader.Core;

public interface ITokenStore
{
    void SaveRefreshToken(string refreshToken);
    string? LoadRefreshToken();
    void Clear();
    
}