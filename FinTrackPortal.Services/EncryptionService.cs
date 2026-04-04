using System.Security.Cryptography;
using System.Text;
using FinTrackPortal.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FinTrackPortal.Services;

public sealed class EncryptionService : IEncryptionService
{
    private readonly byte[]? _key;
    private readonly byte[]? _iv;

    public EncryptionService(IConfiguration config)
    {
        var keyB64 = config["Encryption:Key"];
        var ivB64 = config["Encryption:IV"];
        if (!string.IsNullOrWhiteSpace(keyB64) && !string.IsNullOrWhiteSpace(ivB64))
        {
            try
            {
                _key = Convert.FromBase64String(keyB64.Trim());
                _iv = Convert.FromBase64String(ivB64.Trim());
                if (_key.Length != 32 || _iv.Length != 16)
                {
                    _key = null;
                    _iv = null;
                }
            }
            catch
            {
                _key = null;
                _iv = null;
            }
        }
    }

    public bool IsConfigured => _key != null && _iv != null;

    public string Encrypt(string plainText)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Encryption is not configured.");
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        using var aes = Aes.Create();
        aes.Key = _key!;
        aes.IV = _iv!;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var enc = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipher = enc.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(cipher);
    }

    public string Decrypt(string cipherTextBase64)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Encryption is not configured.");
        if (string.IsNullOrEmpty(cipherTextBase64))
            return string.Empty;

        using var aes = Aes.Create();
        aes.Key = _key!;
        aes.IV = _iv!;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var cipher = Convert.FromBase64String(cipherTextBase64);
        using var dec = aes.CreateDecryptor();
        var plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plain);
    }
}
