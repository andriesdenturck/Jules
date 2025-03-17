namespace Jules.Access.Blob.Service.Models;

public class BlobDb
{
    public Guid Id { get; set; }
    public string FileName { get; set; }
    public string MimeType { get; set; }
    public byte[] Data { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
}