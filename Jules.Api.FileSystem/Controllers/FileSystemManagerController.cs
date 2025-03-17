using Jules.Api.FileSystem.Models;
using Jules.Manager.FileSystem.Contracts;
using Jules.Manager.FileSystem.Contracts.Models;
using Jules.Util.Security.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jules.Api.FileSystem.Controllers;

/// <summary>
/// Controller for managing file and folder operations in the virtual file system.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FileSystemManagerController : ControllerBase
{
    private readonly IFileSystemManager fileSystemManager;
    private readonly ISecurity securityContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemManagerController"/> class.
    /// </summary>
    /// <param name="fileSystemManager">Service for file system operations.</param>
    /// <param name="securityContext">Security context for user permissions.</param>
    public FileSystemManagerController(IFileSystemManager fileSystemManager, ISecurity securityContext)
    {
        this.fileSystemManager = fileSystemManager;
        this.securityContext = securityContext;
    }

    /// <summary>
    /// Uploads and creates a new file in the specified folder.
    /// </summary>
    /// <param name="request">File upload request containing file and target folder path.</param>
    /// <returns>200 OK with created file path, 400 BadRequest, or 403 Forbidden.</returns>
    [HttpPost("CreateFile")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateFile([FromForm] FileRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.FolderPath))
            return BadRequest("Invalid file request.");

        var file = request.File;

        try
        {
            var fileName = file.FileName;
            var mimeType = file.ContentType;
            var folderPath = request.FolderPath;

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            var fileContent = new FileContent
            {
                FileName = fileName,
                Data = fileBytes,
                MimeType = mimeType,
                Path = folderPath,
            };

            var result = await fileSystemManager.CreateFileAsync(folderPath, fileContent);

            return Ok(result.Path);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("You do not have permission to create a file in this folder.");
        }
    }

    /// <summary>
    /// Creates a new folder at the specified path.
    /// </summary>
    /// <param name="folderPath">The path where the new folder will be created.</param>
    /// <returns>200 OK with created folder path, 400 BadRequest, or 403 Forbidden.</returns>
    [HttpPost("CreateFolder")]
    public async Task<IActionResult> CreateFolder([FromQuery] string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return BadRequest("Folder path must be provided.");

        try
        {
            var result = await fileSystemManager.CreateFolderAsync(folderPath);
            return Ok(result.Path);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("You do not have permission to create a folder here.");
        }
    }

    /// <summary>
    /// Reads a file from the specified path.
    /// </summary>
    /// <param name="filePath">The path to the file to read.</param>
    /// <returns>200 OK with file content, 400 BadRequest, 403 Forbidden, or 501 NotImplemented.</returns>
    [HttpGet("ReadFile")]
    public async Task<IActionResult> ReadFile([FromQuery] string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return BadRequest("File path must be provided.");

        try
        {
            var content = await fileSystemManager.DownloadAsync(filePath);
            return Ok(MapToResponse(content));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("You do not have permission to read this file.");
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, "Reading multiple files is not yet implemented.");
        }
    }

    /// <summary>
    /// Deletes a file or folder at the specified path.
    /// </summary>
    /// <param name="filePath">The path to the file or folder to delete.</param>
    /// <returns>200 OK on success, 400 BadRequest, or 403 Forbidden.</returns>
    [HttpDelete("Delete")]
    public async Task<IActionResult> Delete([FromQuery] string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return BadRequest("File path must be provided.");

        try
        {
            var result = await fileSystemManager.DeleteAsync(filePath);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("You do not have permission to delete this file or folder.");
        }
    }

    /// <summary>
    /// Lists files and folders inside a specified folder.
    /// </summary>
    /// <param name="folderPath">The path to the folder.</param>
    /// <returns>200 OK with list of items, or 400 BadRequest.</returns>
    [HttpGet("ListItems")]
    public async Task<IActionResult> ListItems([FromQuery] string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return BadRequest("Folder path must be provided.");

        var files = await fileSystemManager.ListItemsAsync(folderPath);
        var result = files.Select(MapToItem).ToArray();

        return Ok(result);
    }

    /// <summary>
    /// Searches for a string within all files in a specified folder.
    /// </summary>
    /// <param name="folder">The folder to search in.</param>
    /// <param name="searchstring">The string to search for in file contents.</param>
    /// <returns>200 OK with matching items, or 400 BadRequest.</returns>
    [HttpGet("FindInFiles")]
    public async Task<IActionResult> FindInFiles([FromQuery] string folder, [FromQuery] string searchstring)
    {
        if (string.IsNullOrWhiteSpace(folder) || string.IsNullOrWhiteSpace(searchstring))
            return BadRequest("Folder and search string must be provided.");

        var files = await fileSystemManager.FindInFilesAsync(folder, searchstring);
        var result = files.Select(MapToItem).ToArray();

        return Ok(result);
    }

    /// <summary>
    /// Maps internal <see cref="FileContent"/> to a <see cref="FileResponse"/> DTO.
    /// </summary>
    /// <param name="request">The file content to map.</param>
    /// <returns>Mapped <see cref="FileResponse"/>.</returns>
    private FileResponse MapToResponse(FileContent request)
    {
        return new FileResponse
        {
            Content = request.Data,
            MimeType = request.MimeType ?? "application/octet-stream",
            FileName = request.FileName ?? "Unnamed",
            FolderPath = request.Path
        };
    }

    /// <summary>
    /// Maps internal <see cref="Manager.FileSystem.Contracts.Models.Item"/> to API <see cref="Models.Item"/>.
    /// </summary>
    /// <param name="item">The item to map.</param>
    /// <returns>Mapped item.</returns>
    private Models.Item MapToItem(Manager.FileSystem.Contracts.Models.Item item)
    {
        return new Models.Item
        {
            Name = item.Name,
            Path = item.Path,
            IsFolder = item.IsFolder,
            CreatedOn = item.CreatedOn,
        };
    }
}
