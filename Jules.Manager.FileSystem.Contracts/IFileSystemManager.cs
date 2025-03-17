using Jules.Manager.FileSystem.Contracts.Models;

namespace Jules.Manager.FileSystem.Contracts
{
    /// <summary>
    /// Interface for managing file system operations.
    /// This interface defines methods for creating, deleting, listing, searching, and downloading files and folders.
    /// </summary>
    public interface IFileSystemManager
    {
        /// <summary>
        /// Creates a new file at the specified folder path with the given content.
        /// This method accepts the folder path where the file should be created and a <see cref="FileContent"> object representing the content of the file.
        /// The method returns an <see cref="Item"> object representing the newly created file.
        /// </summary>
        /// <param name="folderPath">The path of the folder where the file will be created.</param>
        /// <param name="fileContent">An object containing the content and metadata of the file (name, MIME type, data).</param>
        /// <returns>An <see cref="Item"> object representing the created file.</returns>
        Task<Item> CreateFileAsync(string folderPath, FileContent fileContent);

        /// <summary>
        /// Creates a new folder at the specified folder path.
        /// This method creates a folder in the file system at the given path and returns an <see cref="Item"> object representing the newly created folder.
        /// </summary>
        /// <param name="folderPath">The path where the new folder will be created.</param>
        /// <returns>An <see cref="Item"> object representing the created folder.</returns>
        Task<Item> CreateFolderAsync(string folderPath);

        /// <summary>
        /// Deletes the file or folder at the given path.
        /// This method removes the file or folder from the system at the specified path.
        /// It returns a boolean value indicating whether the operation was successful.
        /// If the item does not exist or there are permission issues, it returns false.
        /// </summary>
        /// <param name="path">The path of the file or folder to be deleted.</param>
        /// <returns>A boolean value: true if the file or folder was successfully deleted, otherwise false.</returns>
        Task<bool> DeleteAsync(string path);

        /// <summary>
        /// Lists all files and folders within a given folder path.
        /// This method retrieves all files and folders located in the specified folder and returns them as an enumeration of <see cref="Item"> objects.
        /// It only returns files, not folders.
        /// </summary>
        /// <param name="folderPath">The path of the folder where the files are located.</param>
        /// <returns>An <see cref="IEnumerable"> of <see cref="Item"> objects representing the files and folders within the specified folder.</returns>
        Task<IEnumerable<Item>> ListItemsAsync(string folderPath);

        /// <summary>
        /// Searches for a specific string within files in a folder and returns a list of items found.
        /// The method scans the files in the specified folder and looks for occurrences of the given search string.
        /// The number parameter controls the maximum number of results to return. If no number is specified, it defaults to 10 results.
        /// </summary>
        /// <param name="folder">The path of the folder where the files will be searched.</param>
        /// <param name="searchstring">The string to search for within the files.</param>
        /// <param name="number">The maximum number of results to return (default is 10).</param>
        /// <returns>An `IEnumerable` of <see cref="Item"> objects representing the files that contain the search string.</returns>
        Task<IEnumerable<Item>> FindInFilesAsync(string folder, string searchstring, int number = 10);

        /// <summary>
        /// Downloads the content of a file from the specified path.
        /// This method retrieves the file at the given path and returns its content as a <see cref="FileContent"> object.
        /// The returned object includes the file's name, MIME type, and binary data.
        /// </summary>
        /// <param name="path">The path of the file to be downloaded.</param>
        /// <returns>A <see cref="FileContent"> object representing the downloaded file's content.</returns>
        Task<FileContent> DownloadAsync(string path);
    }
}