using System.Text.Json.Nodes;
using PCL.Services.Downloads;
using PCL.Services.Logging;
using PCL.Services.Minecraft.Assets;
using PCL.Services.Minecraft.Downloads;
using PCL.Services.Minecraft.Libraries;

namespace PCL.Services.Minecraft.Launch;

/// <summary>
/// The legacy 补全文件 stage: before the JVM starts, every file the launch plan references is
/// verified on disk and repaired through the shared download service — client jar, asset
/// index, asset objects, and the full inheritance chain's libraries (natives included). A
/// missing library would otherwise kill the JVM before its window appears, which reported as
/// an opaque launch failure. Files that exist with content are skipped, so completion after a
/// partial install resumes instead of re-downloading.
/// </summary>
public sealed class MinecraftLaunchFileCompletion : IDisposable
{
    private static readonly TimeSpan FileRetryDelay = TimeSpan.FromSeconds(3);

    private readonly DownloadService _downloads;
    private readonly LogService? _log;
    private readonly HttpClient _http = new();
    private readonly Func<string, IDownloadConnection>? _connectionFactory;

    public MinecraftLaunchFileCompletion(
        DownloadService downloads,
        LogService? log = null,
        Func<string, IDownloadConnection>? connectionFactory = null)
    {
        _downloads = downloads ?? throw new ArgumentNullException(nameof(downloads));
        _log = log;
        _connectionFactory = connectionFactory;
    }

    public async ValueTask CompleteAsync(
        string minecraftRootDirectory,
        MinecraftInstanceDescriptor instance,
        MinecraftResolvedVersionManifests manifests,
        MinecraftLaunchPlatform platform,
        string method,
        MinecraftLaunchProgressPublisher? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(manifests);
        ArgumentException.ThrowIfNullOrWhiteSpace(minecraftRootDirectory);

        string root = Path.GetFullPath(minecraftRootDirectory);
        // The chain reads current → nearest parent → … → root; the base (vanilla) manifest
        // owns the client jar and the asset index.
        JsonObject baseManifest = manifests.Inherited.Count > 0 ? manifests.Inherited[^1] : manifests.Current;
        string baseId = baseManifest["id"]?.ToString() ?? instance.VersionId;

        List<(string[] Sources, string Destination)> missing = [];

        MinecraftClientJarDownloadPlan clientPlan = MinecraftClientDownloadPlanner.CreateClientJarPlan(
            new MinecraftClientJarDownloadPlanRequest
            {
                VersionJson = baseManifest,
                InstanceDirectory = Path.Combine(root, "versions", baseId),
                VersionName = baseId,
            });
        if (clientPlan.File is { } client)
        {
            AddIfMissing(missing,
                MinecraftDownloadSourcePlanner.GetLauncherOrMetaSources(client.Url, true),
                client.LocalPath);
        }

        MinecraftAssetIndexDownloadPlan indexPlan = MinecraftClientDownloadPlanner.CreateAssetIndexPlan(
            new MinecraftAssetIndexDownloadPlanRequest
            {
                VersionJson = baseManifest,
                InheritedVersionJsons = manifests.Inherited,
                MinecraftRootDirectory = root,
            });
        if (indexPlan.HasDownload && indexPlan.LocalPath is { } indexPath)
        {
            AddIfMissing(missing,
                MinecraftDownloadSourcePlanner.GetLauncherOrMetaSources(indexPlan.Url!, true),
                indexPath);
        }

        // Libraries accumulate across the whole chain: the child's overrides win.
        Dictionary<string, (string Url, string? Sha1)> libraries = new(MinecraftLibraryService.PathComparer);
        foreach (JsonObject manifest in new[] { manifests.Current }.Concat(manifests.Inherited))
        {
            foreach (MinecraftLibraryToken library in MinecraftLibraryResolver.Resolve(
                new MinecraftLibraryResolutionRequest
                {
                    VersionJson = manifest,
                    MinecraftRootDirectory = root,
                    OperatingSystem = platform.OperatingSystem,
                    Is64BitArchitecture = platform.Is64BitArchitecture,
                    IsArm64Architecture = platform.IsArm64Architecture,
                    OperatingSystemVersion = platform.OperatingSystemVersion,
                }))
            {
                if (library.IsLocal || string.IsNullOrWhiteSpace(library.Url))
                {
                    continue;
                }

                libraries[library.LocalPath] = (library.Url, library.Sha1);
            }
        }

        foreach ((string localPath, (string url, _)) in libraries)
        {
            string[] sources = MinecraftDownloadSourcePlanner.GetLibrarySources(url, true);
            if (!sources.Contains(url, StringComparer.Ordinal))
            {
                sources = [.. sources, url];
            }

            AddIfMissing(missing, sources, localPath);
        }

        // Assets are planned from the index document, so fetch the index first when missing,
        // then plan the objects against what exists on disk.
        string? indexDiskPath = indexPlan.LocalPath;
        if (indexDiskPath is null && indexPlan.IndexId is { Length: > 0 } indexId)
        {
            indexDiskPath = Path.Combine(root, "assets", "indexes", indexId + ".json");
        }

        if (indexDiskPath is not null)
        {
            await EnsureAssetIndexAsync(indexDiskPath, indexPlan, cancellationToken).ConfigureAwait(false);
            if (File.Exists(indexDiskPath))
            {
                JsonObject indexJson = await MinecraftVersionJsonReader.ReadAsync(indexDiskPath, cancellationToken)
                    .ConfigureAwait(false);
                IReadOnlyList<MinecraftAssetToken> assets = MinecraftAssetListResolver.GetAssetList(
                    new MinecraftAssetListRequest
                    {
                        IndexJson = indexJson,
                        MinecraftRootDirectory = root,
                        InstanceDirectory = instance.DirectoryPath,
                    });
                Dictionary<string, MinecraftAssetFileState> states = [];
                foreach (MinecraftAssetToken asset in assets)
                {
                    states[asset.LocalPath] = new MinecraftAssetFileState(File.Exists(asset.LocalPath), 0);
                }

                foreach (MinecraftAssetDownloadFile file in MinecraftAssetDownloadPlanner.CreatePlan(
                    new MinecraftAssetDownloadPlanRequest { Assets = assets, CheckHash = false, ExistingFiles = states })
                    .Files)
                {
                    AddIfMissing(missing,
                        MinecraftDownloadSourcePlanner.GetAssetSources(
                            MinecraftAssetListResolver.GetObjectUrl(file.Hash), true),
                        file.LocalPath);
                }
            }
        }

        if (missing.Count == 0)
        {
            _log?.Debug("Launch", "File completion found nothing missing; continuing.");
            return;
        }

        _log?.Info("Launch", $"File completion repairing {missing.Count} missing file(s).");
        int done = 0;
        foreach ((string[] sources, string destination) in missing)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(destination) && new FileInfo(destination).Length > 0)
            {
                done++;
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            DownloadTransferResult transfer = await TransferAsync(
                sources, destination, cancellationToken).ConfigureAwait(false);
            if (!transfer.Success)
            {
                // Mirror rate limits are bursty; one short retry has saved whole launches.
                await Task.Delay(FileRetryDelay, cancellationToken).ConfigureAwait(false);
                transfer = await TransferAsync(sources, destination, cancellationToken).ConfigureAwait(false);
            }

            if (!transfer.Success)
            {
                throw new InvalidOperationException(
                    $"补全文件失败：{Path.GetFileName(destination)}（{(transfer.Errors.Count > 0 ? transfer.Errors[0].Message : "未知错误")}）");
            }

            done++;
            double fraction = missing.Count == 0 ? 1d : done / (double)missing.Count;
            progress?.Report(new MinecraftLaunchStageReport(
                MinecraftLaunchStages.CompleteFiles,
                MinecraftLaunchStages.LoginWeight
                    + (MinecraftLaunchStages.CompleteFilesWeight * Math.Clamp(fraction, 0d, 1d)),
                Method: method));
        }

