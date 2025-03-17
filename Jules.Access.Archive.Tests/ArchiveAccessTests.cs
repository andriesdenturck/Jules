using AutoMapper;
using FluentAssertions;
using Jules.Access.Archive.Service;
using Jules.Access.Archive.Service.Models;
using Jules.Util.Security.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ArchiveAccess = Jules.Access.Archive.Service.ArchiveAccess;
using ItemInfo = Jules.Access.Archive.Contracts.Models.ItemInfo;

namespace Jules.Access.Archive.Tests
{
    [TestFixture]
    public class ArchiveAccessTests
    {
        private Mock<IUserContext> _mockUserContext;
        private Mock<ILogger<ArchiveAccess>> _mockLogger;
        private Mock<IMapper> _mockMapper;
        private ArchiveDbContext _dbContext;
        private ArchiveAccess _archiveAccess;
        private string _userName;
        private Guid _userId;

        [SetUp]
        public void SetUp()
        {
            _userId = Guid.NewGuid();
            _userName = "JulesVerne";

            _mockUserContext = new Mock<IUserContext>();
            _mockUserContext.Setup(u => u.UserId).Returns(_userId);
            _mockUserContext.Setup(u => u.UserName).Returns(_userName);
            _mockUserContext.Setup(u => u.IsInRole("admin")).ReturnsAsync(false);

            _mockLogger = new Mock<ILogger<ArchiveAccess>>();
            _mockMapper = new Mock<IMapper>();

            var options = new DbContextOptionsBuilder<ArchiveDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ArchiveDbContext(options, _mockUserContext.Object);

            _archiveAccess = new ArchiveAccess(_dbContext, _mockUserContext.Object, _mockLogger.Object);
        }

        [Test]
        public async Task CreateFolder_Should_Create_New_Folder_When_Valid()
        {
            var parent = new ArchiveItemDb { Id = Guid.NewGuid(), Name = _userName, IsFolder = true };
            _dbContext.Items.Add(parent);
            await _dbContext.SaveChangesAsync();

            var path = $"file:///{_userName}/folderA/";

            var result = await _archiveAccess.CreateFolderAsync(path);

            var item = _dbContext.Items.SingleOrDefault(i => i.Path == path);
            item.Should().NotBeNull();
            result.Should().NotBeNull();
            result.Name.Should().Be("folderA");
        }

        [Test]
        public async Task CreateFolder_Should_Return_Folder_When_Already_Exists()
        {
            var path = $"file:///{_userName}/folderA/";

            var parent = new ArchiveItemDb { Id = Guid.NewGuid(), Name = _userName, IsFolder = true };
            var item = new ArchiveItemDb { Id = Guid.NewGuid(), Parent = parent, Name = "folderA/", IsFolder = true };

            _dbContext.Items.Add(parent);
            _dbContext.Items.Add(item);
            await _dbContext.SaveChangesAsync();

            var ItemInfo = await _archiveAccess.CreateFolderAsync(path);
            ItemInfo.Path.Should().Be(path);
        }

        [Test]
        public async Task CreateFile_Should_Create_File_When_User_Has_Permission()
        {
            var parent = new ArchiveItemDb
            {
                Id = Guid.NewGuid(),
                Path = $"file:///{_userName}/",
                Name = string.Empty,
                IsFolder = true
            };
            _dbContext.Items.Add(parent);
            _dbContext.ItemPermissions.Add(new ItemPermissionDb
            {
                ItemId = parent.Id,
                UserId = _userId,
                PermissionType = PermissionType.CreateFile
            });
            await _dbContext.SaveChangesAsync();

            var file = new ItemInfo { Name = "myfile.txt", Path = $"file:///{_userName}/myfile.txt", TokenId = "token", MimeType = "some-type" };

            var result = await _archiveAccess.CreateFileAsync(file);

            var item = _dbContext.Items.Single(i => i.Name == "myfile.txt");
            item.Should().NotBeNull();
            result.Name.Should().Be("myfile.txt");
        }

