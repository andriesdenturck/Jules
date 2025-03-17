namespace Jules.Access.Archive.Service.Models;

public class ArchiveItemDb
{
    public Guid Id { get; set; }

    public string? Path { get; set; }

    public string Name { get; set; }

    public bool IsFolder { get; set; }
    public Guid? ParentId { get; set; }
    public ArchiveItemDb Parent { get; set; }
    public Guid? FileInfoId { get; set; }
    public FileInfoDb FileInfo { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }

    // Navigation property for child items (files/folders inside this folder)
    public ICollection<ArchiveItemDb> Children { get; set; }

    public ICollection<ItemPermissionDb> Permissions { get; set; }
}