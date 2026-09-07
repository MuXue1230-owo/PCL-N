using PCL.Pxml;
using PCL.Services.Minecraft.Install;
using PCL.UI.Next;
using PCL.Xsr;
using PCL.Xsr.Runtime;

namespace PCL.Desktop.Ui;

internal sealed partial class LaunchPageController
{
    private readonly XsrCommandRouter? _installCatalogCommands;
    private bool _installGameChosen;
    private Task _installPrefetchTask = Task.CompletedTask;
    private void PrefetchInstallCatalog(string game)
    {
        if (_installCatalogCommands is not null && _installCatalogCommands.TryResolve(InstallCatalogRoutes.Prefetch, out XsrCommandId id))
            _installPrefetchTask = _installCatalogCommands.Dispatch(id, new InstallCatalogPrefetchCommand(game), cancellationToken: _lifetimeCancellation.Token).Completion;
    }
    private readonly HashSet<InstallLoader> _supportedInstallLoaders = [];
    private static InstallLoader? ParseInstallLoader(string? value) => Enum.TryParse(value?.Replace(" ", "", StringComparison.Ordinal), out InstallLoader loader) ? loader : null;
    private void ChooseInstallGame(string version)
    {
        _installGameChosen = true;
        _selectedInstallVersion = version;
        _selectedInstallBuilds.Clear();
        _supportedInstallLoaders.Clear();
        SelectInstallLoader("原版 Minecraft", "JavaLoaderVanilla");
        foreach (InstallLoader loader in Enum.GetValues<InstallLoader>())
            if (!InstallCompatibility.IsAddon(loader)
                && InstallCompatibility.UnavailableReason(loader, version) is null) _supportedInstallLoaders.Add(loader);
        UpdateJavaInstallSubpageVisibility();
        PrefetchInstallCatalog(version);
        _locateCatalogSelection = true;
        _catalogRevision = -1;
    }
    private Task _installCatalogTask = Task.CompletedTask;
    private string _catalogRequest = "";
    private long _catalogRevision = -1;
    private long _catalogStateRevision = -1;
    private int _catalogFirst = -1, _catalogCount;
    private bool _locateCatalogSelection;
    private string _catalogSearch = "";
    private long _filteredRevision = -1;
    private IReadOnlyList<InstallCatalogVersion> _filteredGames = [];
    private readonly Dictionary<string, InstallCatalogSnapshot> _catalogCache = [];
    private string CurrentCatalogKey => ActiveCatalogLoader is null ? "games" : _selectedInstallVersion + ":" + _activeJavaInstallPage;
    private readonly Dictionary<InstallLoader, string> _selectedInstallBuilds = [];
    private readonly Dictionary<string, XsrUiEntityId> _catalogHosts = [];
    private readonly Dictionary<XsrUiEntityId, InstallCatalogVersion> _catalogRows = [];
    private readonly Dictionary<string, XsrUiEntityId> _catalogStatus = [];
    private static readonly XsrSemanticId CatalogSelect = XsrSemanticId.Parse("ui.install.catalog.select");
    private static readonly XsrSemanticId CatalogRetry = XsrSemanticId.Parse("ui.install.catalog.retry");
    private static readonly PxmlHostIr CatalogRowTemplate = PxmlCompiler.Compile(PxmlParser.Parse("""
        <Button xmlns="https://pcln.dev/pxml/2026" Key="CatalogRow" Label="选择版本" Command="ui.install.catalog.select" Height="48">
          <StackPanel Orientation="Horizontal" Spacing="12" Weight="1" Margin="12,0,12,0">
            <Image Key="CatalogCheck" Source="lucide/check" Width="18" Height="18" VerticalAlignment="Center" />
            <Text Key="CatalogName" Weight="1" Height="24" VerticalAlignment="Center" />
            <Text Key="CatalogDetail" Width="160" Height="20" VerticalAlignment="Center" />
          </StackPanel>
        </Button>
        """));
    private InstallLoader? ActiveCatalogLoader => (_activeJavaInstallPage == "JavaOptiFabricPage" ? "OptiFabric" : _activeJavaInstallPage == "JavaFabricApiPage" ? "Fabric API" : _activeJavaInstallPage == "JavaQslPage" ? "QSL" : JavaInstallSubpages.First(p => p.PageKey == _activeJavaInstallPage).Loader) switch
    {
        "Fabric API" => InstallLoader.FabricApi,
        "QSL" => InstallLoader.Qsl,
        "OptiFabric" => InstallLoader.OptiFabric,
        "Forge" => InstallLoader.Forge,
        "Cleanroom" => InstallLoader.Cleanroom,
        "NeoForge" => InstallLoader.NeoForge,
        "Fabric" => InstallLoader.Fabric,
        "Legacy Fabric" => InstallLoader.LegacyFabric,
        "Quilt" => InstallLoader.Quilt,
        "LabyMod" => InstallLoader.LabyMod,
        "OptiFine" => InstallLoader.OptiFine,
        "LiteLoader" => InstallLoader.LiteLoader,
        _ => null,
    };
    private void InitializeInstallCatalog()
    {
        foreach (JavaInstallSubpage page in JavaInstallSubpages)
        {
            XsrUiEntityId parent = _javaInstallEntities[page.PageKey];
            // Version names, status and selections come from the service, never a baked-in list.
            XsrUiEntityId host = page.PageKey == "JavaMinecraftPage" ? _javaInstallEntities["JavaMinecraftVersions"] : _shell.Tree.Create(page.PageKey + "Rows");
            if (page.PageKey != "JavaMinecraftPage")
            {
                _shell.Tree.Attach(host, parent);
                _shell.Tree.SetComponent(host, new XsrUiElement());
                _shell.Tree.SetComponent(host, new XsrUiStackPanel(XsrUiOrientation.Vertical) { Spacing = 2 });
                _shell.Tree.SetComponent(parent, new XsrUiScroll { ShowsVerticalIndicator = true });
            }
            if (page.Loader is not null)
            {
                string loaderKey = "JavaLoader" + page.PageKey[4..^4];
                _shell.Tree.GetComponent<XsrUiElement>(_javaInstallEntities[loaderKey])!.IsVisible = false;
            }
            if (page.PageKey is "JavaFabricApiPage" or "JavaQslPage")
                _shell.Tree.GetComponent<XsrUiElement>(_javaInstallEntities[page.PageKey == "JavaFabricApiPage" ? "JavaFabricApiSelect" : "JavaQslSelect"])!.IsVisible = false;
            _shell.Tree.GetComponent<XsrUiStackPanel>(host)!.Spacing = 0;
            _shell.Tree.SetComponent(host, new XsrUiStableContent());
            _shell.Tree.SetComponent(parent, new XsrUiScrollGesture());
            _catalogHosts[page.PageKey] = host;
            XsrUiEntityId status = _shell.Tree.Create(page.PageKey + "Status");
            _shell.Tree.Attach(status, parent);
            _shell.Tree.SetComponent(status, new XsrUiElement { Height = 40 });
            _shell.Tree.SetComponent(status, new XsrUiInput { Focusable = true, Clickable = true });
            _shell.Tree.SetComponent(status, new XsrUiCommandBinding(CatalogRetry));
            _shell.Tree.SetComponent(status, new XsrUiSemantic(XsrUiSemanticRole.Button, "重新获取版本"));
            _shell.Tree.SetComponent(status, new XsrUiText("正在获取版本…"));
            _shell.Tree.SetComponent(status, new XsrUiVisualStyle { Foreground = SecondaryText, FontSize = 13, WrapText = true });
            _catalogStatus[page.PageKey] = status;
            // Keep dependency notices ahead of potentially very long version lists.
            _shell.Tree.Detach(host);
            _shell.Tree.Attach(host, parent);
        }
    }
    private void RequestInstallCatalog(bool force = false)
    {
        if (_installCatalogCommands is null || !_catalogHosts.ContainsKey(_activeJavaInstallPage)) return;
        InstallLoader? loader = ActiveCatalogLoader;
        string key = CurrentCatalogKey;
        if (!force && key == _catalogRequest) return;
        _catalogRequest = key;
        _locateCatalogSelection = true;
        _catalogRevision = -1;
        _catalogFirst = -1;
        if (!force && _catalogCache.ContainsKey(key)) return;
        if (!force && (_store.ReadAppliedValue(_store.Resolve(InstallCatalogService.StateKey)) as InstallCatalogState)?.Catalogs
            .Any(item => item.Revision > 0 && item.Loader == loader && (loader is null || item.GameVersion == _selectedInstallVersion)) == true) return;
        _catalogCache.Remove(key);
        if (_installCatalogCommands.TryResolve(InstallCatalogRoutes.Read, out XsrCommandId id))
            _installCatalogTask = _installCatalogCommands.Dispatch(id, new InstallCatalogReadCommand(loader is null ? "" : _selectedInstallVersion, loader, force), cancellationToken: _lifetimeCancellation.Token).Completion;
    }
    private bool HandleInstallCatalogIntent(DesktopUiIntentEventArgs e)
    {
        if (_shell.Stage.Navigation.Current != _javaInstallPage) return false;
        if (e.Intent.Command == CatalogRetry)
        {
            RequestInstallCatalog(true);
            return true;
        }
        if (e.Intent.Command != CatalogSelect || !_catalogRows.TryGetValue(e.Intent.Source, out InstallCatalogVersion? version)) return false;
        if (ActiveCatalogLoader is { } candidate && _selectedInstallBuilds.GetValueOrDefault(candidate) != version.Id
            && InstallCompatibility.BuildConflict(candidate, version, SelectedInstallCatalogVersions()) is not null) return true;
        if (ActiveCatalogLoader is null)
        {
            if (_installGameChosen && _selectedInstallVersion == version.Id)
            {
                _installGameChosen = false;
                PrefetchInstallCatalog("");
                _selectedInstallBuilds.Clear();
                _supportedInstallLoaders.Clear();
                SelectInstallLoader("原版 Minecraft", "JavaLoaderVanilla");
                _shell.Renderer.SetTextInputValue(_javaInstallEntities["JavaInstallVersionInput"], "");
                _catalogRevision = -1;
                return true;
            }
            ChooseInstallGame(version.Id);
            _shell.Renderer.SetTextInputValue(_javaInstallEntities["JavaInstallVersionInput"], version.Id);
            SelectInstallLoader("原版 Minecraft", "JavaLoaderVanilla");
        }
        else if (ActiveCatalogLoader is { } addonKind && InstallCompatibility.IsAddon(addonKind))
        {
            InstallLoader addon = ActiveCatalogLoader.Value;
            string name = addon == InstallLoader.FabricApi ? "Fabric API" : addon == InstallLoader.OptiFabric ? "OptiFabric" : "QSL";
            if (_selectedInstallBuilds.GetValueOrDefault(addon) == version.Id)
            {
                _selectedInstallBuilds.Remove(addon); _selectedInstallAddons.Remove(name);
                if (addon == InstallLoader.OptiFabric) _selectedInstallBuilds.Remove(InstallLoader.OptiFine);
            }
            else { _selectedInstallBuilds[addon] = version.Id; _selectedInstallAddons.Add(name); }
            UpdateJavaInstallSubpageVisibility();
        }
        else
        {
            InstallLoader selected = ActiveCatalogLoader.Value;
            if (_selectedInstallBuilds.GetValueOrDefault(selected) == version.Id)
            {
                _selectedInstallBuilds.Remove(selected);
                if (selected == InstallLoader.Fabric) { _selectedInstallAddons.Remove("OptiFabric"); _selectedInstallBuilds.Remove(InstallLoader.OptiFabric); _selectedInstallBuilds.Remove(InstallLoader.OptiFine); _selectedInstallBuilds.Remove(InstallLoader.FabricApi); _selectedInstallAddons.Remove("Fabric API"); }
                if (selected == InstallLoader.Quilt) { _selectedInstallBuilds.Remove(InstallLoader.Qsl); _selectedInstallAddons.Remove("QSL"); }
                InstallLoader? remaining = _selectedInstallBuilds.Keys.Where(kind => !InstallCompatibility.IsAddon(kind)).Cast<InstallLoader?>().FirstOrDefault();
                _selectedInstallLoader = remaining is { } kind ? JavaInstallSubpages.First(p => ParseInstallLoader(p.Loader) == kind).Loader! : "原版 Minecraft";
                UpdateJavaInstallSubpageVisibility();
                _catalogRevision = -1;
                return true;
            }
            foreach (InstallLoader other in _selectedInstallBuilds.Keys.Where(other => !InstallCompatibility.CanCombine(selected, other, _selectedInstallVersion)).ToArray())
                _selectedInstallBuilds.Remove(other);
            _selectedInstallBuilds[selected] = version.Id;
            JavaInstallSubpage page = JavaInstallSubpages.First(p => p.PageKey == _activeJavaInstallPage);
            SelectInstallLoader(page.Loader!, JavaInstallLoaderKeys.First(k => k == page.PageKey.Replace("Page", "", StringComparison.Ordinal).Replace("Java", "JavaLoader", StringComparison.Ordinal)));
        }
        _catalogRevision = -1;
        return true;
    }
    private Dictionary<InstallLoader, InstallCatalogVersion> SelectedInstallCatalogVersions()
    {
        var catalogs = (_store.ReadAppliedValue(_store.Resolve(InstallCatalogService.StateKey)) as InstallCatalogState)?.Catalogs;
        return _selectedInstallBuilds.ToDictionary(pair => pair.Key, pair => catalogs?
            .FirstOrDefault(item => item.Loader == pair.Key && item.GameVersion == _selectedInstallVersion)?
            .Versions.FirstOrDefault(version => version.Id == pair.Value) ?? new InstallCatalogVersion(pair.Value, ""));
    }
    private void ProjectInstallCatalog()
    {
        if (_shell.Stage.Navigation.Current != _javaInstallPage || _installCatalogCommands is null) return;
        XsrUiEntityId inputEntity = _javaInstallEntities["JavaInstallVersionInput"];
        XsrUiTextInput input = _shell.Tree.GetComponent<XsrUiTextInput>(inputEntity)!;
        input.Placeholder = _installGameChosen ? "版本名称" : "搜索版本";
        _shell.Tree.GetComponent<XsrUiSemantic>(inputEntity)!.Label = input.Placeholder;
        RequestInstallCatalog();
        var catalogState = _store.ReadAppliedValue(_store.Resolve(InstallCatalogService.StateKey)) as InstallCatalogState;
        if (catalogState is not null && catalogState.Revision != _catalogStateRevision)
        {
            _catalogStateRevision = catalogState.Revision;
            _catalogRevision = -1;
            foreach (string key in _catalogCache.Keys.ToArray())
            {
                var cached = _catalogCache[key];
                if (catalogState.Catalogs.Any(item => item.Loader == cached.Loader && item.GameVersion == cached.GameVersion && item.Revision > cached.Revision))
                    _catalogCache.Remove(key);
            }
        }
        string cacheKey = CurrentCatalogKey;
        InstallCatalogSnapshot? snapshot = _catalogCache.GetValueOrDefault(cacheKey);
        if (snapshot is null)
        {
            snapshot = (_store.ReadAppliedValue(_store.Resolve(InstallCatalogService.StateKey)) as InstallCatalogState)?.Catalogs
                .FirstOrDefault(item => item.Loader == ActiveCatalogLoader && (item.Loader is null || item.GameVersion == _selectedInstallVersion));
            if (snapshot is null || snapshot.Loader != ActiveCatalogLoader
                || snapshot.Loader is not null && snapshot.GameVersion != _selectedInstallVersion) return;
            if (!snapshot.Loading && snapshot.Error is null)
            {
                if (_catalogCache.Count >= 24) _catalogCache.Clear();
                _catalogCache[cacheKey] = snapshot;
            }
        }
        if (!_catalogHosts.TryGetValue(_activeJavaInstallPage, out XsrUiEntityId host)) return;
        XsrUiScroll scroll = _shell.Tree.GetComponent<XsrUiScroll>(_javaInstallEntities[_activeJavaInstallPage])!;
        string query = !_installGameChosen && snapshot.Loader is null ? input.ReadDraft().Trim() : "";
        if (query != _catalogSearch)
        {
            _catalogSearch = query; _filteredRevision = -1; _catalogRevision = -1;
            if (snapshot.Loader is null) scroll.OffsetY = 0;
        }
        if (snapshot.Loader is null && query.Length > 0)
        {
            if (_filteredRevision != snapshot.Revision)
            {
                _filteredGames = snapshot.Versions.Where(version => version.Id.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
                _filteredRevision = snapshot.Revision;
            }
            snapshot = snapshot with { Versions = _filteredGames };
        }
        if (_locateCatalogSelection && !snapshot.Loading)
        {
            _locateCatalogSelection = false;
            string? selected = snapshot.Loader is null ? _installGameChosen ? _selectedInstallVersion : null
                : _selectedInstallBuilds.GetValueOrDefault(snapshot.Loader.Value);
            int selectedIndex = -1;
            if (selected is not null)
                for (int index = 0; index < snapshot.Versions.Count; index++)
                    if (snapshot.Versions[index].Id == selected) { selectedIndex = index; break; }
            if (selectedIndex >= 0)
            {
                _shell.Renderer.FinishScrollInertia(_javaInstallEntities[_activeJavaInstallPage]);
                scroll.OffsetY = Math.Max(0, selectedIndex * 50 - Math.Max(0, _shell.Renderer.Viewport.Height - 240) / 2);
                _catalogRevision = -1;
            }
        }
        int first = Math.Clamp((int)(Math.Max(0, scroll.OffsetY - 48) / 50) - 4, 0, Math.Max(0, snapshot.Versions.Count - 1));
        int count = Math.Min(snapshot.Versions.Count - first, (int)Math.Ceiling(_shell.Renderer.Viewport.Height / 50) + 10);
        if (snapshot.Revision == _catalogRevision && first == _catalogFirst && count == _catalogCount) return;
        _catalogRevision = snapshot.Revision; _catalogFirst = first; _catalogCount = count;
        // A fixed-height window bounds ECS/native controls independently of catalog length.
        // Keep overlapping rows alive, preserving focus and avoiding repeated entry animations.
        var wanted = snapshot.Versions.Skip(first).Take(count).Select(v => v.Id).ToHashSet(StringComparer.Ordinal);
        Dictionary<string, XsrUiEntityId> retained = [];
        foreach (XsrUiEntityId child in _shell.Tree.Children(host).ToArray())
        {
            if (_catalogRows.TryGetValue(child, out InstallCatalogVersion? old) && wanted.Contains(old.Id))
            { retained[old.Id] = child; _shell.Tree.Detach(child); }
            else { _catalogRows.Remove(child); _shell.Tree.Destroy(child); }
        }
        void Spacer(string name, double height)
        {
            if (height <= 0) return;
            XsrUiEntityId spacer = _shell.Tree.Create(name);
            _shell.Tree.SetComponent(spacer, new XsrUiElement { Height = height });
            _shell.Tree.Attach(spacer, host);
        }
        Spacer("CatalogBefore", first * 50);
        var selection = SelectedInstallCatalogVersions();
        string message = snapshot.Loading ? "正在获取版本…" : snapshot.Unsupported ?? (snapshot.Error is not null ? "获取失败，请重试。" : snapshot.Versions.Count == 0 ? (query.Length > 0 ? "没有匹配的版本。" : "此游戏版本暂无兼容版本。") : snapshot.Versions[0].Warning is not null ? "部分来源获取失败，点击重试。" : "");
        if (message.Length == 0 && snapshot.Loader is { } noticeLoader && snapshot.Versions.Count > 0)
            message = InstallCompatibility.BuildNotice(noticeLoader, selection.GetValueOrDefault(noticeLoader) ?? snapshot.Versions[0], selection) ?? "";
        XsrUiEntityId status = _catalogStatus[_activeJavaInstallPage];
        _shell.Tree.GetComponent<XsrUiText>(status)!.Content = message;
        _shell.Tree.GetComponent<XsrUiElement>(status)!.IsVisible = message.Length > 0;
        foreach (InstallCatalogVersion version in snapshot.Versions.Skip(first).Take(count))
        {
            string key = snapshot.Loader is null ? "game:" + version.Id : "loader:" + version.Id;
            bool selected = _installGameChosen && (snapshot.Loader is null ? version.Id == _selectedInstallVersion : _selectedInstallBuilds.GetValueOrDefault(snapshot.Loader.Value) == version.Id);
            string? conflict = snapshot.Loader is { } kind ? InstallCompatibility.BuildConflict(kind, version, selection) : null;
            string detail = conflict ?? (snapshot.Loader is not null ? version.Detail : version.Stable ? "正式版" : "测试版");
            PxmlIrNode Project(PxmlIrNode node) => node with
            {
                Key = node.Key + ":" + key,
                Label = node.Key == "CatalogRow" ? (selected ? "已选择 " : "选择 ") + version.Id : node.Label,
                Content = node.Key switch { "CatalogName" => version.Id, "CatalogDetail" => detail, _ => node.Content },
                Children = [.. node.Children.Select(Project)],
            };
            XsrUiEntityId row;
            if (retained.TryGetValue(version.Id, out row)) _shell.Tree.Attach(row, host);
            else
            {
                row = PxmlUiLoader.Load(new(Project(CatalogRowTemplate.Root)), _shell.Tree, _store, host);
                _shell.Tree.GetComponent<XsrUiElement>(row)!.Margin = new XsrUiThickness(0, 0, 0, 2);
            }
            _shell.Tree.GetComponent<XsrUiSemantic>(row)!.Label = (selected ? "取消选择 " : "选择 ") + version.Id;
            _shell.Tree.GetComponent<XsrUiInput>(row)!.Enabled = selected || conflict is null;
            _catalogRows[row] = version;
            _shell.Tree.SetComponent(row, new XsrUiSelection { IsSelected = selected });
            ApplyVisual(row, selected ? ProfileSurface : XsrUiColor.Transparent, PrimaryText, XsrUiCornerRadii.Inset, hover: PickerBackground);
            _shell.Tree.Walk(row, entity =>
            {
                string name = _shell.Tree.Name(entity);
                if (name.StartsWith("CatalogDetail", StringComparison.Ordinal)) _shell.Tree.GetComponent<XsrUiText>(entity)!.Content = detail;
                StyleText(entity, name.StartsWith("CatalogDetail", StringComparison.Ordinal) ? SecondaryText : PrimaryText, 14);
                if (name.StartsWith("CatalogCheck", StringComparison.Ordinal)) ApplyVisual(entity, XsrUiColor.Transparent, selected ? BadgeText : XsrUiColor.Transparent, 0);
                return true;
            });
        }
        Spacer("CatalogAfter", (snapshot.Versions.Count - first - count) * 50);
        _shell.Tree.MarkDirty(_javaInstallPage, XsrUiDirtyKinds.Layout | XsrUiDirtyKinds.Paint);
    }
}
