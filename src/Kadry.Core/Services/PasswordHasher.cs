using System.Security.Cryptography;

namespace Kadry.Core.Services;

/// <summary>Хеширование паролей: PBKDF2 (SHA-256, 100 000 итераций, соль 16 байт).</summary>
public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public static byte[] GenerateSalt() => RandomNumberGenerator.GetBytes(SaltSize);

    public static byte[] Hash(string password, byte[] salt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentNullException.ThrowIfNull(salt);

        return Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
    }

    public static bool Verify(string password, byte[] salt, byte[] expectedHash)
    {
        if (string.IsNullOrEmpty(password) || salt.Length == 0 || expectedHash.Length == 0)
            return false;

        var actual = Hash(password, salt);
        return CryptographicOperations.FixedTimeEquals(actual, expectedHash);
    }
}
