namespace Jules.Manager.FileSystem.Contracts.Models
{
    /// <summary>
    /// Represents a file or folder item in the file system.
    /// This class contains properties such as the item's name, path, MIME type, folder status, parent item, creation date, and size.
    /// </summary>
    public class Item
    {
        /// <summary>
        /// Gets or sets the name of the item (file or folder).
        /// This property holds the name of the item, including its extension for files (e.g., "document.txt", "image.jpg").
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the path of the item (file or folder).
        /// This property represents the location of the item in the file system. It can be null for root-level items.
        /// This property is marked as internal to restrict its access outside the assembly.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets the MIME type of the item.
        /// The MIME type indicates the media type of the item (e.g., "application/pdf", "image/jpeg"). This is typically relevant for files.
        /// This property is marked as internal to restrict its access outside the assembly.
        /// </summary>
        public string MimeType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the item is a folder.
        /// This property is used to distinguish between files and folders. If true, the item is a folder; otherwise, it's a file.
        /// </summary>
        public bool IsFolder { get; set; }

        /// <summary>
        /// Gets or sets the parent item of this item.
        /// This property represents the parent folder (if any) of the current item. It is null for root-level items.
        /// </summary>
        public Item Parent { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the item was created.
        /// This property represents the timestamp for when the item (file or folder) was created in the system.
        /// </summary>
        public DateTimeOffset CreatedOn { get; set; }

        /// <summary>
        /// Gets or sets the size of the item in bytes.
        /// This property holds the size of the item in bytes. It is only applicable to files and is null for folders.
        /// This property is marked as internal to restrict its access outside the assembly.
        /// </summary>
        public long? Size { get; set; }
    }
}