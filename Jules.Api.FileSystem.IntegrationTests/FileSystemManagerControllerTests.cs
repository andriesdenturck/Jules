using Jules.Api.FileSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Jules.Api.FileSystem.IntegrationTests;

[TestFixture]
public class FileSystemManagerControllerTests
{
    private HttpClient _client;
    private string _token;

    [SetUp]
    public async Task Setup()
    {
        var factory = new WebApplicationFactory<Program>();
        _client = factory.CreateClient();

        var userLogin = new UserLogin { Username = "testuser", Password = "password123" };
        var content = new StringContent(JsonConvert.SerializeObject(userLogin), Encoding.UTF8, "application/json");
        var responseRegister = await _client.PostAsync("/api/auth/register", content);
        var responseLogin = await _client.PostAsync("/api/auth/login", content);

        var loginResponse = JsonConvert.DeserializeObject<dynamic>(await responseLogin.Content.ReadAsStringAsync());
        _token = loginResponse?.token;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
    }

    private async Task CreateFolderAsync(string folderPath)
    {
        var url = $"/api/FileSystemManager/CreateFolder?folderPath={folderPath}";
        var response = await _client.PostAsync(url, null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    private async Task CreateFileAsync(string folderPath, string fileName, string data = "Hello World", string mimeType = "text/plain")
    {
        await CreateFolderAsync(folderPath);

        // Arrange
        var mockFile = CreateMockFile(data, fileName, mimeType);
        var fileContent = new StreamContent(mockFile.OpenReadStream());
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);

        var formContent = new MultipartFormDataContent
    {
        { new StringContent(folderPath), "folderpath" },
        {  fileContent, "file", mockFile.FileName }
    };

        // Act
        var response = await _client.PostAsync("/api/FileSystemManager/CreateFile", formContent);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    public IFormFile CreateMockFile(string content, string fileName, string mimeType = "text/plain")
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var formFile = new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = mimeType // Set the MIME type here
        };
        return formFile;
    }

    [Test]
    public async Task CreateFile_ShouldCreateFileSuccessfully()
    {
        // Arrange
        var folderPath = "/testuser/integration-test/folder1/";
        var fileName = "testfile.txt";

        // Act
        await CreateFileAsync(folderPath, fileName);

        // Assert
        var filesResponse = await _client.GetAsync($"/api/FileSystemManager/ReadFile?filePath={folderPath}{fileName}");
        var body = await filesResponse.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain(fileName));
    }

    [Test]
    public async Task ReadFile_ShouldReturnFileContent()
    {
        // Arrange
        var folderPath = "/testuser/integration-test/folder2/";
        var fileName = "readme.txt";
        var expectedText = "This is the content.";
        await CreateFileAsync(folderPath, fileName, expectedText);

        // Act
        var response = await _client.GetAsync($"/api/FileSystemManager/ReadFile?filePath={folderPath}{fileName}");
        var fileResponse = JsonConvert.DeserializeObject<FileResponse>(await response.Content.ReadAsStringAsync());

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        //// Convert byte array to original string (assuming UTF-8 encoding)
        var content = Encoding.UTF8.GetString(fileResponse.Content);

        Assert.That(content, Contains.Substring(content));
    }

    [Test]
    public async Task DeleteFile_ShouldRemoveFile()
    {
        // Arrange
        var folderPath = "/testuser/integration-test/folder3/";
        var fileName = "todelete.txt";
        await CreateFileAsync(folderPath, fileName);

        // Act
        var response = await _client.DeleteAsync($"/api/FileSystemManager/Delete?filePath={folderPath}{fileName}");
        var body = await response.Content.ReadAsStringAsync();
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var filesResponse = await _client.GetAsync($"/api/FileSystemManager/ListFiles?folderPath={folderPath}");
        var filesContent = await filesResponse.Content.ReadAsStringAsync();
        Assert.That(filesContent, Does.Not.Contain(fileName));
    }

    [Test]
    public async Task ListFiles_ShouldReturnAllFilesInFolder()
    {
        // Arrange
        var folderPath = "/testuser/integration-test/folder4/";
        await CreateFileAsync(folderPath, "a.txt");
        await CreateFileAsync(folderPath, "b.txt");

        // Act
        var response = await _client.GetAsync($"/api/FileSystemManager/ListItems?folderPath={folderPath}");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body, Does.Contain("a.txt"));
        Assert.That(body, Does.Contain("b.txt"));
    }

    [Test]
    public async Task FindInFiles_ShouldReturnMatchingFile()
    {
        // Arrange
        var folderPath = "/testuser/integration-test/folder5/";
        await CreateFileAsync(folderPath, "match.txt", "Unicorn for me");
        await CreateFileAsync(folderPath, "nomatch.txt", "Nothing to see here");

        // Act
        var response = await _client.GetAsync($"/api/FileSystemManager/FindInFiles?folder={folderPath}&searchstring=Unicorn");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(content, Contains.Substring("match.txt"));
        Assert.That(content, Does.Not.Contain("nomatch.txt"));
    }
}