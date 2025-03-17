using Jules.Util.Security.Contracts;
using Jules.Util.Security.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Security.Claims;

namespace Jules.Util.Security.Service.Tests
{
    [TestFixture]
    public class SecurityTests
    {
        private Mock<UserManager<ApplicationUser>> _mockUserManager;
        private Mock<RoleManager<ApplicationRole>> _mockRoleManager;
        private Mock<IUserContext> _mockUserContext;
        private Mock<IJwtService> _mockJwtService;
        private Security _security;

        [SetUp]
        public void SetUp()
        {
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object,
                null, null, null, null, null, null, null, new Mock<ILogger<UserManager<ApplicationUser>>>().Object);
            _mockRoleManager = new Mock<RoleManager<ApplicationRole>>(
                new Mock<IRoleStore<ApplicationRole>>().Object,
                null, null, null, new Mock<ILogger<RoleManager<ApplicationRole>>>().Object);
            _mockUserContext = new Mock<IUserContext>();
            _mockJwtService = new Mock<IJwtService>();

            _security = new Security(
                _mockUserManager.Object,
                _mockRoleManager.Object,
                _mockUserContext.Object,
                _mockJwtService.Object);
        }

        [Test]
        public async Task Register_ShouldReturnTrue_WhenUserIsSuccessfullyRegistered()
        {
            // Arrange
            var userName = "testuser";
            var userPassword = "password123";
            _mockUserManager.Setup(m => m.FindByNameAsync(userName)).ReturnsAsync((ApplicationUser)null);
            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), userPassword)).ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User")).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _security.Register(userName, userPassword);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void Register_ShouldThrowException_WhenUserAlreadyExists()
        {
            // Arrange
            var userName = "testuser";
            var userPassword = "password123";
            var existingUser = new ApplicationUser { UserName = userName };
            _mockUserManager.Setup(m => m.FindByNameAsync(userName)).ReturnsAsync(existingUser);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _security.Register(userName, userPassword));
            Assert.That(ex.Message, Is.EqualTo("The username is already taken."));
        }

        [Test]
        public async Task Login_ShouldReturnToken_WhenUserIsAuthenticated()
        {
            // Arrange
            var userName = "testuser";
            var userPassword = "password123";
            var userRole = "admin";
            var user = new ApplicationUser { UserName = userName, Id = Guid.NewGuid() };
            _mockUserManager.Setup(m => m.FindByNameAsync(userName)).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.CheckPasswordAsync(user, userPassword)).ReturnsAsync(true);

            _mockJwtService.Setup(s => s.GenerateToken(It.IsAny<Claim[]>())).Returns("mock_token");

            // Act
            var token = await _security.Login(userName, userPassword, userRole);

            // Assert
            Assert.That(token, Is.EqualTo("mock_token"));
        }

        [Test]
        public void Login_ShouldThrowException_WhenInvalidCredentials()
        {
            // Arrange
            var userName = "testuser";
            var userPassword = "wrongpassword";
            var user = new ApplicationUser { UserName = userName };
            _mockUserManager.Setup(m => m.FindByNameAsync(userName)).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.CheckPasswordAsync(user, userPassword)).ReturnsAsync(false);

            // Act & Assert
            var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await _security.Login(userName, userPassword, "User"));
            Assert.That(ex.Message, Is.EqualTo("Invalid username or password."));
        }

        [Test]
        public async Task AssignRole_ShouldReturnTrue_WhenRoleIsAssignedSuccessfully()
        {
            // Arrange
            var userName = "testuser";
            var userRole = "admin";
            var user = new ApplicationUser { UserName = userName };
            var role = new ApplicationRole(userRole);

            _mockUserContext.Setup(m => m.IsInRole("admin")).ReturnsAsync(true);
            _mockUserManager.Setup(m => m.FindByNameAsync(userName)).ReturnsAsync(user);
            _mockRoleManager.Setup(m => m.FindByNameAsync(userRole)).ReturnsAsync(role);
            _mockUserManager.Setup(m => m.AddToRoleAsync(user, userRole)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _security.AssignRole(userName, userRole);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void AssignRole_ShouldThrowException_WhenUserHasNoAdminPrivileges()
        {
            // Arrange
            var userName = "testuser";
            var userRole = "admin";
            _mockUserContext.Setup(m => m.IsInRole("admin")).ReturnsAsync(false);

            // Act & Assert
            var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await _security.AssignRole(userName, userRole));
            Assert.That(ex.Message, Is.EqualTo("User has no Admin privileges."));
        }

        [Test]
        public async Task DeleteUser_ShouldReturnTrue_WhenUserIsSuccessfullyDeleted()
        {
            // Arrange
            var userName = "testuser";
            var user = new ApplicationUser { UserName = userName };
            _mockUserContext.Setup(m => m.IsInRole("admin")).ReturnsAsync(true);
            _mockUserManager.Setup(m => m.FindByNameAsync(userName)).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _security.DeleteUser(userName);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void DeleteUser_ShouldThrowException_WhenUserHasNoAdminPrivileges()
        {
            // Arrange
            var userName = "testuser";
            _mockUserContext.Setup(m => m.IsInRole("Admin")).ReturnsAsync(false);

            // Act & Assert
            var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await _security.DeleteUser(userName));

            Assert.That(ex.Message, Is.EqualTo("User has no Admin privileges."));
        }

        [Test]
        public void DeleteUser_ShouldThrowException_WhenUserDoesNotExist()
        {
            // Arrange
            var userName = "testuser";
            _mockUserContext.Setup(m => m.IsInRole("admin")).ReturnsAsync(true);
            _mockUserManager.Setup(m => m.FindByNameAsync(userName)).ReturnsAsync((ApplicationUser)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<KeyNotFoundException>(async () => await _security.DeleteUser(userName));

            Assert.That(ex.Message, Is.EqualTo("User not found."));
        }
    }
}