namespace Jules.Api.FileSystem.Models;

public class Item
{
    public string? Path { get; internal set; }
    public bool IsFolder { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
}