using AutoMapper;
using Jules.Access.Archive.Contracts;
using Jules.Access.Archive.Contracts.Models;
using Jules.Access.Blob.Contracts;
using Jules.Access.Blob.Contracts.Models;
using Jules.Engine.Parsing.Contracts;
using Jules.Engine.Parsing.Contracts.Models;
using Jules.Manager.FileSystem.Contracts;
using Jules.Manager.FileSystem.Contracts.Models;
using Jules.Util.Security.Contracts;
using Jules.Util.Shared;
using Microsoft.Extensions.Logging;

namespace Jules.Manager.FileSystem.Service;

public class FileSystemManager : ServiceBase<FileSystemManager>, IFileSystemManager
{
    private readonly IArchiveAccess archiveAccess;
    private readonly IBlobAccess blobAccess;
    private readonly IParsingEngine parsingEngine;
    private readonly IMapper mapper;

    public FileSystemManager(IArchiveAccess archiveAccess, IBlobAccess blobAccess, IParsingEngine parsingEngine, ILogger<FileSystemManager> logger)
        : base(logger)
    {
        this.archiveAccess = archiveAccess;
        this.blobAccess = blobAccess;
        this.parsingEngine = parsingEngine;

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new FileSystemMappingProfile());
        });

        this.mapper = new Mapper(config);
    }

    public async Task<Item> CreateFileAsync(string folderPath, FileContent fileContent)
    {
        try
        {
            if (!await archiveAccess.HasPrivilegesForItemAsync(folderPath, PermissionType.CreateFile))
            {
                throw new UnauthorizedAccessException("Insufficient privileges to create file.");
            }

            var file = MapToBlob(fileContent);
            var token = await blobAccess.CreateAsync(file);

            var itemInfo = MapToFileInfo(fileContent);
            itemInfo.TokenId = token;
            itemInfo.Path = UriHelper.BuildPath(folderPath, fileContent.FileName, false).AbsoluteUri;

            var newItemInfo = await archiveAccess.CreateFileAsync(itemInfo);
            return MapToItem(newItemInfo);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create file in folder {folderPath}", folderPath);
            throw;
        }
    }

    public async Task<Item> CreateFolderAsync(string folderPath)
    {
        try
        {
            var itemInfo = await archiveAccess.CreateFolderAsync(folderPath);
            return MapToItem(itemInfo);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create folder at path {folderPath}", folderPath);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string path)
    {
        try
        {
            if (!await archiveAccess.HasPrivilegesForItemAsync(path, PermissionType.Delete))
            {
                throw new UnauthorizedAccessException("Insufficient privileges to delete.");
            }

            var childrenTokens = (await archiveAccess.GetChildrenAsync(path, false)).Select(info => info.TokenId).ToArray();

            await archiveAccess.DeleteAsync(path);
            await blobAccess.BulkDeleteAsync(childrenTokens);

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete path {path}", path);
            throw;
        }
    }

    public async Task<IEnumerable<Item>> FindInFilesAsync(string folderPath, string searchString, int number = 10)
    {
        try
        {
            var children = await archiveAccess.GetChildrenAsync(folderPath, false);
            var results = new List<Item>();

            foreach (var itemInfo in children)
            {
                if (results.Count >= number)
                    break;

                var found = await parsingEngine.FindInFileAsync(itemInfo.TokenId, searchString);
                if (found)
                    results.Add(MapToItem(itemInfo));
            }

            return results;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to search in files at {folderPath} with search string '{searchString}'", folderPath, searchString);
            throw;
        }
    }

    public async Task<IEnumerable<Item>> ListItemsAsync(string folderPath)
    {
        try
        {
            var children = await archiveAccess.GetChildrenAsync(folderPath, null);
            return children.Select(MapToItem).OrderBy(it => it.Path).ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to list items at {folderPath}", folderPath);
            throw;
        }
    }

    public async Task<FileContent> DownloadAsync(string path)
    {
        try
        {
            if (!await archiveAccess.HasPrivilegesForItemAsync(path, PermissionType.Read))
                throw new UnauthorizedAccessException("Insufficient privileges to download.");

            var children = await archiveAccess.GetChildrenAsync(path, false);

            if (!children.Any())
            {
                throw new FileNotFoundException("No file or folder found at the given path.");
            }

            if (children.Count() == 1)
            {
                var blob = await blobAccess.ReadAsync(children.Single().TokenId);
                return MapToFileContent(blob);
            }
            else
            {
                var folderStructure = children.Select(MapToFileItem);
                var compressed = await parsingEngine.CompressAsync(folderStructure);
                return MapToFileContent(compressed);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to download content at {path}", path);
            throw;
        }
    }

    // Mapping Helpers
    private FileItem MapToFileItem(ItemInfo info) => mapper.Map<FileItem>(info);

    private Item MapToItem(ItemInfo info) => mapper.Map<Item>(info);

    private ItemInfo MapToFileInfo(FileContent fileContent) => mapper.Map<ItemInfo>(fileContent);

    private Blob MapToBlob(FileContent fileContent) => mapper.Map<Blob>(fileContent);

    private FileContent MapToFileContent(FileItem item) => mapper.Map<FileContent>(item);

    private FileContent MapToFileContent(Blob blob) => mapper.Map<FileContent>(blob);
}