        progress?.Report(new MinecraftLaunchStageReport(
            MinecraftLaunchStages.CompleteFiles,
            MinecraftLaunchStages.LoginWeight + MinecraftLaunchStages.CompleteFilesWeight,
            Method: method));
    }

    private async ValueTask EnsureAssetIndexAsync(
        string indexDiskPath,
        MinecraftAssetIndexDownloadPlan plan,
        CancellationToken cancellationToken)
    {
        if (File.Exists(indexDiskPath) || !plan.HasDownload)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(indexDiskPath)!);
        DownloadTransferResult transfer = await TransferAsync(
            MinecraftDownloadSourcePlanner.GetLauncherOrMetaSources(plan.Url!, true),
            indexDiskPath,
            cancellationToken).ConfigureAwait(false);
        if (!transfer.Success)
        {
            await Task.Delay(FileRetryDelay, cancellationToken).ConfigureAwait(false);
            transfer = await TransferAsync(
                MinecraftDownloadSourcePlanner.GetLauncherOrMetaSources(plan.Url!, true),
                indexDiskPath,
                cancellationToken).ConfigureAwait(false);
        }

        if (!transfer.Success)
        {
            throw new InvalidOperationException("补全文件失败：资源索引下载失败。");
        }
    }

    private Task<DownloadTransferResult> TransferAsync(
        IReadOnlyList<string> sources,
        string destination,
        CancellationToken cancellationToken,
        Action<DownloadProgress>? progress = null) =>
        _downloads.DownloadAsync(
            new DownloadRequest
            {
                Sources = sources,
                DestinationPath = destination,
                ConnectionFactory = _connectionFactory is { } factory
                    ? source => factory(source)
                    : source => new HttpConnectionAdapter(_http, source),
            },
            progress,
            cancellationToken);

    private static void AddIfMissing(
        List<(string[] Sources, string Destination)> missing, string[] sources, string destination)
    {
        if (File.Exists(destination) && new FileInfo(destination).Length > 0)
        {
            return;
        }

        for (int index = 0; index < missing.Count; index++)
        {
            if (MinecraftLibraryService.PathComparer.Equals(missing[index].Destination, destination))
            {
                missing[index] = (sources, destination);
                return;
            }
        }

        missing.Add((sources, destination));
    }

    public void Dispose() => _http.Dispose();

    /// <summary>Adapts one HttpClient GET to the download engine's connection port.</summary>
    private sealed class HttpConnectionAdapter(HttpClient client, string source) : IDownloadConnection
    {
        private HttpResponseMessage? _response;

        public async ValueTask<DownloadConnectionInfo> StartAsync(
            long beginOffset, CancellationToken cancellationToken = default)
        {
            HttpRequestMessage request = new(HttpMethod.Get, source);
            if (beginOffset > 0)
            {
                request.Headers.Range = new(beginOffset, null);
            }

            _response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
            _response.EnsureSuccessStatusCode();
            long length = _response.Content.Headers.ContentLength ?? -1;
            return new DownloadConnectionInfo(length, beginOffset, length >= 0 ? beginOffset + length - 1 : -1, false);
        }

        public async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Stream stream = _response?.Content is null
                ? throw new InvalidOperationException("The connection was never started.")
                : await _response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        public ValueTask StopAsync(CancellationToken cancellationToken = default)
        {
            _response?.Dispose();
            _response = null;
            return ValueTask.CompletedTask;
        }
    }
}
