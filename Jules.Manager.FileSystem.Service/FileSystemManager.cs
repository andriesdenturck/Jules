using AutoMapper;
using Microsoft.Extensions.Logging;
using Jules.Access.Archive.Contracts;
using Jules.Access.Blob.Contracts;
using Jules.Access.Blob.Contracts.Models;
using Jules.Access.Blob.Service;
using Jules.Engine.Parsing.Contracts;
using Jules.Manager.FileSystem.Contracts;
using Jules.Manager.FileSystem.Contracts.Models;
using Jules.Util.Security.Contracts;
using Jules.Util.Shared;
using ItemInfo = Jules.Access.Archive.Contracts.Models.ItemInfo;
using Jules.Engine.Parsing.Contracts.Models;

namespace Jules.Manager.FileSystem.Service;


    /// <summary>
    /// Service implementation of <see cref="IFileSystemManager"/> that handles file and folder operations
    /// including creation, deletion, listing, downloading, and content searching. 
    /// Relies on archive access, blob storage, and parsing engine services.
    /// </summary>
    public class FileSystemManager : ServiceBase<FileSystemManager>, IFileSystemManager
    {

    private readonly IArchiveAccess archiveAccess;
    private readonly IBlobAccess blobAccess;
    private readonly IParsingEngine parsingEngine;
    private readonly IMapper mapper;


    /// <summary>
    /// Creates an instance of the <see cref="FileSystemManager"/> class with required dependencies.
    /// </summary>
    /// <param name="userContext">Context of the current user for permission checks.</param>
    /// <param name="archiveAccess">Access service for archive-based metadata operations.</param>
    /// <param name="blobAccess">Blob access service for raw file storage and retrieval.</param>
    /// <param name="parsingEngine">Engine for searching and compressing files.</param>
    /// <param name="logger">Logger instance.</param>
    public FileSystemManager(IUserContext userContext, IArchiveAccess archiveAccess, IBlobAccess blobAccess, IParsingEngine parsingEngine, ILogger<FileSystemManager> logger)
            : base(userContext, logger)
        {
        this.archiveAccess = archiveAccess;
        this.blobAccess = blobAccess;
        this.parsingEngine = parsingEngine;

        MapperConfiguration configBlob = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new FileSystemMappingProfile());
        });

        this.mapper = new Mapper(configBlob);
    }

    /// <inheritdoc />
    public async Task<Item> CreateFileAsync(string folderPath, FileContent fileContent)
    {
        if (!await archiveAccess.HasPrivilegesForItemAsync(folderPath, PermissionType.CreateFile))
        {
            throw new UnauthorizedAccessException();
        }

        Blob file = MapToBlob(fileContent);
        var token = await blobAccess.CreateAsync(file);

        ItemInfo itemInfo = MapToFileInfo(fileContent);
        itemInfo.TokenId = token;
        itemInfo.Path = UriHelper.BuildPath(folderPath, fileContent.FileName,false).AbsoluteUri;

        var newItemInfo = await archiveAccess.CreateFileAsync(itemInfo);

        return MapToItem(newItemInfo);
    }

    /// <inheritdoc />
    public async Task<Item> CreateFolderAsync(string folderPath)
    {
        var ItemInfo = await archiveAccess.CreateFolderAsync(folderPath);
        return MapToItem(ItemInfo);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string path)
    {
        if (!await archiveAccess.HasPrivilegesForItemAsync(path, PermissionType.Delete))
        {
            throw new UnauthorizedAccessException();
        }

        var childrenTokens = (await archiveAccess.GetChildrenAsync(path, false)).Select(info => info.TokenId).ToArray();

        await archiveAccess.DeleteAsync(path);
        await blobAccess.BulkDeleteAsync(childrenTokens);

        return true;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Item>> FindInFilesAsync(string folderPath, string searchString, int number = 10)
    {
        var children = await archiveAccess.GetChildrenAsync(folderPath, false);
        var results = new List<Item>();

        // Process children asynchronously one by one
        foreach (var itemInfo in children)
        {
            // Check if we have enough results
            if (results.Count >= number)
            {
                break;
            }

            // Wait for the parsing engine to find matches
            var found = await parsingEngine.FindInFileAsync(itemInfo.TokenId, searchString);

            if (found)
            {
                // If a match is found, map and add to results
                results.Add(MapToItem(itemInfo));
            }
        };

        return results.ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Item>> ListItemsAsync(string folderPath)
    {
        var children = await archiveAccess.GetChildrenAsync(folderPath, null);
        return children.Select(this.MapToItem).OrderBy(it => it.Path).ToList();
    }

    /// <inheritdoc />
    public async Task<FileContent> DownloadAsync(string path)
    {
        if (!await archiveAccess.HasPrivilegesForItemAsync(path, PermissionType.Read))
        {
            throw new UnauthorizedAccessException();
        }

        var children = await archiveAccess.GetChildrenAsync(path, false);

        if (children.Count() == 0)
        {
            throw new FileNotFoundException("Path is not valid");
        }
        else if (children.Count() == 1)
        {
            var blob = await blobAccess.ReadAsync(children.Single().TokenId);
            return MapToFileContent(blob);
        }
        else
        {
            // Compression Logic
            var folderStructure = children.Select(MapToFileItem).AsEnumerable();
            var compressed = await this.parsingEngine.CompressAsync(folderStructure);

            return MapToFileContent(compressed);
        }
    }

    private FileItem MapToFileItem(ItemInfo info) => this.mapper.Map<FileItem>(info);

    private Item MapToItem(ItemInfo info) => this.mapper.Map<Item>(info);

    private ItemInfo MapToFileInfo(FileContent fileContent) => this.mapper.Map<ItemInfo>(fileContent);

    private Blob MapToBlob(FileContent fileContent) => this.mapper.Map<Blob>(fileContent);

    private FileContent MapToFileContent(FileItem item) => this.mapper.Map<FileContent>(item);

    private FileContent MapToFileContent(Blob blob) => this.mapper.Map<FileContent>(blob);
}