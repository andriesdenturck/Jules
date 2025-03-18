using Jules.Access.Archive.Contracts;
using Jules.Access.Blob.Contracts;
using Jules.Access.Blob.Contracts.Models;
using Jules.Engine.Parsing.Contracts;
using Jules.Engine.Parsing.Contracts.Models;
using Jules.Manager.FileSystem.Contracts.Models;
using Jules.Manager.FileSystem.Service;
using Jules.Util.Security.Contracts;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Text;
using ItemInfo = Jules.Access.Archive.Contracts.Models.ItemInfo;

namespace Jules.Manager.FileSystem.Tests
{
    [TestFixture]
    public class FileSystemManagerTests
    {
        private FileSystemManager sut;
        private Mock<IUserContext> mockUserContext;
        private Mock<IArchiveAccess> mockArchiveAccess;
        private Mock<IBlobAccess> mockBlobAccess;
        private Mock<IParsingEngine> mockParsingEngine;
        private Mock<ILogger<FileSystemManager>> mockLogger;

        [SetUp]
        public async Task Setup()
        {
            mockUserContext = new Mock<IUserContext>();
            mockArchiveAccess = new Mock<IArchiveAccess>();
            mockBlobAccess = new Mock<IBlobAccess>();
            mockParsingEngine = new Mock<IParsingEngine>();
            mockLogger = new Mock<ILogger<FileSystemManager>>();

            sut = new FileSystemManager(
                mockUserContext.Object,
                mockArchiveAccess.Object,
                mockBlobAccess.Object,
                mockParsingEngine.Object,
                mockLogger.Object
            );
        }

        [Test]
        public async Task CreateFile_WithValidPrivileges_ReturnsItem()
        {
            // Arrange
            string folderPath = "/folder";
            var fileContent = new FileContent { Path = "/folder/file.txt", MimeType = "text/plain", Data = new byte[] { 0x01 } };
            var itemInfo = new ItemInfo();
            var createdItemInfo = new ItemInfo { Path = fileContent.Path, TokenId = "token" };

            mockArchiveAccess.Setup(x => x.HasPrivilegesForItemAsync(folderPath, PermissionType.CreateFile)).ReturnsAsync(true);
            mockBlobAccess.Setup(x => x.CreateAsync(It.IsAny<Blob>())).ReturnsAsync("token");
            mockArchiveAccess.Setup(x => x.CreateFileAsync(It.IsAny<ItemInfo>())).ReturnsAsync(createdItemInfo);

            // Act
            var result = await sut.CreateFileAsync(folderPath, fileContent);

            // Assert
            Assert.That(result, Is.Not.Null);
            mockBlobAccess.Verify(x => x.CreateAsync(It.IsAny<Blob>()), Times.Once);
            mockArchiveAccess.Verify(x => x.CreateFileAsync(It.IsAny<ItemInfo>()), Times.Once);
        }

        [Test]
        public async Task CreateFile_WithoutPrivileges_ThrowsUnauthorizedAccessException()
        {
            mockArchiveAccess.Setup(x => x.HasPrivilegesForItemAsync(It.IsAny<string>(), PermissionType.CreateFile)).ReturnsAsync(false);

            Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await sut.CreateFileAsync("/folder", new FileContent()));
        }

