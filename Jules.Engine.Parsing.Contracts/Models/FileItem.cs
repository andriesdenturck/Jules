namespace Jules.Engine.Parsing.Contracts.Models
{
    /// <summary>
    /// Represents a file item with its metadata, including file name, folder path, and token identifier.
    /// </summary>
    public class FileItem
    {
        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        /// <value>The name of the file.</value>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the folder path where the file is located.
        /// </summary>
        /// <value>The path to the folder containing the file.</value>
        public string FolderPath { get; set; }

        /// <summary>
        /// Gets or sets the MIME type of the file, which indicates the format of the data.
        /// </summary>
        public string MimeType { get; set; }

        /// <summary>
        /// Gets or sets the encrypted token identifier associated with the file.
        /// </summary>
        /// <value>The token identifier for accessing the file.</value>
        public string TokenId { get; set; }

        /// <summary>
        /// Gets or sets the data as a byte array.
        /// </summary>
        public byte[] Data { get; set; }
    }
}