namespace Jules.Api.FileSystem.Models;

public class FileRequest
{
    public string FolderPath { get; set; }
    public IFormFile File { get; set; }
}