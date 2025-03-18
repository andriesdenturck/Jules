namespace Jules.Access.Archive.Contracts.Models;

/// <summary>
/// Represents metadata information for a file or folder in the system.
/// This class contains properties such as the item's unique ID, name, path, MIME type, and size.
/// </summary>
public class ItemInfo
{
    /// <summary>
    /// Gets or sets a value indicating whether this entry is a folder.
    /// This property is used to differentiate between files and folders.
    /// </summary>
    public bool IsFolder { get; set; }

    /// <summary>
    /// Gets or sets the token ID for the item.
    /// This token is used to reference the actual file content.
    /// </summary>
    public string TokenId { get; set; }

    /// <summary>
    /// Gets or sets the path of the item.
    /// This could be a file system path or a virtual path representing the item's location.
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Gets or sets the MIME type of the file.
    /// The MIME type specifies the media type of the file, such as "application/pdf" or "image/jpeg".
    /// Not applicable for folders.
    /// </summary>
    public string MimeType { get; set; }

    /// <summary>
    /// Gets or sets the size of the file in bytes.
    /// Not applicable for folders.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the creation date and time of the item.
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }
}
