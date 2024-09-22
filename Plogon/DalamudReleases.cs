using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Serilog;

using Tomlyn;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace Plogon;

/// <summary>
/// Dalamud acquisition
/// </summary>
public class DalamudReleases
{
    private const string URL_TEMPLATE = "https://aonyx.ffxiv.wang/Dalamud/Release/VersionInfo?track={0}";

    private readonly Overrides? overrides;

    private class Overrides
    {
        public Dictionary<string, string> ChannelTracks { get; set; } = new();
    }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="overridesFile">File containing overrides.</param>
    /// <param name="releasesDir">Where releases should go</param>
    public DalamudReleases(FileInfo? overridesFile, DirectoryInfo releasesDir)
    {
        this.ReleasesDir = releasesDir;

        if (overridesFile?.Exists ?? false)
            this.overrides = Toml.ToModel<Overrides>(overridesFile.OpenText().ReadToEnd());
    }

    /// <summary>
    /// Where releases go
    /// </summary>
    public DirectoryInfo ReleasesDir { get; }

    private async Task<DalamudVersionInfo?> GetVersionInfoForTrackAsync(string track)
    {
        var dalamudTrack = "staging";
        if (this.overrides != null && this.overrides.ChannelTracks.TryGetValue(track, out var mapping))
        {
            dalamudTrack = mapping;
            Log.Information("Overriding channel {Track} Dalamud track with {NewTrack}", track, dalamudTrack);
        }

        using var client = new HttpClient();
        return await client.GetFromJsonAsync<DalamudVersionInfo>(string.Format(URL_TEMPLATE, dalamudTrack));
    }

    /// <summary>
    /// Download Dalamud for a track and get the place it is
    /// </summary>
    /// <param name="track"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<DirectoryInfo> GetDalamudAssemblyDirAsync(string track)
    {
        var versionInfo = await this.GetVersionInfoForTrackAsync(track);
        if (versionInfo == null)
            throw new Exception("Could not get Dalamud version info");

        var extractDir = this.ReleasesDir.CreateSubdirectory($"{track}-{versionInfo.AssemblyVersion}");

        if (extractDir.GetFiles().Length != 0)
            return extractDir;

        Log.Information("Downloading Dalamud assembly for track {Track}({Version})", track, versionInfo.AssemblyVersion);

        using var client = new HttpClient();
        var zipBytes = await client.GetByteArrayAsync(versionInfo.DownloadUrl);

        // Extract the zip file to the extractDir
        using var zipStream = new MemoryStream(zipBytes);
        // using var archive = new ZipArchive(zipStream);
        // archive.ExtractToDirectory(extractDir.FullName);
        var output = extractDir.FullName;
        using (var archive = ArchiveFactory.Open(zipStream))
        {
            var reader = archive.ExtractAllEntries();

            while (reader.MoveToNextEntry())
            {
                if (!reader.Entry.IsDirectory)
                    reader.WriteEntryToDirectory(output, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
            }
        }

        return extractDir;
    }

    private class DalamudVersionInfo
    {
#pragma warning disable CS8618
        public string AssemblyVersion { get; set; }
        public string SupportedGameVer { get; set; }
        public string RuntimeVersion { get; set; }
        public bool RuntimeRequired { get; set; }
        public string Key { get; set; }
        public string DownloadUrl { get; set; }
#pragma warning restore CS8618
    }
}
