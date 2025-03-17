namespace Jules.Access.Blob.Contracts.Models;

/// <summary>
/// Represents a blob (binary large object) with properties for identifying, storing, and managing metadata.
/// </summary>
public class Blob
{
    /// <summary>
    /// Gets or sets the token for the blob.
    /// </summary>
    public string TokenId { get; set; }

    /// <summary>
    /// Gets or sets the name of the blob.
    /// </summary>
    /// <remarks>
    /// The name can be used to identify the blob and may be stored as a file name or another identifier.
    /// </remarks>
    public string FileName { get; set; }

    /// <summary>
    /// Gets or sets the MIME type of the blob, which indicates the format of the data.
    /// </summary>
    public string MimeType { get; set; }

    /// <summary>
    /// Gets or sets the data of the blob as a byte array.
    /// </summary>
    public byte[] Data { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the blob was created.
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }
}