using Jules.Util.Security.Contracts;

namespace Jules.Access.Archive.Service.Models;

public class ItemPermissionDb
{
    public Guid Id { get; set; }

    public Guid ItemId { get; set; }

    public ArchiveItemDb Item { get; set; }

    public Guid? UserId { get; set; }

    public PermissionType PermissionType { get; set; }

    public DateTimeOffset CreatedOn { get; set; }

    public Guid CreatedBy { get; set; }
}