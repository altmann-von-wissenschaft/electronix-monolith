namespace Application.Services;

/// <summary>
/// Builds URLs for HTTP calls to modules when <c>ServiceUrls:*</c> may be either an API host root or already scoped to a module path (e.g. <c>.../api/products</c>).
/// </summary>
internal static class InterServiceUrl
{
    /// <param name="baseUrl">Value from configuration (e.g. ServiceUrls:Products).</param>
    /// <param name="prefix">Logical route prefix without leading slash, e.g. <c>api/products</c>.</param>
    /// <param name="suffix">Remainder after prefix, e.g. <c>{id}</c> or <c>{id}/stock</c>.</param>
    public static string Combine(string baseUrl, string prefix, string suffix)
    {
        baseUrl = baseUrl.TrimEnd('/');
        prefix = prefix.Trim('/');
        suffix = suffix.TrimStart('/');

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            return string.IsNullOrEmpty(suffix) ? $"{baseUrl}/{prefix}" : $"{baseUrl}/{prefix}/{suffix}";

        var path = uri.AbsolutePath.TrimEnd('/');
        var prefixPath = "/" + prefix;
        if (path.EndsWith(prefixPath, StringComparison.OrdinalIgnoreCase))
        {
            var authority = uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);
            return string.IsNullOrEmpty(suffix) ? $"{authority}{path}" : $"{authority}{path}/{suffix}";
        }

        return string.IsNullOrEmpty(suffix) ? $"{baseUrl}/{prefix}" : $"{baseUrl}/{prefix}/{suffix}";
    }
}
