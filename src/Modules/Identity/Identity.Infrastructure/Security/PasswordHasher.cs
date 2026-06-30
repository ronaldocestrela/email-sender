using System;
using System.Security.Cryptography;
using Identity.Application.Ports;

namespace Identity.Infrastructure.Security;

/// <summary>
/// Provedor seguro de hashing e verificação de senhas usando o algoritmo PBKDF2 (SHA256).
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;      // 128 bits
    private const int KeySize = 32;       // 256 bits
    private const int Iterations = 100000; // Número robusto de iterações

    /// <summary>
    /// Gera o hash da senha utilizando PBKDF2 com Salt aleatório.
    /// </summary>
    public string HashPassword(string password)
    {
        if (password == null) throw new ArgumentNullException(nameof(password));

        using var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            SaltSize,
            Iterations,
            HashAlgorithmName.SHA256);

        var salt = pbkdf2.Salt;
        var key = pbkdf2.GetBytes(KeySize);

        var hashBytes = new byte[SaltSize + KeySize];
        Array.Copy(salt, 0, hashBytes, 0, SaltSize);
        Array.Copy(key, 0, hashBytes, SaltSize, KeySize);

        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Verifica se a senha em texto claro corresponde ao hash armazenado usando comparação em tempo constante.
    /// </summary>
    public bool VerifyPassword(string hashedPassword, string password)
    {
        if (hashedPassword == null) return false;
        if (password == null) return false;

        try
        {
            var hashBytes = Convert.FromBase64String(hashedPassword);
            if (hashBytes.Length != SaltSize + KeySize) return false;

            var salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);

            var key = new byte[KeySize];
            Array.Copy(hashBytes, SaltSize, key, 0, KeySize);

            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256);

            var keyToCheck = pbkdf2.GetBytes(KeySize);

            return CryptographicOperations.FixedTimeEquals(key, keyToCheck);
        }
        catch
        {
            return false;
        }
    }
}
