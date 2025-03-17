namespace Jules.Access.Archive.Contracts.Models;

/// <summary>
/// Represents a file's metadata information in the system.
/// This class contains properties such as the file's unique ID, name, path, MIME type, and size.
/// </summary>
public class ItemInfo
{
    /// <summary>
    /// Gets or sets the unique identifier for the file.
    /// This GUID serves as a unique key to identify the file in the system.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this entry is a folder.
    /// This property is used to differentiate between files and folders.
    /// </summary>
    public bool IsFolder { get; set; }

    /// <summary>
    /// Gets or sets the token ID for the file.
    /// This token may is used to reference to the actual file content.
    /// </summary>
    public string TokenId { get; set; }

    /// <summary>
    /// Gets or sets the file name.
    /// This is the name of the file, including its extension, e.g., "document.txt" or "image.jpg".
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the path of the file
    /// This could be a file system path or a URL pointing to the file's location.
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Gets or sets the MIME type of the file.
    /// The MIME type specifies the media type of the file, such as "application/pdf" or "image/jpeg".
    /// </summary>
    public string MimeType { get; set; }

    /// <summary>
    /// Gets or sets the size of the file in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the creation date and time of the file.
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }
}