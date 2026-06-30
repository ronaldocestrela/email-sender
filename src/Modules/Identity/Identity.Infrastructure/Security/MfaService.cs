using System;
using System.Security.Cryptography;
using System.Text;
using Identity.Application.Ports;

namespace Identity.Infrastructure.Security;

/// <summary>
/// Provedor concreto de Multi-Factor Authentication (TOTP) de acordo com a RFC 6238.
/// </summary>
public class MfaService : IMfaService
{
    private static readonly char[] Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();

    /// <summary>
    /// Gera um segredo aleatório Base32 de 80 bits.
    /// </summary>
    public string GenerateMfaSecret()
    {
        var bytes = new byte[10];
        RandomNumberGenerator.Fill(bytes);
        return ToBase32String(bytes);
    }

    /// <summary>
    /// Gera a URI padronizada para registro no Google Authenticator, Microsoft Authenticator, etc.
    /// </summary>
    public string GetQrCodeUri(string email, string secret, string issuer = "EmailSender")
    {
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentNullException(nameof(email));
        if (string.IsNullOrWhiteSpace(secret)) throw new ArgumentNullException(nameof(secret));

        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";
    }

    /// <summary>
    /// Verifica a validade do código TOTP com suporte a compensação de drift temporal (+/- 1 ciclo).
    /// </summary>
    public bool VerifyCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(secret)) return false;
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6 || !int.TryParse(code, out _)) return false;

        try
        {
            var key = FromBase32String(secret);
            var timestamp = DateTime.UtcNow;

            // Tolerância de drift temporal (+/- 30 segundos)
            for (int i = -1; i <= 1; i++)
            {
                var counter = GetTimeStep(timestamp, i);
                var expectedCode = GenerateTotpCode(key, counter);
                if (expectedCode == code)
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static long GetTimeStep(DateTime timestamp, int offsetStep)
    {
        var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var timeDifference = timestamp.ToUniversalTime() - unixEpoch;
        var seconds = (long)timeDifference.TotalSeconds;
        return (seconds / 30) + offsetStep;
    }

    private static string GenerateTotpCode(byte[] key, long counter)
    {
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counterBytes);
        }

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes);

        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24)
                     | ((hash[offset + 1] & 0xFF) << 16)
                     | ((hash[offset + 2] & 0xFF) << 8)
                     | (hash[offset + 3] & 0xFF);

        var code = binary % 1000000;
        return code.ToString("D6");
    }

    private static string ToBase32String(byte[] data)
    {
        var result = new StringBuilder((data.Length + 4) / 5 * 8);
        long bin = 0;
        int size = 0;

        foreach (var b in data)
        {
            bin = (bin << 8) | b;
            size += 8;

            while (size >= 5)
            {
                size -= 5;
                result.Append(Base32Alphabet[(int)((bin >> size) & 0x1F)]);
            }
        }

        if (size > 0)
        {
            result.Append(Base32Alphabet[(int)((bin << (5 - size)) & 0x1F)]);
        }

        return result.ToString();
    }

    private static byte[] FromBase32String(string base32)
    {
        base32 = base32.Trim().ToUpperInvariant();
        var bytes = new byte[base32.Length * 5 / 8];
        int index = 0;
        int byteVal = 0;
        int bitsRemaining = 8;

        foreach (var c in base32)
        {
            var charValue = Array.IndexOf(Base32Alphabet, c);
            if (charValue < 0)
                throw new ArgumentException("Caracteres Base32 inválidos.");

            if (bitsRemaining > 5)
            {
                byteVal = (byteVal << 5) | charValue;
                bitsRemaining -= 5;
            }
            else
            {
                byteVal = (byteVal << bitsRemaining) | (charValue >> (5 - bitsRemaining));
                bytes[index++] = (byte)byteVal;
                byteVal = charValue & ((1 << (5 - bitsRemaining)) - 1);
                bitsRemaining = 8 - (5 - bitsRemaining);
            }
        }

        return bytes;
    }
}
