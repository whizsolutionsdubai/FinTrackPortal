namespace FinTrackPortal.Interfaces;

/// <summary>AES-256-CBC for sensitive fields (e.g. IBAN). Keys from configuration.</summary>
public interface IEncryptionService
{
    bool IsConfigured { get; }

    string Encrypt(string plainText);

    string Decrypt(string cipherTextBase64);
}
