using AutoMapper;
using AutoMapper.QueryableExtensions;
using Jules.Access.Archive.Contracts;
using Jules.Access.Archive.Service.Models;
using Jules.Util.Security.Contracts;
using Jules.Util.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ItemInfo = Jules.Access.Archive.Contracts.Models.ItemInfo;

namespace Jules.Access.Archive.Service;

/// <summary>
/// Provides access to archive metadata and file structure stored in the database.
/// Implements <see cref="IArchiveAccess"/> and handles creation, deletion, and permission checks
/// on files and folders stored in the archive.
/// </summary>
public class ArchiveAccess : ServiceBase<ArchiveAccess>, IArchiveAccess
{
    private readonly ArchiveDbContext dbContext;
    private readonly IMapper mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveAccess"/> class.
    /// Configures the AutoMapper with the <see cref="ArchiveMappingProfile"/> and sets up dependencies.
    /// </summary>
    /// <param name="context">The Entity Framework database context for archive data.</param>
    /// <param name="userContext">Provides information about the current user, used for permission checks.</param>
    /// <param name="logger">Logger instance for logging operations.</param>
    public ArchiveAccess(ArchiveDbContext context, IUserContext userContext, ILogger<ArchiveAccess> logger) : base(userContext, logger)
    {
        this.dbContext = context;

        MapperConfiguration configBlob = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new ArchiveMappingProfile());
        });

        this.mapper = new Mapper(configBlob);
    }

    /// <inheritdoc />
    public async Task<ItemInfo> CreateFileAsync(ItemInfo fileInfo)
    {
        var fileUri = UriHelper.GetUri(fileInfo.Path);

        if (dbContext.Items.Any(item => item.Path == fileUri.AbsoluteUri))
        {
            throw new Exception();
        }

        var parent = await GetParent(fileUri);

        if (!await HasPrivilegesForItemInternal(parent, PermissionType.CreateFile))
        {
            throw new UnauthorizedAccessException();
        }

        var newFileInfoDb = MapToFileInfoDb(fileInfo);

        var newItem = new ArchiveItemDb
        {
            IsFolder = false,
            Name = fileInfo.Name,
            Parent = parent,
            FileInfo = newFileInfoDb,
        };

        dbContext.Items.Add(newItem);
        await dbContext.SaveChangesAsync();

        return MapToItemInfo(newItem);
    }

    /// <inheritdoc />
    public async Task<ItemInfo> CreateFolderAsync(string path)
    {
        if (!path.EndsWith("/"))
        {
            path += "/";
        }
        var folderUri = UriHelper.GetUri(path);
        var existingFolder = await dbContext.Items.SingleOrDefaultAsync(item => item.Path == folderUri.AbsoluteUri);

        if (existingFolder != null)
        {
            return MapToItemInfo(existingFolder);
        }

        var parent = await GetParent(folderUri);

        var newItem = new ArchiveItemDb
        {
            IsFolder = true,
            Parent = parent,
            Name = folderUri.Segments.Last().Trim('/'),
        };

        dbContext.Items.Add(newItem);
        await dbContext.SaveChangesAsync();

        return MapToItemInfo(newItem);
    }

    /// <inheritdoc />
    private async Task<ArchiveItemDb> GetParent(Uri fileUri)
    {
        var parentUri = fileUri.AbsoluteUri.EndsWith("/") ? new Uri(fileUri, "..") : new Uri(fileUri, ".");

        ArchiveItemDb parent = await GetItemByPath(parentUri.AbsoluteUri);

        if (parent != null)
        {
            return parent;
        }
        else
        {
            if (parentUri.LocalPath == "/")
            {
                if (fileUri.Segments.Last().Trim('/') == userContext.UserName)
                {
                    return null;
                }

                throw new Exception();
            }

            var grandParent = await GetParent(parentUri);

            if (grandParent != null && dbContext.Entry(grandParent).State != EntityState.Added && !await HasPrivilegesForItemInternal(grandParent, PermissionType.CreateFolder))
            {
                throw new UnauthorizedAccessException();
            }

            parent = new ArchiveItemDb
            {
                Name = parentUri.Segments.Last().Trim('/'),
                Parent = grandParent,
                IsFolder = true, // assume only last is file, others are folders
            };

            dbContext.Items.Add(parent);
            return parent;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string itemPath)
    {
        ArchiveItemDb item = await GetItemByPath(itemPath);

        if (!await HasPrivilegesForItemInternal(item, PermissionType.Delete))
        {
            throw new UnauthorizedAccessException();
        }

        dbContext.Items.Remove(item);
        dbContext.SaveChanges();
    }

    /// <inheritdoc />
    public async Task<IQueryable<ItemInfo>> GetChildrenAsync(string parentFolder, bool? foldersOrFiles)
    {
        var uri = UriHelper.GetUri(parentFolder);



        IQueryable<ArchiveItemDb> itemsQuery = !foldersOrFiles.HasValue ? dbContext.Items.Include(c => c.FileInfo) : dbContext.Items.Include(c => c.FileInfo).Where(it => it.IsFolder == foldersOrFiles);

        var isAdmin = await this.userContext.IsInRole("admin");

        if (isAdmin)
        {
            return itemsQuery.Where(i => i.Path.StartsWith(uri.AbsoluteUri)).ProjectTo<ItemInfo>(this.mapper.ConfigurationProvider);
        }

        Guid userId = this.userContext.UserId;
        var permissionQuery = dbContext.ItemPermissions;
        // Step 1: Build the paths the user has access to based on permissions (no ToList)
        return dbContext.ItemPermissions
            .Where(p => p.UserId == userId && p.PermissionType.HasFlag(PermissionType.Read))
            .Join(itemsQuery, p => p.ItemId, i => i.Id, (p, i) => i)
            .Where(i => i.Path.StartsWith(uri.AbsoluteUri))
            .ProjectTo<ItemInfo>(this.mapper.ConfigurationProvider); // Get paths the user can access
    }

    /// <inheritdoc />
    public async Task<bool> HasPrivilegesForItemAsync(string itemPath, PermissionType privilege)
    {
        var itemDb = await GetItemByPath(itemPath);

        if (itemDb == null)
        {
            throw new KeyNotFoundException("File or folder not found");
        }

        return await HasPrivilegesForItemInternal(itemDb, privilege);
    }

    /// <inheritdoc />
    public async Task<ItemInfo> GetFileAsync(string path)
    {
        var item = await GetItemByPath(path);

        if (item == null)
        {
            throw new KeyNotFoundException("File not found.");
        }
        if (item.IsFolder)
        {
            throw new InvalidOperationException("Given path is not a file.");
        }
        if (!await HasPrivilegesForItemInternal(item, PermissionType.Read))
        {
            throw new UnauthorizedAccessException();
        }

        return MapToItemInfo(item);
    }

    public async Task<ItemInfo> GetFolderAsync(string path)
    {
        var item = await GetItemByPath(path);

        if (item == null)
        {
            throw new KeyNotFoundException("Folder not found.");
        }
        if (!item.IsFolder)
        {
            throw new InvalidOperationException("Given path is not a folder.");
        }
        if (!await HasPrivilegesForItemInternal(item, PermissionType.Read))
        {
            throw new UnauthorizedAccessException();
        }

        return MapToItemInfo(item);
    }

    private async Task<ArchiveItemDb?> GetItemByPath(string itemPath) => await dbContext.Items.SingleOrDefaultAsync(p => p.Path == UriHelper.GetUri(itemPath).AbsoluteUri);

    private async Task<bool> HasPrivilegesForItemInternal(ArchiveItemDb item, PermissionType privilege)
    {
        if (await userContext.IsInRole("admin"))
        {
            return true;
        }

        if (dbContext.Entry(item).State == EntityState.Added)
        {
            return true;
        }

        Guid userId = this.userContext.UserId;

        return await dbContext.ItemPermissions
            .Where(p => p.UserId == userId && p.PermissionType.HasFlag(privilege) && p.ItemId == item.Id)
            .AnyAsync();
    }

    private FileInfoDb MapToFileInfoDb(ItemInfo fileInfo) => this.mapper.Map<FileInfoDb>(fileInfo);

    private ItemInfo MapToItemInfo(ArchiveItemDb fileItem) => this.mapper.Map<ItemInfo>(fileItem);
}