        [Test]
        public void CreateFile_Should_Throw_When_File_Already_Exists()
        {
            ArchiveItemDb entity = new ArchiveItemDb { IsFolder = true, Name = _userName, Path = $"file:///{_userName}" };
            _dbContext.Items.Add(entity);
            _dbContext.Items.Add(new ArchiveItemDb { Name = "myfile.txt", Parent = entity });
            _dbContext.SaveChanges();

            var file = new ItemInfo { Name = "myfile.txt", Path = $"file:///{_userName}/myfile.txt", TokenId = "token", MimeType = "some-type" };

            Assert.ThrowsAsync<Exception>(async () => await _archiveAccess.CreateFileAsync(file));
        }

        [Test]
        public async Task CreateFile_Should_Throw_When_User_Lacks_Permission()
        {
            var parent = new ArchiveItemDb { Id = Guid.NewGuid(), Path = $"file:///{Guid.NewGuid()}/", Name = string.Empty, IsFolder = true };
            _dbContext.Items.Add(parent);
            await _dbContext.SaveChangesAsync();

            var file = new ItemInfo { Name = "myfile.txt", Path = $"file:///{_userName}/myfile.txt", TokenId = "token", MimeType = "some-type" };

            _mockUserContext.Setup(u => u.UserId).Returns(Guid.NewGuid());

            Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await _archiveAccess.CreateFileAsync(file));
        }

        [Test]
        public async Task Delete_Should_Remove_Item_When_User_Has_Permission()
        {
            var parent = new ArchiveItemDb { Id = Guid.NewGuid(), Path = $"file:///{Guid.NewGuid()}", Name = string.Empty, IsFolder = true };
            var item = new ArchiveItemDb { Id = Guid.NewGuid(), Parent = parent, Name = "file.txt" };
            _dbContext.Items.AddRange(parent, item);
            _dbContext.ItemPermissions.Add(new ItemPermissionDb
            {
                ItemId = item.Id,
                UserId = _userId,
                PermissionType = PermissionType.Delete
            });
            await _dbContext.SaveChangesAsync();

            await _archiveAccess.DeleteAsync(item.Path);

            _dbContext.Items.Any(i => i.Path == item.Path).Should().BeFalse();
        }

        [Test]
        public async Task Delete_Should_Throw_When_User_Lacks_Permission()
        {
            var parent = new ArchiveItemDb { Id = Guid.NewGuid(), Path = $"file:///{Guid.NewGuid()}", Name = string.Empty, IsFolder = true };
            var item = new ArchiveItemDb { Id = Guid.NewGuid(), Parent = parent, Name = "path" };
            _dbContext.Items.Add(parent);
            _dbContext.Items.Add(item);
            await _dbContext.SaveChangesAsync();

            _mockUserContext.Setup(u => u.UserId).Returns(new Guid());

            Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await _archiveAccess.DeleteAsync(item.Path));
        }

        [Test]
        public async Task HasPrivilegesForItem_Should_Return_True_For_Admin()
        {
            _mockUserContext.Setup(u => u.IsInRole("admin")).ReturnsAsync(true);
            var parent = new ArchiveItemDb { Id = Guid.NewGuid(), Path = $"file:///{Guid.NewGuid()}", Name = string.Empty, IsFolder = true };
            var item = new ArchiveItemDb { Id = Guid.NewGuid(), Parent = parent, Name = "path" };
            _dbContext.Items.AddRange(parent, item);
            await _dbContext.SaveChangesAsync();

            var result = await _archiveAccess.HasPrivilegesForItemAsync(item.Path, PermissionType.Read);
            result.Should().BeTrue();
        }

        [Test]
        public async Task GetChildren_Should_Return_Accessible_Children()
        {
            var parent = new ArchiveItemDb { Id = Guid.NewGuid(), Name = _userName, IsFolder = true };
            var child1 = new ArchiveItemDb { Id = Guid.NewGuid(), Name = "child1.txt", IsFolder = false, Parent = parent };
            var child2 = new ArchiveItemDb { Id = Guid.NewGuid(), Name = "child2.txt", IsFolder = false, Parent = parent };

            _dbContext.Items.AddRange(parent, child1, child2);
            _dbContext.ItemPermissions.Add(new ItemPermissionDb
            {
                UserId = _userId,
                ItemId = parent.Id,
                PermissionType = PermissionType.Read
            });
            await _dbContext.SaveChangesAsync();

            var children = (await _archiveAccess.GetChildrenAsync($"{_userName}/", false)).ToList();
            children.Should().Contain(c => c.Name.EndsWith("child1.txt") || c.Name.EndsWith("child2.txt"));
        }
    }
}