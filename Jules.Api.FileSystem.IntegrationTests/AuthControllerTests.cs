using Jules.Api.FileSystem.Models;
using Jules.Util.Security.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Jules.Api.FileSystem.IntegrationTests
{
    [TestFixture]
    public class AuthControllerTests
    {
        private HttpClient _client;
        private WebApplicationFactory<Program> _factory; // Using Program.cs here instead of Startup.cs

        [SetUp]
        public async Task SetUp()
        {
            _factory = new WebApplicationFactory<Program>();
            _client = _factory.CreateClient();
        }

        [Test]
        public async Task Register_ShouldReturnCreated_WhenValidData()
        {
            // Arrange
            var userLogin = new UserLogin { Username = "testuser", Password = "password123" };
            var content = new StringContent(JsonConvert.SerializeObject(userLogin), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/auth/register", content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));
        }

        [Test]
        public async Task Login_ShouldReturnOkWithToken_WhenValidCredentials()
        {
            // Arrange
            var userLogin = new UserLogin { Username = "testuser", Password = "password123" };
            var registerContent = new StringContent(JsonConvert.SerializeObject(userLogin), Encoding.UTF8, "application/json");

            // Register the user first
            var registerResponse = await _client.PostAsync("/api/auth/register", registerContent);
            Assert.That(registerResponse.IsSuccessStatusCode, Is.True, "User registration failed during test setup.");

            // Act - now login
            var loginContent = new StringContent(JsonConvert.SerializeObject(userLogin), Encoding.UTF8, "application/json");
            var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);
            var loginResponseContent = await loginResponse.Content.ReadAsStringAsync();

            // Assert
            Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(loginResponseContent.Contains("token"), Is.True);
        }

        [Test]
        public async Task AssignRole_ShouldReturnOk_WhenRoleAssignedSuccessfully()
        {
            // Arrange
            var userLogin = new UserLogin { Username = "testuser", Password = "password123", Role = "admin" };
            var registerTestUser = new StringContent(JsonConvert.SerializeObject(userLogin), Encoding.UTF8, "application/json");
            var registerResponse = await _client.PostAsync("/api/auth/register", registerTestUser);

            var adminLogin = new UserLogin { Username = "admin", Password = "P@ssw0rd", Role = "admin" };
            var content = new StringContent(JsonConvert.SerializeObject(adminLogin), Encoding.UTF8, "application/json");
            var responseLogin = await _client.PostAsync("/api/auth/login", content);

            var adminLoginResponse = JsonConvert.DeserializeObject<dynamic>(await responseLogin.Content.ReadAsStringAsync());
            var token = adminLoginResponse?.token.ToString();

            var userName = "testuser";
            var role = "admin";
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"/api/auth/assignrole?userName={userName}&role={role}");

            // Add Authorization header to the request
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var assignResponse = await _client.SendAsync(requestMessage);
            var loginContent = new StringContent(JsonConvert.SerializeObject(userLogin), Encoding.UTF8, "application/json");
            var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);

            // Assert
            Assert.That(assignResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
        }

        [Test]
        public async Task DeleteUser_ShouldReturnOk_WhenUserDeletedSuccessfully()
        {
            // Arrange
            var userLogin = new UserLogin { Username = "testuser", Password = "password123" };
            var registerTestUser = new StringContent(JsonConvert.SerializeObject(userLogin), Encoding.UTF8, "application/json");
            var registerResponse = await _client.PostAsync("/api/auth/register", registerTestUser);

            var adminLogin = new UserLogin { Username = "admin", Password = "P@ssw0rd", Role = "admin" };
            var content = new StringContent(JsonConvert.SerializeObject(adminLogin), Encoding.UTF8, "application/json");
            var responseLogin = await _client.PostAsync("/api/auth/login", content);

            var adminLoginResponse = JsonConvert.DeserializeObject<dynamic>(await responseLogin.Content.ReadAsStringAsync());
            var token = adminLoginResponse?.token.ToString();

            var userName = "testuser";
            var requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"/api/auth/deleteuser?username={userName}");

            // Add Authorization header to the request
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var deleteResponse = await _client.SendAsync(requestMessage);
            var loginContent = new StringContent(JsonConvert.SerializeObject(userLogin), Encoding.UTF8, "application/json");
            var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);

            // Assert
            Assert.That(deleteResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
            Assert.That(loginResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Unauthorized));
        }

        [TearDown]
        public async Task TearDown()
        {
            // Create a scope to resolve scoped services like UserManager
            using (var scope = _factory.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                // Find the user by username
                var user = await userManager.FindByNameAsync("testuser");

                // If the user exists, delete them
                if (user != null)
                {
                    await userManager.DeleteAsync(user);
                }
            }

            // Clean up the HttpClient and WebApplicationFactory
            _client.Dispose();
            _factory.Dispose();
        }
    }
}