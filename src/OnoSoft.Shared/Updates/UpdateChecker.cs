using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace OnoSoft.Shared.Updates;

public record UpdateInfo(string LatestVersion, string ReleaseUrl);

/// <summary>
/// Checks a GitHub repo's "latest" release against the running app's version.
/// Every OnoSoft app can call this once at startup (or from the tray menu) to
/// let users know a newer build exists — no auto-download, just a link out to
/// the GitHub Release / distribution page.
/// </summary>
public class UpdateChecker
{
    private readonly HttpClient _http;
    private readonly string _owner;
    private readonly string _repo;

    /// <param name="owner">GitHub account/organization, e.g. "onodera888".</param>
    /// <param name="repo">Repository name for this specific app.</param>
    /// <param name="userAgent">Required by the GitHub API; use the app's name.</param>
    public UpdateChecker(string owner, string repo, string userAgent)
    {
        _owner = owner;
        _repo = repo;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(SanitizeToken(userAgent), "1.0"));
        _http.Timeout = TimeSpan.FromSeconds(5);
    }

    /// <summary>Returns update info if the latest GitHub release is newer than <paramref name="currentVersion"/>, otherwise null.</summary>
    public async Task<UpdateInfo?> CheckAsync(Version currentVersion)
    {
        try
        {
            var url = $"https://api.github.com/repos/{_owner}/{_repo}/releases/latest";
            using var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            var tagName = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
            var htmlUrl = doc.RootElement.GetProperty("html_url").GetString() ?? "";

            var versionText = tagName.TrimStart('v', 'V');
            if (!Version.TryParse(versionText, out var latestVersion)) return null;

            return latestVersion > currentVersion ? new UpdateInfo(versionText, htmlUrl) : null;
        }
        catch (Exception)
        {
            // Offline, rate-limited, or no releases yet — silently skip the check.
            return null;
        }
    }

    private static string SanitizeToken(string value) =>
        string.IsNullOrWhiteSpace(value) ? "OnoSoftApp" : value.Replace(' ', '-');
}
