namespace Jules.Access.Blob.Contracts;

/// <summary>
/// Provides methods for managing blob data including creation, retrieval, and deletion.
/// </summary>
public interface IBlobAccess
{
    /// <summary>
    /// Stores a new blob and returns an encrypted token identifier for future access.
    /// </summary>
    /// <param name="blob">The blob data to store.</param>
    /// <returns>An encrypted token representing the blob's identifier.</returns>
    Task<string> CreateAsync(Models.Blob blob);

    /// <summary>
    /// Retrieves a blob by its encrypted token identifier.
    /// </summary>
    /// <param name="token">The encrypted token corresponding to the blob.</param>
    /// <returns>The matching blob object.</returns>
    Task<Models.Blob> ReadAsync(string token);

    /// <summary>
    /// Updates an existing blob with new data.
    /// </summary>
    /// <param name="file">The updated blob data to store, including any necessary modifications.</param>
    /// <returns>
    /// An encrypted token that represents the updated blob's identifier.
    /// </returns>
    Task<string> UpdateAsync(Models.Blob file);

    /// <summary>
    /// Deletes a blob based on its encrypted token identifier.
    /// </summary>
    /// <param name="token">The encrypted token of the blob to delete.</param>
    Task DeleteAsync(string token);

    /// <summary>
    /// Deletes multiple blobs based on a collection of encrypted token identifiers.
    /// </summary>
    /// <param name="token">A collection of encrypted tokens representing blobs to delete.</param>
    Task BulkDeleteAsync(IEnumerable<string> token);
}