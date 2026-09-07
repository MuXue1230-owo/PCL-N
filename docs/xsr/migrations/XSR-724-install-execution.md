# XSR-724 Real install execution

`MinecraftInstallService` (PCL.Services/Minecraft/Install) turns the install selection into a
real version-library install and reports the entire run as one task-center task. The route is
`minecraft.install.run` (`MinecraftInstallCommand`: root, game version, primary loader +
build, addon list); the runtime composer builds the router over the foundation host.

## Pipeline

1. **版本信息** — resolve the vanilla version JSON from the Mojang manifest (bmclapi failover
   via `MinecraftDownloadSourcePlanner.GetLauncherOrMetaSources`), and for the Fabric family
   the loader profile JSON (`meta…/versions/loader/{game}/{build}/profile/json`). Both version
   documents are written before any transfer — a partial install stays inspectable, matching
   legacy write-order. Loader documents carry `id = <game>-<loader><build>` and
   `inheritsFrom = <game>`.
2. **Planning** — one transfer plan over the existing shared planners: asset index
   (`CreateAssetIndexPlan`), client jar (`CreateClientJarPlan`), vanilla + loader libraries
   (`MinecraftLibraryResolver` with the detected platform context, bmclapi failover), assets
   (`MinecraftAssetListResolver` + `MinecraftAssetDownloadPlanner`, existing files skipped),
   and addon jars resolved from the install catalog sources into `mods/`. Files that already
   exist with content are skipped, so re-runs complete rather than re-download.
3. **游戏文件 / 加载器 / 附加组件** — every planned file transfers through the shared
   `DownloadService` (resume, segmentation, per-destination coalescing). Progress is
   file-accurate across one shared budget: overall = (completed + intra-file fraction) /
   total files, speed passes through per transfer.
4. **完成** — the task completes (`已安装 <id>`), `Installed(root)` fires, and the composition
   root rescans the active version library so the new instance appears immediately.

## Scope boundary (explicit)

Processor-based loaders (Forge, NeoForge, Cleanroom, OptiFine's installer path, LiteLoader,
LabyMod) are rejected **before any disk write** with a message that names the missing path —
XSR-604 deliberately deferred processor execution. The Fabric family (Fabric, Legacy Fabric,
Quilt) is fully supported because their profile JSON is declarative. The Java-runtime and
Bedrock install flows remain on their own tracks (JavaRuntimeInstaller / XSR-721).

## Test seams

Metadata resolution and byte transfer are ports: `IMinecraftInstallMetadataSource`
(production: HTTP over the shared client) and the per-request `ConnectionFactory`
(production: HTTP connection adapter). Tests inject in-memory fakes — no network in CI.
