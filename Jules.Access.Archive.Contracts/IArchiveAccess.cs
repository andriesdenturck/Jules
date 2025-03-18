using Jules.Access.Archive.Contracts.Models;
using Jules.Util.Security.Contracts;

namespace Jules.Access.Archive.Contracts
{
    /// <summary>
    /// Defines operations for accessing and managing files and folders.
    /// Provides methods to create, retrieve, delete, and check privileges for files and folders.
    /// </summary>
    public interface IArchiveAccess
    {
        /// <summary>
        /// Creates a new file in the archive with the provided metadata.
        /// </summary>
        /// <param name="fileInfo">An <see cref="ItemInfo"/> object representing the file to be created.</param>
        /// <returns>An <see cref="ItemInfo"/> object representing the created file.</returns>
        Task<ItemInfo> CreateFileAsync(ItemInfo fileInfo);

        /// <summary>
        /// Creates a new folder at the specified path.
        /// </summary>
        /// <param name="folderPath">The path of the folder to be created.</param>
        /// <returns>An <see cref="ItemInfo"/> object representing the created folder.</returns>
        Task<ItemInfo> CreateFolderAsync(string folderPath);

        /// <summary>
        /// Retrieves metadata for a file based on the provided path.
        /// </summary>
        /// <param name="filepath">The path of the file to retrieve.</param>
        /// <returns>An <see cref="ItemInfo"/> object representing the file.</returns>
        Task<ItemInfo> GetFileAsync(string filepath);

        /// <summary>
        /// Retrieves metadata for a folder based on the provided path.
        /// </summary>
        /// <param name="folderPath">The path of the folder to retrieve.</param>
        /// <returns>An <see cref="ItemInfo"/> object representing the folder.</returns>
        Task<ItemInfo> GetFolderAsync(string folderPath);

        /// <summary>
        /// Deletes a file or folder at the specified path.
        /// </summary>
        /// <param name="itemPath">The path of the item (file or folder) to be deleted.</param>
        Task DeleteAsync(string itemPath);

        /// <summary>
        /// Checks if the current user has the specified privileges for a given item (file or folder).
        /// </summary>
        /// <param name="itemPath">The path of the item to check.</param>
        /// <param name="privilege">The type of privilege (e.g., read, write, delete) to check for.</param>
        /// <returns><c>true</c> if the user has the specified privilege; otherwise, <c>false</c>.</returns>
        Task<bool> HasPrivilegesForItemAsync(string itemPath, PermissionType privilege);

        /// <summary>
        /// Retrieves the child files and/or folders of a given folder.
        /// </summary>
        /// <param name="parentFolder">The path of the parent folder.</param>
        /// <param name="foldersOrFiles">
        /// Determines what to include:
        /// <list type="bullet">
        /// <item><description><c>true</c>: Return folders only</description></item>
        /// <item><description><c>false</c>: Return files only</description></item>
        /// <item><description><c>null</c>: Return both files and folders</description></item>
        /// </list>
        /// </param>
        /// <returns>An <see cref="IQueryable{ItemInfo}"/> representing the children of the folder.</returns>
        Task<IQueryable<ItemInfo>> GetChildrenAsync(string parentFolder, bool? foldersOrFiles);
    }
}
