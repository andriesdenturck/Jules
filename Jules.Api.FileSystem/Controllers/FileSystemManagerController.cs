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
    /// Reads a file from the specified path, or if a folder is specified, returns a ZIP of its contents.
    /// </summary>
    /// <param name="path">The path to the file or folder to read.</param>
    /// <returns>200 OK with file content or ZIP, 400 BadRequest, 403 Forbidden.</returns>
    [HttpGet("DownloadFile")]
    public async Task<IActionResult> DownloadFile([FromQuery] string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return BadRequest("Path must be provided.");

        try
        {
            var content = await fileSystemManager.DownloadAsync(path);
            return File(content.Data, content.MimeType ?? "application/octet-stream", content.FileName ?? "Unnamed");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid("You do not have permission to read this file or folder.");
        }
    }

    /// <summary>
    /// Deletes a file or folder at the specified path.
    /// </summary>
    /// <param name="path">The path to the file or folder to delete.</param>
    /// <returns>200 OK on success, 400 BadRequest, or 403 Forbidden.</returns>
    [HttpDelete("Delete")]
    public async Task<IActionResult> Delete([FromQuery] string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return BadRequest("File path must be provided.");

        try
        {
            var result = await fileSystemManager.DeleteAsync(path);
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
    /// Maps internal <see cref="Manager.FileSystem.Contracts.Models.Item"/> to API <see cref="Models.Item"/>.
    /// </summary>
    /// <param name="item">The item to map.</param>
    /// <returns>Mapped item.</returns>
    private Models.Item MapToItem(Manager.FileSystem.Contracts.Models.Item item)
    {
        return new Models.Item
        {
            Path = item.Path,
            IsFolder = item.IsFolder,
            CreatedOn = item.CreatedOn,
        };
    }
}
