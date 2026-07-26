using Microsoft.Extensions.Configuration;

namespace HomeAutomation.Core.Extensions;

internal static class ConfigurationExtensions
{
    public static Uri GetRequiredHttpBaseUri(this IConfiguration configuration, string key)
    {
        string? configuredValue = configuration[key];
        if (!Uri.TryCreate(configuredValue, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException($"Configuration value '{key}' must be an absolute HTTP or HTTPS URL.");
        }

        return uri.AbsoluteUri.EndsWith('/')
            ? uri
            : new Uri($"{uri.AbsoluteUri}/", UriKind.Absolute);
    }
}
