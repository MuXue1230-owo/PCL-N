# XSR-724 Real install execution

`MinecraftInstallService` (PCL.Services/Minecraft/Install) turns the install selection into a
real version-library install and reports the entire run as one task-center task. The route is
`minecraft.install.run` (`MinecraftInstallCommand`: root, game version, primary loader +
build, addon list); the runtime composer builds the router over the foundation host.

## Pipeline

1. **版本信息** — resolve the vanilla version JSON from the Mojang manifest (bmclapi failover
   via `MinecraftDownloadSourcePlanner.GetLauncherOrMetaSources`), and for the Fabric family
   the loader profile JSON (`meta…/versions/loader/{game}/{build}/profile/json`). Loader
   documents carry `id = <game>-<loader><build>` and `inheritsFrom = <game>`. Both version
   documents **commit last**, after every transfer: discovery lists instances by their version
   json, so a run that dies mid-transfer leaves an invisible, resumable directory instead of a
   launchable half-install whose missing libraries kill the JVM before its window appears.
2. **Planning** — one transfer plan over the existing shared planners: asset index
   (`CreateAssetIndexPlan`), client jar (`CreateClientJarPlan`), vanilla + loader libraries
   (`MinecraftLibraryResolver` with the detected platform context, bmclapi failover), assets
   (`MinecraftAssetListResolver` + `MinecraftAssetDownloadPlanner`, existing files skipped),
   and addon jars resolved from the install catalog sources into `mods/`. Files that already
   exist with content are skipped, so re-runs complete rather than re-download.
3. **游戏文件 / 加载器 / 附加组件** — every planned file transfers through the shared
   `DownloadService` (resume, segmentation, per-destination coalescing). Progress is
   file-accurate across one shared budget: overall = (completed + intra-file fraction) /
   total files, speed passes through per transfer. Mirror rate limits are bursty, so one file
   gets a short delayed retry, and third-party library lists keep the canonical URL as a
   last-resort source after the bmclapi mirrors.
4. **完成** — the task completes (`已安装 <id>`), `Installed(root)` fires, and the composition
   root rescans the active version library so the new instance appears immediately.

## Scope boundary (explicit)

Deferred processor-based loaders (Cleanroom, OptiFine's installer path, LiteLoader,
LabyMod) are rejected **before any disk write** with a message that names the missing path —
Forge and NeoForge use the isolated official installer flow described below. The Fabric family (Fabric, Legacy Fabric,
Quilt) is fully supported because their profile JSON is declarative. The Java-runtime and
Bedrock install flows remain on their own tracks (JavaRuntimeInstaller / XSR-721).

## Integrity and scoping corrections (2026-09-09, review round)

- **One shared verifier.** `MinecraftFileVerifier` is the single integrity rule for install,
  launch completion, and future repair passes: a known SHA-1 must match, else a known size
  must match, else the file needs only existence-with-content. Every consumer uses it both
  for reuse decisions (existence-with-content alone used to certify truncated artifacts as
  complete installs) and after each transfer — a failed verification deletes the artifact and
  retries once before failing the run. Assets are hash-verified too: launch completion plans
  every object and lets the verifier decide reuse (the `CheckHash = false` shortcut is gone).
- **Addon resolution is game-scoped.** `ResolveAddonDownloadAsync` passes the install's game
  version into `GetLoadersAsync`; provider-side game-version filters are meaningless without
  it.

## Launch-side twin

`MinecraftLaunchFileCompletion` runs the same planner set from the launch pipeline's
`complete_files` stage (see XSR-717): verify-then-repair with the shared download engine,
never re-downloading present files.

## Test seams

Metadata resolution and byte transfer are ports: `IMinecraftInstallMetadataSource`
(production: HTTP over the shared client) and the per-request `ConnectionFactory`
(production: HTTP connection adapter). Tests inject in-memory fakes — no network in CI.

## Selected artifact retention (2026-09-11)

Commands carry the base game version independently of the optional instance name. The Desktop
projects selected addon download descriptors from sealed catalog state into the install command.
The installer validates HTTPS and the leaf filename, then uses the normal verified transfer path.
Legacy callers without descriptors still resolve the game-scoped catalog.

## Forge / NeoForge follow-up (2026-09-12)

Launch resolution options (`--width`, `--height`) have one authoritative final value, even
when loader/vanilla manifests already declare them. Forge early-display rejects duplicate options.

Forge and NeoForge use official Maven installers in a private staging root. Services acquire
compatible Java, prefetch declared installer libraries with the shared download engine, run
`--installClient` without a console window, drain output and terminate the installer on cancellation.
Only a successful, validated loader manifest is published to the real version library; staging
launcher profiles never replace user profiles. Other processor loaders remain deferred.

Validation: deterministic service tests cover Forge/NeoForge dispatch, staging isolation,
manifest-last publication, generated-library integrity and missing processor output. Real
Windows installer probes completed for Forge 47.4.20 / Minecraft 1.20.1 and NeoForge
21.1.235 / Minecraft 1.21.1. These probes validate installation, not interactive game rendering.
