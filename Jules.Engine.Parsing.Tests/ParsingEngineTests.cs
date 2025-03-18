using Jules.Access.Blob.Contracts;
using Jules.Engine.Parsing.Contracts.Models;
using Jules.Engine.Parsing.Service;
using Jules.Util.Security.Contracts;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.IO.Compression;
using System.Text;
using Blob = Jules.Access.Blob.Contracts.Models.Blob;

namespace Jules.Engine.Parsing.Tests
{
    public class ParsingEngineTests
    {
        private Mock<IBlobAccess> mockBlobAccess;
        private Mock<ILogger<ParsingEngine>> mockLogger;
        private ParsingEngine parsingEngine;

        [SetUp]
        public void SetUp()
        {
            mockBlobAccess = new Mock<IBlobAccess>();
            mockLogger = new Mock<ILogger<ParsingEngine>>();

            parsingEngine = new ParsingEngine(mockBlobAccess.Object, mockLogger.Object);
        }

        [Test]
        public async Task FindInFile_ShouldReturnTrue_WhenBase64StartsWithSearchStringAndMimeTypeIsTextPlain()
        {
            var tokenId = "validToken";
            var searchString = "Hello";
            var blob = new Blob
            {
                Data = Encoding.UTF8.GetBytes("HelloWorld"),
                MimeType = "text/plain"
            };

            mockBlobAccess.Setup(b => b.ReadAsync(tokenId)).ReturnsAsync(blob);

            var result = await parsingEngine.FindInFileAsync(tokenId, searchString);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task FindInFile_ShouldReturnFalse_WhenBase64DoesNotStartWithSearchString()
        {
            var tokenId = "validToken";
            var searchString = "Goodbye"; // Base64 of "Goodbye"
            var blob = new Blob
            {
                Data = Encoding.UTF8.GetBytes("HelloWorld"),
                MimeType = "text/plain"
            };

            mockBlobAccess.Setup(b => b.ReadAsync(tokenId)).ReturnsAsync(blob);

            var result = await parsingEngine.FindInFileAsync(tokenId, searchString);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task FindInFile_ShouldReturnTrue_WhenSearchStringIsEmpty()
        {
            var tokenId = "validToken";
            var blob = new Blob
            {
                Data = Encoding.UTF8.GetBytes("Anything"),
                MimeType = "application/json"
            };

            mockBlobAccess.Setup(b => b.ReadAsync(tokenId)).ReturnsAsync(blob);

            var result = await parsingEngine.FindInFileAsync(tokenId, "");

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task FindInFile_ShouldReturnFalse_WhenBlobDataIsEmpty()
        {
            var tokenId = "validToken";
            var blob = new Blob
            {
                Data = new byte[0],
                MimeType = "text/plain"
            };

            mockBlobAccess.Setup(b => b.ReadAsync(tokenId)).ReturnsAsync(blob);

            var result = await parsingEngine.FindInFileAsync(tokenId, "Hello");

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task FindInFile_ShouldThrowArgumentNullException_WhenTokenIdIsNull()
        {
            // Arrange
            string tokenId = null;
            var searchString = "Hello";

            // Act
            var result = await parsingEngine.FindInFileAsync(tokenId, searchString);

            // Assert
            Assert.That(result, Is.False); // Assuming method now returns false in this case

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Token ID is null")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once
            );
        }

        [Test]
        public async Task FindInFile_ShouldThrowNotSupportedException_WhenMimeTypeIsNotTextBased()
        {
            var tokenId = "invalid-mime";
            var blob = new Blob
            {
                Data = Encoding.UTF8.GetBytes("some data"),
                MimeType = "application/octet-stream"
            };

            mockBlobAccess.Setup(b => b.ReadAsync(tokenId)).ReturnsAsync(blob);

            Assert.ThrowsAsync<NotSupportedException>(() => parsingEngine.FindInFileAsync(tokenId, "data"));
        }

        [Test]
        public async Task FindInFile_ShouldThrowInvalidOperationException_WhenBlobAccessThrows()
        {
            mockBlobAccess.Setup(b => b.ReadAsync("bad-token")).Throws(new InvalidOperationException("Failed to read"));

            Assert.ThrowsAsync<InvalidOperationException>(() => parsingEngine.FindInFileAsync("bad-token", "search"));
        }

        [Test]
        public async Task Compress_ShouldCreateZipArchive_WithCorrectEntries()
        {
            // Arrange
            var fileItems = new List<FileItem>
            {
                new FileItem { TokenId = "file1", Path = "folder1/file1.txt" },
                new FileItem { TokenId = "file2", Path = "folder1/folder2/file2.txt" }
            };

            var blob1 = new Blob
            {
                Data = Encoding.UTF8.GetBytes("File 1 Content"),
                MimeType = "text/plain",
                FileName = "file1.txt"
            };

            var blob2 = new Blob
            {
                Data = Encoding.UTF8.GetBytes("File 2 Content"),
                MimeType = "text/plain",
                FileName = "file2.txt"
            };

            mockBlobAccess.Setup(b => b.ReadAsync("file1")).ReturnsAsync(blob1);
            mockBlobAccess.Setup(b => b.ReadAsync("file2")).ReturnsAsync(blob2);

            // Act
            var compressedData = await parsingEngine.CompressAsync(fileItems);

            // Assert
            using (var memoryStream = new MemoryStream(compressedData.Data))
            using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Read))
            {
                Assert.That(zipArchive.Entries.Count, Is.EqualTo(2));  // Ensure two files in the zip

                // Check if both files are inside the zip
                var entry1 = zipArchive.GetEntry("folder1/file1.txt");
                var entry2 = zipArchive.GetEntry("folder1/folder2/file2.txt");

                Assert.That(entry1, Is.Not.Null);
                Assert.That(entry2, Is.Not.Null);

                // Check the content of the first file
                using (var entryStream = entry1.Open())
                using (var reader = new StreamReader(entryStream))
                {
                    var content = reader.ReadToEnd();
                    Assert.That(content, Is.EqualTo("File 1 Content"));
                }

                // Check the content of the second file
                using (var entryStream = entry2.Open())
                using (var reader = new StreamReader(entryStream))
                {
                    var content = reader.ReadToEnd();
                    Assert.That(content, Is.EqualTo("File 2 Content"));
                }
            }
        }

        [Test]
        public async Task Compress_ShouldCreateEmptyZip_WhenNoFilesAreProvided()
        {
            // Arrange
            var fileItems = new List<FileItem>(); // Empty list

            // Act
            var compressedData = await parsingEngine.CompressAsync(fileItems);

            // Assert
            using (var memoryStream = new MemoryStream(compressedData.Data))
            using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Read))
            {
                Assert.That(zipArchive.Entries.Count, Is.EqualTo(0));  // Ensure the zip is empty
            }
        }

        [Test]
        public async Task Compress_ShouldHandleSingleFileCorrectly()
        {
            // Arrange
            var fileItems = new List<FileItem>
            {
                new FileItem { TokenId = "file1", Path = "/folder1/file1.txt" }
            };

            var blob1 = new Blob
            {
                Data = Encoding.UTF8.GetBytes("Single File Content"),
                MimeType = "text/plain",
                FileName = "file1.txt"
            };

            mockBlobAccess.Setup(b => b.ReadAsync("file1")).ReturnsAsync(blob1);

            // Act
            var compressedData = await parsingEngine.CompressAsync(fileItems);

            // Assert
            using (var memoryStream = new MemoryStream(compressedData.Data))
            using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Read))
            {
                Assert.That(zipArchive.Entries.Count, Is.EqualTo(1));  // Ensure one file in the zip
                var entry1 = zipArchive.GetEntry("folder1/file1.txt");
                Assert.That(entry1, Is.Not.Null);

                // Check the content of the file
                using (var entryStream = entry1.Open())
                using (var reader = new StreamReader(entryStream))
                {
                    var content = reader.ReadToEnd();
                    Assert.That(content, Is.EqualTo("Single File Content"));
                }
            }
        }
    }
}