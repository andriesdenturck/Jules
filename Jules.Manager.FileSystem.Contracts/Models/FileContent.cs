namespace Jules.Manager.FileSystem.Contracts.Models
{
    /// <summary>
    /// Represents the content of a file in the system.
    /// This class contains properties such as the file's name, path, MIME type, and binary data.
    /// </summary>
    public class FileContent
    {
        /// <summary>
        /// Gets or sets the name of the file.
        /// This property holds the name of the file, including its extension (e.g., "document.txt", "image.jpg").
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the path of the file.
        /// This could be the file's location on the file system or a URL pointing to the file's storage location.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the MIME type of the file.
        /// The MIME type specifies the media type of the file, such as "application/pdf" or "image/jpeg".
        /// This property helps to identify the file format.
        /// </summary>
        public string MimeType { get; set; }

        /// <summary>
        /// Gets or sets the binary data of the file.
        /// This property holds the file content in byte array format, representing the raw data of the file.
        /// </summary>
        public byte[] Data { get; set; }
    }
}