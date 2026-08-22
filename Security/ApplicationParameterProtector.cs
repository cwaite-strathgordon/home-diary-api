using System.Security.Cryptography;
using System.Text;

namespace HomeDiary_api.Security;

public sealed class ApplicationParameterProtector(IConfiguration configuration)
{
    private const string Prefix = "v1:";

    public string Protect(string plaintext)
    {
        var key = GetKey();
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(key, tag.Length);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var payload = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, payload, nonce.Length + tag.Length, ciphertext.Length);
        return Prefix + Convert.ToBase64String(payload);
    }

    public string Unprotect(string protectedValue)
    {
        if (!protectedValue.StartsWith(Prefix, StringComparison.Ordinal))
            throw new CryptographicException("Unsupported application parameter encryption format.");

        var payload = Convert.FromBase64String(protectedValue[Prefix.Length..]);
        if (payload.Length < 29)
            throw new CryptographicException("Encrypted application parameter is invalid.");

        var nonce = payload.AsSpan(0, 12);
        var tag = payload.AsSpan(12, 16);
        var ciphertext = payload.AsSpan(28);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(GetKey(), tag.Length);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }

    private byte[] GetKey()
    {
        var configured = configuration["ApplicationParameterEncryptionKey"];
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException(
                "ApplicationParameterEncryptionKey is missing. Configure the same 32-byte base64 key for the API and AI worker.");

        byte[] key;
        try
        {
            key = Convert.FromBase64String(configured);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                "ApplicationParameterEncryptionKey must be a base64-encoded 32-byte key.", ex);
        }

        return key.Length == 32
            ? key
            : throw new InvalidOperationException(
                "ApplicationParameterEncryptionKey must decode to exactly 32 bytes.");
    }
}
