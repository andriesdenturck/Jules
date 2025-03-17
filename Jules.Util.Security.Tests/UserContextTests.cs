using Jules.Util.Security.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using NUnit.Framework;
using System.Security.Claims;

namespace Jules.Util.Security.Tests
{
    [TestFixture]
    public class UserContextTests
    {
        private Mock<UserManager<ApplicationUser>> _mockUserManager;
        private Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private Mock<ClaimsPrincipal> _mockClaimsPrincipal;
        private UserContext _userContext;

        [SetUp]
        public void SetUp()
        {
            // Mock IHttpContextAccessor
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            // Mock ClaimsPrincipal (this represents the user)
            _mockClaimsPrincipal = new Mock<ClaimsPrincipal>();

            // Set up the mock HttpContext to return the mocked ClaimsPrincipal
            _mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(_mockClaimsPrincipal.Object);

            // Mock UserManager
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null
            );

            // Instantiate the UserContext with mocked dependencies
            _userContext = new UserContext(_mockUserManager.Object, _mockHttpContextAccessor.Object);
        }

        #region Tests for User Property

        [Test]
        public void User_ReturnsClaimsPrincipal()
        {
            // Arrange
            var userName = "testuser";
            var userId = "b3fbaeb9-e8f8-4b1e-bd24-098c3c60221e";

            var expectedClaimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.NameIdentifier, userId),
            }));

            _mockHttpContextAccessor.Setup(x => x.HttpContext.User).Returns(expectedClaimsPrincipal);

            // Act
            var contextUserName = _userContext.UserName;
            var contextUserId = _userContext.UserId;

            // Assert
            Assert.That(contextUserName, Is.EqualTo(userName));
            Assert.That(contextUserId, Is.EqualTo(contextUserId));
        }

        #endregion Tests for User Property

        #region Tests for UserId Property

        [Test]
        public void UserId_ReturnsCorrectUserIdFromClaims()
        {
            // Arrange
            var expectedUserId = Guid.Parse("b3fbaeb9-e8f8-4b1e-bd24-098c3c60221e");
            _mockClaimsPrincipal.Setup(x => x.Claims).Returns(new Claim[] { new Claim(ClaimTypes.NameIdentifier, expectedUserId.ToString()) });

            // Act
            var userId = _userContext.UserId;

            // Assert
            Assert.That(userId, Is.EqualTo(expectedUserId));
        }

        [Test]
        public void UserId_ThrowsException_WhenClaimIsMissing()
        {
            // Arrange
            _mockClaimsPrincipal.Setup(x => x.FindFirst("userId")).Returns((Claim)null);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(GetUserId);

            Assert.That(ex.Message, Is.EqualTo("UserId claim not found or invalid."));
        }

        [Test]
        public void UserId_ThrowsException_WhenClaimIsInvalid()
        {
            // Arrange
            _mockClaimsPrincipal.Setup(x => x.FindFirst("userId")).Returns(new Claim("userId", "invalid-guid"));

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(GetUserId);

            Assert.That(ex.Message, Is.EqualTo("UserId claim not found or invalid."));
        }

        private void GetUserId()
        {
            var _ = _userContext.UserId;
        }

        #endregion Tests for UserId Property

        #region Tests for IsInRole Method

        [Test]
        public async Task IsInRole_ReturnsTrue_WhenUserIsInRole()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var role = "admin";

            // Mock UserManager to return the user with the "Admin" role
            var user = new ApplicationUser { Id = userId, UserName = "testuser" };
            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.IsInRoleAsync(user, "admin")).ReturnsAsync(true);

            // Act
            var result = await _userContext.IsInRole(role);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task IsInRole_ReturnsFalse_WhenUserIsNotInRole()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var role = "admin";

            // Mock UserManager to return no roles for the user
            var user = new ApplicationUser { Id = userId, UserName = "testuser" };
            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string>());

            // Act
            var result = await _userContext.IsInRole(role);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task IsInRole_ReturnsFalse_WhenRoleDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var role = "admin";

            // Mock UserManager to return a different role
            var user = new ApplicationUser { Id = userId, UserName = "testuser" };
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "user" });

            // Act
            var result = await _userContext.IsInRole(role);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion Tests for IsInRole Method
    }
}