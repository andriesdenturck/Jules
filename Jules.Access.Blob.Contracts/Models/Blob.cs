namespace Jules.Access.Blob.Contracts.Models;

/// <summary>
/// Represents a blob (binary large object) with properties for identifying, storing, and managing metadata.
/// </summary>
public class Blob
{
    /// <summary>
    /// Gets or sets the token identifier for the blob.
    /// </summary>
    public string TokenId { get; set; }

    /// <summary>
    /// Gets or sets the name of the blob.
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Gets or sets the MIME type of the blob, indicating the format of its data (e.g., "application/pdf").
    /// </summary>
    public string MimeType { get; set; }

    /// <summary>
    /// Gets or sets the binary data of the blob.
    /// </summary>
    public byte[] Data { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the blob was created.
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }
}