        [Test]
        public async Task CreateFolder_WithValidPrivileges_ReturnsItem()
        {
            var ItemInfo = new ItemInfo { Path = "/new-folder" };
            mockArchiveAccess.Setup(x => x.HasPrivilegesForItemAsync("/new-folder", PermissionType.CreateFolder)).ReturnsAsync(true);
            mockArchiveAccess.Setup(x => x.CreateFolderAsync("/new-folder")).ReturnsAsync(ItemInfo);

            var result = await sut.CreateFolderAsync("/new-folder");

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task Delete_WithValidPrivileges_ReturnsTrue()
        {
            var children = new List<ItemInfo> { new ItemInfo { TokenId = "token" } };
            mockArchiveAccess.Setup(x => x.HasPrivilegesForItemAsync("/folder", PermissionType.Delete)).ReturnsAsync(true);
            mockArchiveAccess.Setup(x => x.GetChildrenAsync("/folder", false)).ReturnsAsync(children.AsQueryable());

            var result = await sut.DeleteAsync("/folder");

            Assert.That(result, Is.True);
            mockArchiveAccess.Verify(x => x.DeleteAsync("/folder"), Times.Once);
            mockBlobAccess.Verify(x => x.BulkDeleteAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
        }

        [Test]
        public async Task Delete_WithoutPrivileges_ThrowsUnauthorizedAccessException()
        {
            mockArchiveAccess.Setup(x => x.HasPrivilegesForItemAsync(It.IsAny<string>(), PermissionType.Delete)).ReturnsAsync(false);

            Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await sut.DeleteAsync("/folder"));
        }

        [Test]
        public async Task FindInFiles_ReturnsMatchingItems()
        {
            var file1 = new ItemInfo { TokenId = "token1", Path = "/file1.txt" };
            var file2 = new ItemInfo { TokenId = "token2", Path = "/file2.txt" };

            mockArchiveAccess.Setup(x => x.GetChildrenAsync("/folder", false)).ReturnsAsync(new[] { file1, file2 }.AsQueryable());
            mockParsingEngine.Setup(x => x.FindInFileAsync(file1.TokenId, "match")).ReturnsAsync(true);
            mockParsingEngine.Setup(x => x.FindInFileAsync(file2.TokenId, "match")).ReturnsAsync(false);

            var result = await sut.FindInFilesAsync("/folder", "match");

            Assert.That(result.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task ListFiles_ReturnsMappedItems()
        {
            var children = new List<ItemInfo> {
                new ItemInfo { Path = "file1.txt" },
                new ItemInfo { Path = "file2.txt" }
            };

            mockArchiveAccess.Setup(x => x.GetChildrenAsync("/folder", null)).ReturnsAsync(children.AsQueryable());

            var result = await sut.ListItemsAsync("/folder");

            Assert.That(result.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task Download_WithSingleChild_ReturnsFileContent()
        {
            var itemInfo = new ItemInfo { TokenId = "token" };
            var blob = new Blob { Data = new byte[] { 0x01 }, MimeType = "text/plain" };

            mockArchiveAccess.Setup(x => x.HasPrivilegesForItemAsync("/file", PermissionType.Read)).ReturnsAsync(true);
            mockArchiveAccess.Setup(x => x.GetChildrenAsync("/file", false)).ReturnsAsync((new[] { itemInfo }).AsQueryable());
            mockBlobAccess.Setup(x => x.ReadAsync(itemInfo.TokenId)).ReturnsAsync(blob);

            var result = await sut.DownloadAsync("/file");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data.Length, Is.EqualTo(1));
        }

        [Test]
        public async Task Download_WithMultipleChildren_ReturnsCompressedFile()
        {
            var fileInfos = new List<ItemInfo> {
                new ItemInfo { TokenId = "token1"  },
                new ItemInfo { TokenId = "token2" }
            };
            var item = new FileItem
            {
                Data = new byte[] { 0x11, 0x22 },
                MimeType = "application/zip",
            };

            mockArchiveAccess.Setup(x => x.HasPrivilegesForItemAsync("/folder", PermissionType.Read)).ReturnsAsync(true);
            mockArchiveAccess.Setup(x => x.GetChildrenAsync("/folder", false)).ReturnsAsync(fileInfos.AsQueryable());

            mockParsingEngine.Setup(x => x.CompressAsync(It.IsAny<IEnumerable<FileItem>>())).ReturnsAsync(item);
            mockParsingEngine.Setup(x => x.CompressAsync(It.IsAny<IEnumerable<FileItem>>())).ReturnsAsync(item);

            var result = await sut.DownloadAsync("/folder");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.MimeType, Is.EqualTo("application/zip"));
        }

        [Test]
        public async Task Download_WithoutPrivileges_ThrowsUnauthorizedAccessException()
        {
            mockArchiveAccess.Setup(x => x.HasPrivilegesForItemAsync("/file", PermissionType.Read)).ReturnsAsync(false);

            Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.DownloadAsync("/file"));
        }

        [Test]
        public async Task Download_ShouldReturnFileContent_WhenFileExistsAndUserHasReadPermission()
        {
            // Arrange
            var filePath = "/some/folder/file.txt";
            var ItemInfo = new ItemInfo { TokenId = "validToken" };
            var blob = new Blob
            {
                Data = Encoding.UTF8.GetBytes("File content"),
                MimeType = "text/plain"
            };

            mockArchiveAccess.Setup(a => a.HasPrivilegesForItemAsync(filePath, PermissionType.Read)).ReturnsAsync(true);
            mockArchiveAccess.Setup(a => a.GetChildrenAsync(filePath, false)).ReturnsAsync(new[] { ItemInfo }.AsQueryable());
            mockBlobAccess.Setup(b => b.ReadAsync("validToken")).ReturnsAsync(blob);

            // Act
            var result = await sut.DownloadAsync(filePath);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data, Is.EqualTo(Encoding.UTF8.GetBytes("File content")));
            Assert.That(result.MimeType, Is.EqualTo("text/plain"));
        }

        [Test]
        public async Task Download_ShouldThrowFileNotFoundException_WhenFileDoesNotExist()
        {
            // Arrange
            var filePath = "/some/folder/nonexistent_file.txt";

            mockArchiveAccess.Setup(a => a.HasPrivilegesForItemAsync(filePath, PermissionType.Read)).ReturnsAsync(true);
            mockArchiveAccess.Setup(a => a.GetChildrenAsync(filePath, false)).ReturnsAsync(Array.Empty<ItemInfo>().AsQueryable());

            // Act & Assert
            Assert.ThrowsAsync<FileNotFoundException>(() => sut.DownloadAsync(filePath));
        }

        [Test]
        public async Task Download_ShouldThrowUnauthorizedAccessException_WhenUserLacksReadPermission()
        {
            // Arrange
            var filePath = "/some/folder/file.txt";

            mockArchiveAccess.Setup(a => a.HasPrivilegesForItemAsync(filePath, PermissionType.Read)).ReturnsAsync(false);

            // Act & Assert
            Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.DownloadAsync(filePath));
        }

