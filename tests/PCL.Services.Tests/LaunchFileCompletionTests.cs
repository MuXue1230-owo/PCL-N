using System.Text.Json.Nodes;
using PCL.Services.Downloads;
using PCL.Services.Minecraft;
using PCL.Services.Minecraft.Assets;
using PCL.Services.Minecraft.Launch;
using PCL.Services.Minecraft.Libraries;
using PCL.Xsr.State;

namespace PCL.Services.Tests;

// The legacy 补全文件 contract: before the JVM starts, every referenced file is verified and
// repaired — the client jar, the asset index, asset objects, and the whole inheritance chain's
// libraries. A missing library used to kill the JVM before its window appeared.
internal static partial class Program
{
    private sealed class CompletionFixture : IDisposable
    {
        public XsrStateStore Store;
        public DownloadService Downloads;
        public MinecraftLaunchFileCompletion Completion;
        public List<string> RequestedUrls = [];
        public string Root;

        public CompletionFixture()
        {
            XsrStateStoreBuilder builder = new();
            DownloadService.DeclareState(builder);
            Store = builder.Build();
            Downloads = new(Store);
            Root = Path.Combine(Path.GetTempPath(), "nexa-completion-tests", Guid.NewGuid().ToString("N"));
            Completion = new(
                Downloads,
                connectionFactory: source =>
                {
                    lock (RequestedUrls) RequestedUrls.Add(source);
                    return new ServingConnection("REPAIRED"u8.ToArray());
                });
        }

        public void Dispose()
        {
            Completion.Dispose();
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }

    private static async ValueTask LaunchCompletionRepairsMissingFilesBeforeStart()
    {
        using CompletionFixture fixture = new();
        string versionsRoot = Path.Combine(fixture.Root, "versions");
        string gameDirectory = Path.Combine(versionsRoot, "1.20.1");
        Directory.CreateDirectory(gameDirectory);
        await File.WriteAllTextAsync(Path.Combine(gameDirectory, "1.20.1.json"), """
        {
          "id": "1.20.1",
          "assetIndex": {
            "id": "5",
            "url": "https://piston-meta.mojang.com/v1/packages/index/5.json",
            "sha1": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "size": 90,
            "totalSize": 90
          },
          "downloads": {
            "client": {
              "url": "https://piston-data.mojang.com/v1/objects/client.jar",
              "sha1": "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
              "size": 10
            }
          },
          "libraries": [
            {
              "name": "com.example:present:1.0.0",
              "downloads": { "artifact": {
                "path": "com/example/present/1.0.0/present-1.0.0.jar",
                "url": "https://libraries.minecraft.net/com/example/present.jar",
                "sha1": "cccccccccccccccccccccccccccccccccccccccc",
                "size": 8 } }
            },
            {
              "name": "com.example:missing:1.0.0",
              "downloads": { "artifact": {
                "path": "com/example/missing/1.0.0/missing-1.0.0.jar",
                "url": "https://libraries.minecraft.net/com/example/missing.jar",
                "sha1": "dddddddddddddddddddddddddddddddddddddddd",
                "size": 8 } }
            }
          ]
        }
        """);

        // Present files stay untouched; the client jar, the missing library, the asset index,
        // and one asset object are gone.
        string presentJar = Path.Combine(
            fixture.Root, "libraries", "com", "example", "present", "1.0.0", "present-1.0.0.jar");
        Directory.CreateDirectory(Path.GetDirectoryName(presentJar)!);
        await File.WriteAllTextAsync(presentJar, "ALREADY-THERE");
        string assetHash = new string('e', 40);
        string assetPath = Path.Combine(fixture.Root, "assets", "objects", assetHash[..2], assetHash);
        Directory.CreateDirectory(Path.GetDirectoryName(assetPath)!);
        await File.WriteAllTextAsync(assetPath, "ASSET-OK");
        Directory.CreateDirectory(Path.Combine(fixture.Root, "assets", "indexes"));
        await File.WriteAllTextAsync(Path.Combine(fixture.Root, "assets", "indexes", "5.json"), """
        { "objects": {
            "minecraft/sounds/gone.ogg": {
              "hash": "ffffffffffffffffffffffffffffffffffffffff",
              "size": 5 }
        } }
        """);

        MinecraftInstanceDescriptor instance = new(
            "1.20.1",
            gameDirectory,
            "1.20.1",
            new MinecraftVersionDescriptor(
                "1.20.1",
                gameDirectory,
                Path.Combine(gameDirectory, "1.20.1.json"),
                JarPath: null,
                InheritsFrom: null,
                MainClass: null,
                ReleaseTime: null,
                Classification: new MinecraftVersionClassification("1.20.1", "release", MinecraftVersionCategory.Release, null)),
            new MinecraftInstanceMetadata());
        MinecraftResolvedVersionManifests manifests = await MinecraftVersionJsonReader.ResolveAsync(
            instance, fixture.Root);

        await fixture.Completion.CompleteAsync(
            fixture.Root,
            instance,
            manifests,
            new MinecraftLaunchPlatform(
                MinecraftLibraryOperatingSystem.Win32,
                "10.0.26100",
                Is64BitArchitecture: true,
                IsArm64Architecture: false),
            method: "offline",
            progress: null,
            CancellationToken.None);

        string missingJar = Path.Combine(
            fixture.Root, "libraries", "com", "example", "missing", "1.0.0", "missing-1.0.0.jar");
        AssertTrue(File.Exists(missingJar));
        AssertTrue(File.Exists(Path.Combine(gameDirectory, "1.20.1.jar")));
        string repairedAsset = Path.Combine(
            fixture.Root, "assets", "objects", "ffffffffffffffffffffffffffffffffffffffff"[..2],
            "ffffffffffffffffffffffffffffffffffffffff");
        AssertTrue(File.Exists(repairedAsset));
        // Present files are never re-downloaded.
        AssertEqual("ALREADY-THERE", await File.ReadAllTextAsync(presentJar));
        AssertEqual("ASSET-OK", await File.ReadAllTextAsync(assetPath));
        lock (fixture.RequestedUrls)
        {
            AssertFalse(fixture.RequestedUrls.Any(url => url.Contains("present.jar", StringComparison.Ordinal)));
        }
    }
}
