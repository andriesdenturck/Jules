using Jules.Access.Archive.Service.Models;
using Jules.Util.Security.Contracts;
using Jules.Util.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Jules.Access.Archive.Service;

public class ArchiveDbContext : DbContext
{
    private readonly IUserContext userContext;

    public ArchiveDbContext(DbContextOptions<ArchiveDbContext> options, IUserContext userContext) : base(options)
    {
        this.userContext = userContext;
    }

    public DbSet<ArchiveItemDb> Items { get; set; }
    public DbSet<FileMetaDataDb> FileInfos { get; set; }
    public DbSet<ItemPermissionDb> ItemPermissions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ArchiveItemDb>()
        .HasIndex(u => u.Path)
        .IsUnique();

        modelBuilder.Entity<ArchiveItemDb>()
                .HasMany(p => p.Children)
                .WithOne(c => c.Parent)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ArchiveItemDb>()
            .HasOne(p => p.FileMetaData)
            .WithOne(c => c.File)
        .HasForeignKey<FileMetaDataDb>(c => c.FileId) // Replace with actual FK property
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ArchiveItemDb>()
                    .HasMany(c => c.Permissions)
                     .WithOne(c => c.Item)
                    .HasForeignKey(c => c.ItemId)
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ArchiveItemDb>()
                .Property(i => i.CreatedOn)
                .ValueGeneratedOnAdd()
                .HasValueGenerator<CustomCreatedOnValueGenerator>();

        modelBuilder.Entity<ItemPermissionDb>()
                .Property(i => i.CreatedOn)
                .ValueGeneratedOnAdd()
                .HasValueGenerator<CustomCreatedOnValueGenerator>();
    }

    public override int SaveChanges()
    {
        HandlePermissionHooks();
        UpdateItemPaths();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        HandlePermissionHooks();
        UpdateItemPaths();
        return await base.SaveChangesAsync(cancellationToken);
    }

    // Auto-add owner permission on new items
    private void HandlePermissionHooks()
    {
        var userId = userContext.UserId;

        var newItems = ChangeTracker.Entries<ArchiveItemDb>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity);

        foreach (var item in newItems)
        {
            item.CreatedBy = userId;

            item.Permissions = new List<ItemPermissionDb>(){ new ItemPermissionDb
            {
                ItemId = item.Id,
                UserId = userId,
                PermissionType = PermissionType.Owner,
                CreatedBy = userId,
            }
            };
        }
    }

    private void UpdateItemPaths()
    {
        foreach (var entry in ChangeTracker.Entries<ArchiveItemDb>())
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                entry.Entity.Path = UriHelper.BuildPath(entry.Entity.Parent?.Path, entry.Entity.Name, entry.Entity.IsFolder).ToString();
            }
        }
    }
}