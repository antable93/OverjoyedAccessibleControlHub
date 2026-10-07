using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OverjoyedVersion3;

internal static class EyeTrackingAssets
{
    public static async Task<string> ExtractAsync()
    {
        using var manifestStream = await FileSystem.OpenAppPackageFileAsync("EyeTracking/asset-manifest.json");
        using var reader = new StreamReader(manifestStream);
        var json = await reader.ReadToEndAsync();
        var assets = JsonSerializer.Deserialize<List<Asset>>(json)
            ?? throw new InvalidDataException("The eye-tracking asset manifest is empty.");
        if (assets.Count == 0 || !assets.Any(asset => asset.Path == "index.html"))
            throw new InvalidDataException("The eye-tracking asset manifest has no entry page.");

        var version = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        var directory = Path.Combine(FileSystem.CacheDirectory, "EyeTracking", version);
        Directory.CreateDirectory(directory);
        foreach (var asset in assets)
        {
            if (string.IsNullOrWhiteSpace(asset.Path) ||
                asset.Path.Split('/').Any(part => part is "" or "." or "..") ||
                asset.Path.Contains('\\') || asset.Path.Contains(':'))
                throw new InvalidDataException($"Invalid eye-tracking asset path: {asset.Path}.");

            var destination = Path.Combine(directory, asset.Path.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(destination))
            {
                using var existing = File.OpenRead(destination);
                var hash = Convert.ToHexString(await SHA256.HashDataAsync(existing));
                if (hash.Equals(asset.Sha256, StringComparison.OrdinalIgnoreCase)) continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            using var source = await FileSystem.OpenAppPackageFileAsync($"EyeTracking/{asset.Path}");
            using (var output = File.Create(destination))
                await source.CopyToAsync(output);

            using var copied = File.OpenRead(destination);
            var copiedHash = Convert.ToHexString(await SHA256.HashDataAsync(copied));
            if (!copiedHash.Equals(asset.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Eye-tracking asset checksum failed: {asset.Path}.");
        }
        return directory;
    }

    private sealed class Asset
    {
        [JsonPropertyName("path")]
        public required string Path { get; init; }

        [JsonPropertyName("sha256")]
        public required string Sha256 { get; init; }
    }
}
