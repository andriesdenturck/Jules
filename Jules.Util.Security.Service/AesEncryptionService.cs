using Jules.Util.Security.Contracts;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Jules.Util.Security;

/// <summary>
/// Provides AES encryption and decryption services using a secret key and IV (Initialization Vector).
/// This service uses AES (Advanced Encryption Standard) algorithm to securely encrypt and decrypt data.
/// </summary>
public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    /// <summary>
    /// Initializes a new instance of the <see cref="AesEncryptionService"/> class.
    /// Retrieves the key and IV from configuration (stored as Base64 strings).
    /// </summary>
    /// <param name="config">The <see cref="IConfiguration"/> object containing the encryption settings.</param>
    public AesEncryptionService(IConfiguration config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config), "Configuration cannot be null.");
        }

        var keyString = config["Encryption:Key"]; // Base64 string
        var ivString = config["Encryption:IV"];   // Base64 string

        if (string.IsNullOrEmpty(keyString))
        {
            throw new ArgumentNullException(nameof(config), "Encryption:Key setting is missing or empty.");
        }

        if (string.IsNullOrEmpty(ivString))
        {
            throw new ArgumentNullException(nameof(config), "Encryption:IV setting is missing or empty.");
        }

        // Check if the key is a valid Base64 string
        if (!IsBase64String(keyString))
        {
            throw new ArgumentException("The Encryption:Key setting is not a valid Base64 string.", nameof(config));
        }

        // Check if the IV is a valid Base64 string
        if (!IsBase64String(ivString))
        {
            throw new ArgumentException("The Encryption:IV setting is not a valid Base64 string.", nameof(config));
        }

        _key = Convert.FromBase64String(keyString);
        _iv = Convert.FromBase64String(ivString);

        // Ensure key length is valid for AES (128, 192, or 256 bits)
        if (_key.Length != 16 && _key.Length != 24 && _key.Length != 32)
        {
            throw new ArgumentException("The key length must be 16, 24, or 32 bytes.", nameof(config));
        }

        // Ensure IV length is correct (AES requires 16 bytes for the IV)
        if (_iv.Length != 16)
        {
            throw new ArgumentException("The IV length must be 16 bytes.", nameof(config));
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Encrypts the provided plain text using AES encryption.
    /// </summary>
    /// <param name="plainText">The plain text to encrypt.</param>
    /// <returns>The encrypted text as a Base64-encoded string.</returns>
    public string Encrypt(string plainText)
    {
        if (plainText == null)
        {
            throw new ArgumentNullException(nameof(plainText), "Plain text cannot be null.");
        }

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using var sw = new StreamWriter(cs);

        sw.Write(plainText);
        sw.Close();

        return Convert.ToBase64String(ms.ToArray());
    }

    /// <inheritdoc />
    /// <summary>
    /// Decrypts the provided cipher text using AES decryption.
    /// </summary>
    /// <param name="cipherText">The encrypted text to decrypt (Base64-encoded).</param>
    /// <returns>The decrypted plain text.</returns>
    public string Decrypt(string cipherText)
    {
        if (cipherText == null)
        {
            throw new ArgumentNullException(nameof(cipherText), "Plain text cannot be null.");
        }

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        var buffer = Convert.FromBase64String(cipherText);
        using var ms = new MemoryStream(buffer);
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }

    // Helper function to validate Base64 strings
    private bool IsBase64String(string base64String)
    {
        Span<byte> buffer = new Span<byte>(new byte[base64String.Length]);
        return Convert.TryFromBase64String(base64String, buffer, out _);
    }
}