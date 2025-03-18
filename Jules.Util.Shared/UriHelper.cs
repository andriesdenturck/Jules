namespace Jules.Util.Shared;

public static class UriHelper
{
    public static Uri GetUri(string path)
    {
        if (!Uri.TryCreate(path, UriKind.Absolute, out Uri? uri))
        {
            if (Uri.TryCreate(path, UriKind.Relative, out Uri? _))
            {
                return GetUri("file:///" + path.TrimStart('/'));
            }
            throw new Exception("Invalid URI.");
        }

        if (uri.Scheme != Uri.UriSchemeFile)
        {
            throw new Exception("Invalid URI.");
        }

        return uri;
    }

    public static Uri BuildPath(string? parentPath, string itemName, bool isFolder)
    {
        if (string.IsNullOrWhiteSpace(parentPath))
        {
            parentPath = $"file:///";
        }
        else
        {
            parentPath = parentPath.Trim('/') + "/";
        }

        var parentUri = GetUri(parentPath);

        // Sanitize name
        var safeName = isFolder && !string.IsNullOrEmpty(itemName) && !itemName.EndsWith("/") ? $"{itemName}/" : itemName;

        return new Uri(parentUri, safeName);
    }
}