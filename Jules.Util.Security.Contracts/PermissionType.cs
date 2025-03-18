namespace Jules.Util.Security.Contracts;

[Flags]
public enum PermissionType
{
    None = 0,

    Read = 1 << 0,  // 1
    Write = 1 << 1,  // 2

    CreateFile = 1 << 2,  // 4
    CreateFolder = 1 << 3,  // 8

    Rename = 1 << 4,  // 16
    Delete = 1 << 5,  // 32
    UpdateMetadata = 1 << 6,  // 64
    Move = 1 << 7,  // 128
    SetPermissions = 1 << 8,  // 256

    Owner = ~None
}