namespace Jules.Util.Shared
{
    public static class UriHelper
    {
        public static Uri GetUri(string path)
        {
            if (!Uri.TryCreate(path, UriKind.Absolute, out Uri? uri))
            {
                if (Uri.TryCreate(path, UriKind.Relative, out Uri? _))
                {
                    return GetUri("file:///" + path);
                }
                throw new Exception("Invalid URI.");
            }

            if (uri.Scheme != Uri.UriSchemeFile)
            {
                throw new Exception("Invalid URI.");
            }

            return uri;
        }
    }
}