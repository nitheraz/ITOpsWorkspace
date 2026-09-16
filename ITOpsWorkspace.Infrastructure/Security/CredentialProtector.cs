using System.Security.Cryptography;

namespace ITOpsWorkspace.Infrastructure.Security;

public static class CredentialProtector
{
    // Encrypts/decrypts data tied to the current Windows user account.
    // Data encrypted this way cannot be decrypted by another user or on another machine.
    private static readonly byte[] Entropy = { 9, 4, 7, 2, 5, 1, 8, 3 };

    public static byte[] Protect(string plainText)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
    }

    public static string Unprotect(byte[] encrypted)
    {
        if (encrypted.Length == 0) return string.Empty;
        var bytes = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}