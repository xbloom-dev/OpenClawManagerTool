using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace OpenClawManager.Services;

public sealed class GitHubReleaseUpdateService : IUpdateCheckService
{
    private const string LatestReleaseEndpoint = "https://api.github.com/repos/xbloom-dev/OpenClawManagerTool/releases/latest";

    private static readonly HttpClient Http = BuildClient();

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var currentVersion = AppVersionInfo.Number;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseEndpoint);
            using var response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return new UpdateCheckResult(
                    Success: false,
                    UpdateAvailable: false,
                    CurrentVersion: currentVersion,
                    LatestVersion: null,
                    ReleaseUrl: null,
                    ErrorMessage: $"HTTP {(int)response.StatusCode}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            var root = doc.RootElement;
            var tagName = root.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() : null;
            var htmlUrl = root.TryGetProperty("html_url", out var urlProp) ? urlProp.GetString() : null;

            if (string.IsNullOrWhiteSpace(tagName))
            {
                return new UpdateCheckResult(
                    Success: false,
                    UpdateAvailable: false,
                    CurrentVersion: currentVersion,
                    LatestVersion: null,
                    ReleaseUrl: htmlUrl,
                    ErrorMessage: "missing tag_name");
            }

            if (!TryParseVersionCore(currentVersion, out var current))
            {
                return new UpdateCheckResult(
                    Success: false,
                    UpdateAvailable: false,
                    CurrentVersion: currentVersion,
                    LatestVersion: tagName,
                    ReleaseUrl: htmlUrl,
                    ErrorMessage: "invalid current version");
            }

            if (!TryParseVersionCore(tagName, out var latest))
            {
                return new UpdateCheckResult(
                    Success: false,
                    UpdateAvailable: false,
                    CurrentVersion: currentVersion,
                    LatestVersion: tagName,
                    ReleaseUrl: htmlUrl,
                    ErrorMessage: "invalid latest tag");
            }

            return new UpdateCheckResult(
                Success: true,
                UpdateAvailable: latest > current,
                CurrentVersion: currentVersion,
                LatestVersion: tagName,
                ReleaseUrl: htmlUrl,
                ErrorMessage: null);
        }
        catch (OperationCanceledException)
        {
            return new UpdateCheckResult(
                Success: false,
                UpdateAvailable: false,
                CurrentVersion: currentVersion,
                LatestVersion: null,
                ReleaseUrl: null,
                ErrorMessage: "timeout/canceled");
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult(
                Success: false,
                UpdateAvailable: false,
                CurrentVersion: currentVersion,
                LatestVersion: null,
                ReleaseUrl: null,
                ErrorMessage: ex.Message);
        }
    }

    private static HttpClient BuildClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("OpenClawManagerTool", AppVersionInfo.Number));
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        return client;
    }

    private static bool TryParseVersionCore(string raw, out Version version)
    {
        version = new Version(0, 0, 0);
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var normalized = raw.Trim();
        if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[1..];

        var plusIndex = normalized.IndexOf('+');
        if (plusIndex >= 0)
            normalized = normalized[..plusIndex];

        var dashIndex = normalized.IndexOf('-');
        if (dashIndex >= 0)
            normalized = normalized[..dashIndex];

        normalized = normalized.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return false;

        if (Version.TryParse(normalized, out var parsed) && parsed != null)
        {
            version = parsed;
            return true;
        }

        var parts = normalized.Split('.');
        if (parts.Length == 2
            && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var major)
            && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minor))
        {
            version = new Version(major, minor, 0);
            return true;
        }

        return false;
    }
}
