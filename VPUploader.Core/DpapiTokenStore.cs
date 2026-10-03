using System.Security.Cryptography;
using System.Text;

namespace VPUploader.Core;

public class DpapiTokenStore : ITokenStore
{
    // asks for the local AppData folder on any machine
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VPUploader",
        "refresh_token.bin");

    public void SaveRefreshToken(string refreshToken)
    {
        // since FilePath can't return null because of how we
        // programmed it, we use the null-forgiving operator (!) 
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        
        byte[] plainBytes = Encoding.UTF8.GetBytes(refreshToken);
        byte[] encrypted = ProtectedData.Protect(
            plainBytes,
            optionalEntropy: null,
            scope: DataProtectionScope.CurrentUser);
        
        File.WriteAllBytes(FilePath, encrypted);
    }

    public string? LoadRefreshToken()
    {
        if (!File.Exists(FilePath))
            return null;
        
        byte[] encrypted = File.ReadAllBytes(FilePath);
        
        // ProtectedData is a Windows exclusive wrapper for
        // Windows Data Protection Api (DPAPI)
        byte[] plainBytes = ProtectedData.Unprotect(
            encrypted,
            optionalEntropy: null,
            scope: DataProtectionScope.CurrentUser);
        
        
        return Encoding.UTF8.GetString(plainBytes);
    }

    public void Clear()
    {
        if (File.Exists(FilePath))
            File.Delete(FilePath);
    }
    
}