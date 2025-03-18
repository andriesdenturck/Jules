using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;

namespace Jules.Util.Security.Tests
{
    [TestFixture]
    public class AesEncryptionServiceTests
    {
        private AesEncryptionService _aesEncryptionService;
        private Mock<IConfiguration> _configMock;

        // Correct key and IV that are valid for AES
        private const string SecretKeyBase64 = "MTIzNDU2Nzg5MDEyMzQ1Ng=="; // Base64 encoded string of "1234567890123456" (16 bytes)

        private const string IvBase64 = "YWJjZGVmZ2hpamtsbW5vcA=="; // Base64 encoded string of "abcdefghijklmnop" (16 bytes)

        [SetUp]
        public void Setup()
        {
            // Set up mock configuration to return test values
            _configMock = new Mock<IConfiguration>();
            _configMock.Setup(config => config["Encryption:Key"]).Returns(SecretKeyBase64);
            _configMock.Setup(config => config["Encryption:IV"]).Returns(IvBase64);

            _aesEncryptionService = new AesEncryptionService(_configMock.Object);
        }

        #region Constructor Tests

        [Test]
        public void Constructor_WithNullConfig_ShouldThrowArgumentNullException()
        {
            // Arrange
            IConfiguration config = null;

            // Act & Assert
            Assert.That(() => new AesEncryptionService(config),
                Throws.ArgumentNullException.With.Message.Contains("Configuration cannot be null"));
        }

        [Test]
        public void Constructor_ShouldThrowArgumentException_WhenKeyIsInvalidBase64()
        {
            var invalidBase64Key = "InvalidBase64";  // Invalid Base64 string
            _configMock.Setup(config => config["Encryption:Key"]).Returns(invalidBase64Key);
            _configMock.Setup(config => config["Encryption:IV"]).Returns(IvBase64);

            Assert.That(() => new AesEncryptionService(_configMock.Object),
     Throws.ArgumentException.With.Message.Contains("The Encryption:Key setting is not a valid Base64 string."));
        }

        [Test]
        public void Constructor_ShouldThrowArgumentException_WhenIvIsInvalidBase64()
        {
            var invalidBase64Iv = "InvalidBase64";  // Invalid Base64 string
            _configMock.Setup(config => config["Encryption:Key"]).Returns(SecretKeyBase64);
            _configMock.Setup(config => config["Encryption:IV"]).Returns(invalidBase64Iv);

            Assert.That(() => new AesEncryptionService(_configMock.Object),
                Throws.ArgumentException.With.Message.Contains("The Encryption:IV setting is not a valid Base64 string."));
        }

        [Test]
        public void Constructor_WithInvalidKey_ShouldThrowArgumentException()
        {
            // Arrange
            _configMock.Setup(config => config["Encryption:Key"]).Returns("SGVsbG8gVw==");

            // Act & Assert
            Assert.That(() => new AesEncryptionService(_configMock.Object),
                Throws.ArgumentException.With.Message.Contains("The key length must be 16, 24, or 32 bytes."));
        }

        [Test]
        public void Constructor_WithInvalidIv_ShouldThrowArgumentException()
        {
            // Arrange
            _configMock.Setup(config => config["Encryption:IV"]).Returns("SGVsbG8gVw==");

            // Act & Assert
            Assert.That(() => new AesEncryptionService(_configMock.Object),
                Throws.ArgumentException.With.Message.Contains("The IV length must be 16 bytes."));
        }

        #endregion Constructor Tests

        #region Encrypt Method Tests

        [Test]
        public void Encrypt_ShouldReturnEncryptedString()
        {
            // Arrange
            var plainText = "This is a test message";

            // Act
            var encrypted = _aesEncryptionService.Encrypt(plainText);

            // Assert
            Assert.That(encrypted, Is.Not.Null.And.Not.Empty);
            Assert.That(encrypted, Does.Match(@"^[A-Za-z0-9+/=]*$"));
        }

        [Test]
        public void Encrypt_ShouldReturnSameEncryptedString_ForSamePlainText()
        {
            // Arrange
            var plainText = "RepeatableTest";

            // Act
            var encryptedText1 = _aesEncryptionService.Encrypt(plainText);
            var encryptedText2 = _aesEncryptionService.Encrypt(plainText);

            // Assert
            Assert.That(encryptedText2, Is.EqualTo(encryptedText1)); // Encryption should produce the same result every time
        }

        [Test]
        public void Encrypt_WithNullPlainText_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.That(() => _aesEncryptionService.Encrypt(null),
                Throws.ArgumentNullException.With.Message.Contains("Plain text cannot be null"));
        }

        #endregion Encrypt Method Tests

        #region Decrypt Method Tests

        [Test]
        public void Decrypt_ShouldReturnDecryptedString()
        {
            // Arrange
            var plainText = "This is a test message";
            var encrypted = _aesEncryptionService.Encrypt(plainText);

            // Act
            var decrypted = _aesEncryptionService.Decrypt(encrypted);

            // Assert
            Assert.That(decrypted, Is.EqualTo(plainText));
        }

        [Test]
        public void Decrypt_ShouldThrowArgumentNullException_WhenNullCipherTextIsProvided()
        {
            // Act & Assert
            Assert.That(() => _aesEncryptionService.Decrypt(null),
                Throws.ArgumentNullException.With.Message.Contains("Plain text cannot be null"));
        }

        [Test]
        public void Decrypt_ShouldThrowFormatException_WhenInvalidBase64CipherTextIsProvided()
        {
            // Arrange
            var invalidCipherText = "InvalidCipherText";

            // Act & Assert
            Assert.That(() => _aesEncryptionService.Decrypt(invalidCipherText),
                Throws.TypeOf<FormatException>());
        }

        #endregion Decrypt Method Tests

        #region Edge Case Tests

        [Test]
        public void EncryptAndDecrypt_ShouldWorkForEmptyString()
        {
            // Arrange
            var plainText = string.Empty;

            // Act
            var encryptedText = _aesEncryptionService.Encrypt(plainText);
            var decryptedText = _aesEncryptionService.Decrypt(encryptedText);

            // Assert
            Assert.That(decryptedText, Is.EqualTo(plainText)); // Decrypted text should be empty as well
        }

        [Test]
        public void EncryptAndDecrypt_ShouldWorkForLongText()
        {
            // Arrange
            var longText = new string('A', 10000);  // Test with a long string

            // Act
            var encryptedText = _aesEncryptionService.Encrypt(longText);
            var decryptedText = _aesEncryptionService.Decrypt(encryptedText);

            // Assert
            Assert.That(decryptedText, Is.EqualTo(longText)); // Ensure the long text is decrypted correctly
        }

        #endregion Edge Case Tests
    }
}