        [Test]
        public async Task Download_ShouldThrowException_WhenCompressionFails()
        {
            // Arrange
            var filePath = "/some/folder";
            var fileInfos = new[]
            {
                new ItemInfo { TokenId = "token1" },
                new ItemInfo { TokenId = "token2" }
            };

            mockArchiveAccess.Setup(a => a.HasPrivilegesForItemAsync(filePath, PermissionType.Read)).ReturnsAsync(true);
            mockArchiveAccess.Setup(a => a.GetChildrenAsync(filePath, false)).ReturnsAsync(fileInfos.AsQueryable());
            mockBlobAccess.Setup(b => b.ReadAsync("token1")).ReturnsAsync(new Blob { Data = Encoding.UTF8.GetBytes("File 1 content"), MimeType = "text/plain" });
            mockBlobAccess.Setup(b => b.ReadAsync("token2")).ReturnsAsync(new Blob { Data = Encoding.UTF8.GetBytes("File 2 content"), MimeType = "text/plain" });

            mockParsingEngine.Setup(p => p.CompressAsync(It.IsAny<IEnumerable<FileItem>>())).Throws(new Exception("Compression failed"));

            // Act & Assert
            Assert.ThrowsAsync<Exception>(() => sut.DownloadAsync(filePath));
        }

        [Test]
        public async Task Download_ShouldReturnEmptyContent_WhenFileIsEmpty()
        {
            // Arrange
            var filePath = "/some/folder/emptyFile.txt";
            var itemInfo = new ItemInfo { TokenId = "emptyToken" };
            var blob = new Blob
            {
                Data = Array.Empty<byte>(),
                MimeType = "text/plain"
            };

            mockArchiveAccess.Setup(a => a.HasPrivilegesForItemAsync(filePath, PermissionType.Read)).ReturnsAsync(true);
            mockArchiveAccess.Setup(a => a.GetChildrenAsync(filePath, false)).ReturnsAsync(new[] { itemInfo }.AsQueryable());
            mockBlobAccess.Setup(b => b.ReadAsync("emptyToken")).ReturnsAsync(blob);

            // Act
            var result = await sut.DownloadAsync(filePath);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data, Is.Empty);
        }
    }
}