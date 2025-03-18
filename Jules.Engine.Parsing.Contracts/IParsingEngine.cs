using Jules.Engine.Parsing.Contracts.Models;

namespace Jules.Engine.Parsing.Contracts
{
    /// <summary>
    /// Defines a parsing engine capable of searching for specific content within files and compressing file data.
    /// </summary>
    public interface IParsingEngine
    {
        /// <summary>
        /// Searches for the specified string within the file identified by the given token ID.
        /// </summary>
        /// <param name="tokenId">An encrypted identifier representing the file to be searched. The token is used to locate the file in the system.</param>
        /// <param name="searchString">The string to search for within the file's contents. This is the target text to find within the file.</param>
        /// <returns><c>true</c> if the file starts with the given string; otherwise, <c>false</c>. Returns <c>true</c> when the string is found, indicating a successful search.</returns>
        Task<bool> FindInFileAsync(string tokenId, string searchString);

        /// <summary>
        /// Compresses the provided collection of file items into a single compressed byte array.
        /// </summary>
        /// <param name="fileItems">A collection of file items to be compressed. Each item in the collection represents a file or data to be compressed.</param>
        /// <returns>An item containing the compressed data representing the provided file items. The result is a compressed stream that can be stored or transmitted.</returns>
        Task<FileItem> CompressAsync(IEnumerable<FileItem> fileItems);
    }
}