using Jules.Access.Archive.Contracts.Models;
using Jules.Util.Security.Contracts;

namespace Jules.Access.Archive.Contracts
{
    /// <summary>
    /// Defines operations for accessing and managing files and folders.
    /// This interface provides methods to create, retrieve, delete, and check privileges for files and folders.
    /// </summary>
    public interface IArchiveAccess
    {
        /// <summary>
        /// Creates a new file in the archive with the provided file information.
        /// This method accepts a <see cref="ItemInfo"> object representing the file to be created and returns the created file's metadata.
        /// </summary>
        /// <param name="fileName">The file information, including its name, path, size, etc.</param>
        /// <returns>A <see cref="ItemInfo"> object representing the created file.</returns>
        Task<Models.ItemInfo> CreateFileAsync(Models.ItemInfo fileName);

        /// <summary>
        /// Creates a new folder at the specified path.
        /// This method accepts a path as a string and returns a <see cref="ItemInfo "> object representing the created folder.
        /// </summary>
        /// <param name="path">The path of the folder that should be created.</param>
        /// <returns>A <see cref="ItemInfo "> object representing the created folder.</returns>
        Task<ItemInfo> CreateFolderAsync(string path);

        /// <summary>
        /// Retrieves a file's metadata based on the provided path.
        /// This method returns a <see cref="ItemInfo"> object for the file located at the specified path.
        /// </summary>
        /// <param name="path">The path of the file to retrieve.</param>
        /// <returns>A <see cref="Models.ItemInfo"> object representing the file at the specified path.</returns>
        Task<Models.ItemInfo> GetFileAsync(string path);

        /// <summary>
        /// Retrieves a folder's metadata based on the provided path.
        /// This method returns a <see cref="ItemInfo "> object for the folder located at the specified path.
        /// </summary>
        /// <param name="path">The path of the folder to retrieve.</param>
        /// <returns>A <see cref="ItemInfo "> object representing the folder at the specified path.</returns>
        Task<ItemInfo> GetFolderAsync(string path);

        /// <summary>
        /// Deletes a file or folder at the specified path.
        /// This method removes the item (file or folder) located at the given path from the archive.
        /// </summary>
        /// <param name="itemPath">The path of the item (file or folder) to be deleted.</param>
        Task DeleteAsync(string itemPath);

        /// <summary>
        /// Checks if the current user has the specified privileges for a given item (file or folder).
        /// This method returns a boolean indicating whether the user has the necessary permissions.
        /// </summary>
        /// <param name="itemPath">The path of the item (file or folder) to check privileges for.</param>
        /// <param name="privilege">The type of privilege (e.g., read, write, delete) to check for.</param>
        /// <returns>True if the user has the specified privilege for the item, otherwise false.</returns>
        Task<bool> HasPrivilegesForItemAsync(string itemPath, PermissionType privilege);

        /// <summary>
        /// Retrieves the child files or folders of a given folder.
        /// This method returns an <see cref="IQueryable"> collection of <see cref="Models.ItemInfo"> objects representing the children of the specified parent folder.
        /// </summary>
        /// <param name="parentFolder">The path of the parent folder whose children are to be retrieved.</param>
        /// <param name="foldersOrFiles">A boolean determining whether to include or exclude files or folders.</param>
        /// <returns>An <see cref="IQueryable"> collection of <see cref="Models.ItemInfo"> objects representing the children of the specified folder.
        /// If filesOnly == true: Folders only
        /// If filesOnly == false: Files only
        /// If filesOnly == null: Both files and folders</returns>
        Task<IQueryable<Models.ItemInfo>> GetChildrenAsync(string parentFolder, bool? foldersOrFiles);
    }
}