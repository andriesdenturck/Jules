using Jules.Access.Blob.Service;
using Jules.Access.Blob.Service.Models;
using Jules.Util.Security.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Jules.Access.Blob.Tests
{
    public class BlobAccessTests
    {
        private Mock<IEncryptionService> mockEncryption;
        private Mock<ILogger<BlobAccess>> mockLogger;
        private BlobDbContext dbContext;
        private BlobAccess blobAccess;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<BlobDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            dbContext = new BlobDbContext(options);
            mockEncryption = new Mock<IEncryptionService>();
            mockLogger = new Mock<ILogger<BlobAccess>>();

            blobAccess = new BlobAccess(dbContext, mockEncryption.Object, mockLogger.Object);
        }

        [Test]
        public async Task Create_Should_Save_And_Return_Encrypted_Id()
        {
            var blob = new Contracts.Models.Blob { FileName = "file.txt", Data = [0x1, 0x2], MimeType = "text/plain" };
            mockEncryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns("encrypted-id");

            var result = await blobAccess.CreateAsync(blob);

            Assert.That(result, Is.EqualTo("encrypted-id"));
            Assert.That(dbContext.Blobs.Count(), Is.EqualTo(1));

            // Assert that the CreatedOn property is set
            var savedBlob = dbContext.Blobs.Single();  // Retrieve the saved blob from the database
            Assert.That(savedBlob.CreatedOn, Is.Not.EqualTo(DateTimeOffset.MinValue));  // Ensure CreatedOn is not the default value
            Assert.That(savedBlob.CreatedOn, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(5)));  // Ensure CreatedOn is close to the current time (allowing for a small range)
        }

        [Test]
        public void Create_With_Null_Should_Throw()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => blobAccess.CreateAsync(null));
        }

        [Test]
        public async Task Read_Should_Return_Valid_Blob()
        {
            var id = Guid.NewGuid();
            dbContext.Blobs.Add(new BlobDb { Id = id, FileName = "hello.txt", Data = [0x3], MimeType = "text/plain" });
            dbContext.SaveChanges();

            mockEncryption.Setup(e => e.Decrypt("valid-token")).Returns(id.ToString());

            var result = await blobAccess.ReadAsync("valid-token");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.FileName, Is.EqualTo("hello.txt"));
        }

        [Test]
        public void Read_With_Invalid_Token_Should_Throw_ArgumentException()
        {
            mockEncryption.Setup(e => e.Decrypt("invalid-token")).Throws<FormatException>();

            var ex = Assert.ThrowsAsync<ArgumentException>(() => blobAccess.ReadAsync("invalid-token"));
            Assert.That(ex.InnerException, Is.TypeOf<FormatException>());
        }

        [Test]
        public void Read_With_Unknown_Id_Should_Throw_KeyNotFoundException()
        {
            var randomId = Guid.NewGuid();
            mockEncryption.Setup(e => e.Decrypt("unknown-token")).Returns(randomId.ToString());

            Assert.ThrowsAsync<KeyNotFoundException>(() => blobAccess.ReadAsync("unknown-token"));
        }

        [Test]
        public async Task Delete_Should_Remove_Blob()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            dbContext.Blobs.Add(new BlobDb { Id = id1, FileName = "todelete1.txt", MimeType = "text/plain", Data = new byte[0] });
            dbContext.Blobs.Add(new BlobDb { Id = id2, FileName = "todelete2.txt", MimeType = "text/plain", Data = new byte[0] });
            dbContext.SaveChanges();

            mockEncryption.Setup(e => e.Decrypt("delete-token1")).Returns(id1.ToString());
            mockEncryption.Setup(e => e.Decrypt("delete-token2")).Returns(id2.ToString());

            await blobAccess.BulkDeleteAsync(new string[] { "delete-token1", "delete-token2" });

            Assert.That(dbContext.Blobs.Any(), Is.False);
        }

        [Test]
        public void Delete_Non_Existent_Should_Throw_KeyNotFoundException()
        {
            var id = Guid.NewGuid();
            mockEncryption.Setup(e => e.Decrypt("bad-token")).Returns(id.ToString());

            Assert.ThrowsAsync<KeyNotFoundException>(() => blobAccess.DeleteAsync("bad-token"));
        }

        [Test]
        public async Task BulkDelete_Should_Delete_All()
        {
            var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };

            dbContext.Blobs.AddRange(
                new BlobDb { Id = ids[0], FileName = "file1", MimeType = "text/plain", Data = new byte[0] },
                new BlobDb { Id = ids[1], FileName = "file2", MimeType = "text/plain", Data = new byte[0] }
            );
            dbContext.SaveChanges();

            var tokens = new[] { "token1", "token2" };
            mockEncryption.Setup(e => e.Decrypt("token1")).Returns(ids[0].ToString());
            mockEncryption.Setup(e => e.Decrypt("token2")).Returns(ids[1].ToString());

            await blobAccess.BulkDeleteAsync(tokens);

            Assert.That(dbContext.Blobs.Any(), Is.False);
        }

        [Test]
        public async Task BulkDelete_With_Empty_Tokens_Should_Do_Nothing()
        {
            await blobAccess.BulkDeleteAsync(new List<string>());

            Assert.Pass("No exceptions thrown, as expected.");
        }

        [Test]
        public void BulkDelete_With_One_Bad_Token_Should_Throw_KeyNotFoundException()
        {
            var goodId = Guid.NewGuid();
            dbContext.Blobs.Add(new BlobDb { Id = goodId, FileName = "file-good", MimeType = "text/plain", Data = new byte[0] });
            dbContext.SaveChanges();

            mockEncryption.Setup(e => e.Decrypt("good")).Returns(goodId.ToString());
            mockEncryption.Setup(e => e.Decrypt("bad")).Returns(Guid.NewGuid().ToString());

            Assert.ThrowsAsync<KeyNotFoundException>(() => blobAccess.BulkDeleteAsync(new[] { "good", "bad" }));
        }

        [Test]
        public async Task Mapper_Should_Handle_Minimal_Fields()
        {
            var blob = new Contracts.Models.Blob { FileName = "onlyname", MimeType = "text/plain", Data = new byte[0] };
            mockEncryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns("enc");

            var result = await blobAccess.CreateAsync(blob);

            Assert.That(result, Is.EqualTo("enc"));
            Assert.That(dbContext.Blobs.Count(), Is.EqualTo(1));
        }

        // Additional unit test for Update method (if required)
        [Test]
        public async Task Update_Should_Save_And_Return_Encrypted_Id()
        {
            var id = Guid.NewGuid();
            var encryptedId = "encrypted-id";
            dbContext.Blobs.Add(new BlobDb { Id = id, FileName = "test", MimeType = "text/plain", Data = new byte[0] });
            dbContext.SaveChanges();

            var blob = new Contracts.Models.Blob { TokenId = encryptedId, FileName = "update.txt", Data = new byte[] { 0x3, 0x4 }, MimeType = "text/plain" };

            mockEncryption.Setup(e => e.Decrypt(It.IsAny<string>())).Returns(id.ToString());
            mockEncryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns(encryptedId);

            var result = await blobAccess.UpdateAsync(blob);

            Assert.That(result, Is.EqualTo(encryptedId));

            var updatedBlob = dbContext.Blobs.Single();
            Assert.That(updatedBlob.FileName, Is.EqualTo("update.txt"));
        }

        // Additional unit test for Update with Null Blob
        [Test]
        public void Update_With_Null_Should_Throw_ArgumentNullException()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => blobAccess.UpdateAsync(null));
        }
    }
}