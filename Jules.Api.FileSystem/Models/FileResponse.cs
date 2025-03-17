namespace Jules.Api.FileSystem.Models;

public class FileResponse
{
    public string FolderPath { get; set; }
    public string FileName { get; set; }
    public string MimeType { get; set; }
    public byte[]? Content { get; set; }
}