namespace Jules.Access.Archive.Service.Models;

public class FileMetaDataDb
{
    public Guid Id { get; set; }
    public Guid? FileId { get; set; }
    public ArchiveItemDb? File { get; set; }
    public long? Size { get; set; }
    public string MimeType { get; set; }
    public string TokenId { get; set; }
}