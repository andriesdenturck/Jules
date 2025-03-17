namespace Jules.Access.Archive.Service.Models;

public class FileInfoDb
{
    public Guid Id { get; set; }
    public long? Size { get; set; }
    public string MimeType { get; set; }
    public string TokenId { get; set; }
}