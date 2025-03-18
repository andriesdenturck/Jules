using Jules.Access.Blob.Contracts;
using Jules.Engine.Parsing.Contracts;
using Jules.Engine.Parsing.Contracts.Models;
using Jules.Util.Security.Contracts;
using Jules.Util.Shared;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Text;

namespace Jules.Engine.Parsing.Service;

/// <summary>
/// Implementation of <see cref="IParsingEngine"/> that parses supported file types for content matching and compresses file data.
/// </summary>
public partial class ParsingEngine : ServiceBase<ParsingEngine>, IParsingEngine
{
    private readonly IBlobAccess blobAccess;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParsingEngine"/> class.
    /// </summary>
    /// <param name="blobAccess">Service to retrieve blob content from storage.</param>
    /// <param name="logger">Logger instance for logging events in the service.</param>
    public ParsingEngine(IBlobAccess blobAccess, ILogger<ParsingEngine> logger)
        : base(logger)
    {
        this.blobAccess = blobAccess;
    }

    /// <inheritdoc />
    public async Task<bool> FindInFileAsync(string tokenId, string searchString)
    {
        try
        {
            if (string.IsNullOrEmpty(tokenId))
            {
                Logger.LogWarning("Token ID is null or empty.");
                return false;
            }

            var blob = await this.blobAccess.ReadAsync(tokenId);

            if (blob?.Data == null || blob.Data.Length == 0)
            {
                Logger.LogWarning("Blob data is empty or null for tokenId: {TokenId}", tokenId);
                return false;
            }

            string mimeType = blob.MimeType;

            // Handle supported MIME types for content searching
            switch (mimeType.ToLowerInvariant())
            {
                case "text/plain":
                case "application/json":
                case "application/xml":
                case "text/xml":
                case "text/html":
                case "application/javascript":
                case "application/x-www-form-urlencoded":
                case "application/x-yaml":
                case "text/csv":
                    // Convert byte data to UTF-8 string and search for the substring
                    string utf8String = Encoding.UTF8.GetString(blob.Data);
                    return utf8String.StartsWith(searchString, StringComparison.OrdinalIgnoreCase);

                default:
                    // Log unsupported MIME type and throw exception
                    Logger.LogError("Unsupported MIME type '{MimeType}' for tokenId: {TokenId}", mimeType, tokenId);
                    throw new NotSupportedException($"MIME type '{mimeType}' is not supported for string reading.");
            }
        }
        catch (NotSupportedException)
        {
            // Rethrow unsupported MIME type exceptions
            throw;
        }
        catch (Exception ex)
        {
            // Log any other exceptions and rethrow as an InvalidOperationException
            Logger.LogError(ex, "An error occurred while parsing blob for tokenId: {TokenId}", tokenId);
            throw new InvalidOperationException("An error occurred while attempting to parse the file.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<FileItem> CompressAsync(IEnumerable<FileItem> fileItems)
    {
        using (var memoryStream = new MemoryStream())
        {
            using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                // Add files to the zip archive
                foreach (var item in fileItems)
                {
                    var blob = await this.blobAccess.ReadAsync(item.TokenId);

                    if (blob?.Data == null || blob.Data.Length == 0)
                    {
                        Logger.LogWarning("Blob data is empty for file {FileName} in folder {FolderName}. Skipping.", item.TokenId, item.FileName);
                        continue; // Skip files with no data
                    }

                    // Create an entry for the file in the zip archive, maintaining the folder structure
                    var entryName = Path.Combine(item.FileName, blob.FileName);
                    var entry = zipArchive.CreateEntry(entryName);

                    using (var entryStream = entry.Open())
                    using (var fileStream = new MemoryStream(blob.Data))
                    {
                        await fileStream.CopyToAsync(entryStream); // Copy the file data into the zip entry
                    }
                }
            }
            return new FileItem
            {
                Data = memoryStream.ToArray(),
                MimeType = "application/zip",
                FileName = "Compressed.zip"
            };
        }
    }
}