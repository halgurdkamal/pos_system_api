using System.Security.Cryptography;
using System.Text;

namespace pos_system_api.Infrastructure.Auth;

/// <summary>
/// Service for hashing and verifying passwords using PBKDF2
/// </summary>
public class PasswordHasher
{
    private const int SaltSize = 16; // 128 bits
    private const int KeySize = 32; // 256 bits
    private const int Iterations = 100000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string HashPassword(string password)
    {
        // Generate salt
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        // Hash password
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            Algorithm,
            KeySize);

        // Combine salt and hash
        var hashBytes = new byte[SaltSize + KeySize];
        Array.Copy(salt, 0, hashBytes, 0, SaltSize);
        Array.Copy(hash, 0, hashBytes, SaltSize, KeySize);

        // Convert to Base64 for storage
        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyPassword(string password, string hash)
    {
        // Decode the stored hash
        var hashBytes = Convert.FromBase64String(hash);

        // Extract salt
        var salt = new byte[SaltSize];
        Array.Copy(hashBytes, 0, salt, 0, SaltSize);

        // Hash the input password with the extracted salt
        var hashToCompare = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            Algorithm,
            KeySize);

        // Compare hashes
        for (int i = 0; i < KeySize; i++)
        {
            if (hashBytes[i + SaltSize] != hashToCompare[i])
                return false;
        }

        return true;
    }
}
