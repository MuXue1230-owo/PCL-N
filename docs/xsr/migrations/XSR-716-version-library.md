# XSR-716 Multi-directory version selection

## Locked contract

The `选择版本` subpage uses one compact `当前目录：[name ▾]` selector above an installed
version list. It opens a bounded dropdown over the list, with outside-click/Escape dismissal.
Unnamed directories show their complete path. Named directories show a name and the complete
path in secondary text at the right. Names can be added, changed and cleared; the platform
official-launcher directory is always `官方文件夹` and its name cannot be edited. Repeated
instructional copy and the selection footer are omitted. A check identifies selection. Switching directories, adding a folder
through a native picker or absolute path, refreshing and forgetting registrations are supported.
Forgetting a directory never deletes game files. Successful version selection persists, returns
immediately to the launch home and updates it without starting a game. Keyboard/back focus and existing interruptible/reduced motion
remain intact. Search filters presentation without changing selection.

`MinecraftLibraryService` owns directory-qualified selection and discovery snapshots in shared
Host State. It reuses `MinecraftInstanceDiscovery` and `SettingsService`. One additive text setting,
`MinecraftLibrary`, contains a schema-1 JSON document with registered paths, active directory and
per-directory selected IDs and optional names (absent names deserialize as empty). The existing
`DefaultMinecraftRootProvider` supplies the official directory identity. Writes complete before state publication. Paths are absolute and
normalized using the platform comparer; equal version IDs in different roots remain distinct.
Scans are cancellable and generation-scoped; stale results never replace a newer choice.
Missing, inaccessible and empty roots expose explicit states. Unknown existing settings survive.

Desktop owns only PXML projection, transient search/chooser state and the native picker effect.
Workers publish state; entities are reconciled only at frame preparation. The launch command keeps
its existing two-argument constructor and adds an optional `MinecraftRootDirectory` property.
Existing coordinator overloads remain; explicit-root overloads resolve inheritance, libraries,
assets and Java acquisition within that captured root. An in-flight launch is never redirected by
a subsequent directory selection. PXML Host IR and Sidecar compatibility surfaces do not change.

Version-kind parity is taken from the read-only legacy `InstanceDisplayHelper` and its state-icon
acceptance cases: release/Grass, snapshot/CommandBlock, old/CobbleStone, April Fools/GoldBlock,
OptiFine/GrassPath, LiteLoader/Egg, Forge/Anvil, NeoForge, Cleanroom, Quilt, Fabric (including
Legacy Fabric), and LabyMod. Service discovery publishes a domain kind, including inherited
loader signals and their precedence; Desktop maps that kind to a bounded backend image key.
Only these twelve individual PNG assets are migrated, with provenance; no legacy implementation
or project dependency is imported. Invalid manifests keep the existing discovery exclusion rule.

## Acceptance

Cover two roots with identical IDs, restart persistence, cancelled/stale scans, save failures,
selection retention, missing/empty roots, safe forgetting, root-qualified start commands,
selection markers, focus restoration, minimum-window geometry, search and scrolling. Run managed
suites, architecture/format gates, renderer benchmarks and Desktop NativeAOT/trim smoke. Inspect
the rendered Avalonia scene before closing the unit.
