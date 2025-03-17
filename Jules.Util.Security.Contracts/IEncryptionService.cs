namespace Jules.Util.Security.Contracts
{
    /// <summary>
    /// Provides methods for encrypting and decrypting data.
    /// This interface defines the contract for encryption and decryption services.
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>
        /// Encrypts a plain text string.
        /// </summary>
        /// <param name="plainText">The plain text to be encrypted.</param>
        /// <returns>The encrypted string (cipher text).</returns>
        string Encrypt(string plainText);

        /// <summary>
        /// Decrypts a cipher text string.
        /// </summary>
        /// <param name="cipherText">The encrypted text to be decrypted.</param>
        /// <returns>The decrypted string (plain text).</returns>
        string Decrypt(string cipherText);
    }
}