using PCL.Services.Minecraft.Install;
using PCL.UI.Next;
namespace PCL.Desktop.Tests;
internal static partial class Program
{
    private sealed class LargeInstallSource : IInstallCatalogSource
    {
        private int _loaderReads;
        public int LoaderReads => Volatile.Read(ref _loaderReads);
        public TaskCompletionSource<int> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<IReadOnlyList<InstallCatalogVersion>> Loader { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<IReadOnlyList<InstallCatalogVersion>> GetGamesAsync(CancellationToken token) => Task.FromResult<IReadOnlyList<InstallCatalogVersion>>(
            Enumerable.Range(0, 10000).Select(i => new InstallCatalogVersion(i == 0 ? "1.20.1" : "fixture-" + i, "release")).ToArray());
        public Task<IReadOnlyList<InstallCatalogVersion>> GetLoadersAsync(InstallLoader loader, string game, CancellationToken token)
        { Interlocked.Increment(ref _loaderReads); Started.TrySetResult(Environment.CurrentManagedThreadId); return Loader.Task.WaitAsync(token); }
    }
    private static void InstallCatalogVirtualizesAndPrefetchesOffUiThread()
    {
        LargeInstallSource source = new();
        using LaunchPageFixture fixture = new(new ImmediateInstanceSource([]), installSource: source);
        fixture.Shell.Renderer.ReducedMotion = true;
        Emit(fixture.Intents, "ui.navigation.download"); Emit(fixture.Intents, "ui.install.java");
        fixture.Controller.WaitUntilIdle().GetAwaiter().GetResult();
        XsrUiScene scene = fixture.Shell.Render(new(1024, 600));
        var host = FindEntity(fixture.Shell, "JavaMinecraftVersions");
        AssertTrue(fixture.Shell.Tree.Children(host).Count < 30);
        var search = FindEntity(fixture.Shell, "JavaInstallVersionInput");
        fixture.Shell.Renderer.Focus(search);
        fixture.Shell.Renderer.SetTextInputValue(search, "fixture-9999");
        scene = fixture.Shell.Render(new(1024, 600));
        AssertEqual("搜索版本", FindByKey(fixture.Shell, scene, "JavaInstallVersionInput").TextInput!.Value.Placeholder);
        AssertTrue(HasKey(fixture.Shell, scene, "CatalogRow:game:fixture-9999"));
        AssertFalse(HasKey(fixture.Shell, scene, "CatalogRow:game:1.20.1"));
        AssertEqual(search, fixture.Shell.Renderer.Focused);
        fixture.Shell.Renderer.SetTextInputValue(search, "");
        scene = fixture.Shell.Render(new(1024, 600));
        var row = FindByKey(fixture.Shell, scene, "CatalogRow:game:1.20.1");
        AssertTrue(row.SuppressEntryAnimation);
        int uiThread = Environment.CurrentManagedThreadId;
        AssertTrue(fixture.Shell.Renderer.Activate(row.Entity));
        scene = fixture.Shell.Render(new(1024, 600));
        AssertFalse(source.Loader.Task.IsCompleted);
        AssertTrue(source.Started.Task.Wait(TimeSpan.FromSeconds(5)));
        AssertTrue(uiThread != source.Started.Task.Result);
        AssertTrue(IsVisible(fixture.Shell, "JavaFabricTab"));
        AssertEqual("版本名称", FindByKey(fixture.Shell, scene, "JavaInstallVersionInput").TextInput!.Value.Placeholder);
        AssertEqual(row.Entity, FindByKey(fixture.Shell, scene, "CatalogRow:game:1.20.1").Entity);
        // Same game toggles off immediately and cancels its background generation.
        fixture.Shell.Renderer.Activate(row.Entity); scene = fixture.Shell.Render(new(1024, 600));
        AssertEqual(1, FindByKey(fixture.Shell, scene, "JavaInstallPager").Pager!.Value.PageCount);
        fixture.Shell.Renderer.Activate(row.Entity); fixture.Shell.Render(new(1024, 600));
        Emit(fixture.Intents, "ui.install.page.fabric");
        scene = fixture.Shell.Render(new(1024, 600));
        AssertTrue(source.LoaderReads > 0);
        AssertEqual("正在获取版本…", FindByKey(fixture.Shell, scene, "JavaFabricPageStatus").Text);
        AssertTrue(IsVisible(fixture.Shell, "JavaForgeTab"));
        source.Loader.SetResult([new("0.16.0", "Fabric")]);
        fixture.Controller.WaitUntilIdle().GetAwaiter().GetResult();
        int completedReads = source.LoaderReads;
        scene = fixture.Shell.Render(new(1024, 600));
        var loaderRow = FindByKey(fixture.Shell, scene, "CatalogRow:loader:0.16.0").Entity;
        fixture.Shell.Renderer.Activate(loaderRow); fixture.Shell.Render(new(1024, 600));
        AssertTrue(IsVisible(fixture.Shell, "JavaFabricApiTab"));
        fixture.Shell.Renderer.Activate(loaderRow); fixture.Shell.Render(new(1024, 600));
        AssertFalse(IsVisible(fixture.Shell, "JavaFabricApiTab")); AssertTrue(IsVisible(fixture.Shell, "JavaForgeTab"));
        Emit(fixture.Intents, "ui.install.page.minecraft"); fixture.Shell.Render(new(1024, 600));
        Emit(fixture.Intents, "ui.install.page.fabric"); scene = fixture.Shell.Render(new(1024, 600));
        AssertEqual(completedReads, source.LoaderReads);
        AssertEqual(loaderRow, FindByKey(fixture.Shell, scene, "CatalogRow:loader:0.16.0").Entity);
        Emit(fixture.Intents, "ui.install.page.minecraft"); scene = fixture.Shell.Render(new(1024, 600));
        var page = FindByKey(fixture.Shell, scene, "JavaMinecraftPage");
        double extent = page.Scroll!.Value.ContentHeight;
        AssertTrue(extent > 499000);
        AssertTrue(fixture.Shell.Renderer.PointerScroll(new(page.Rect.X + 50, page.Rect.Y + 100), 250000));
        scene = fixture.Shell.Render(new(1024, 600));
        AssertTrue(fixture.Shell.Tree.Children(host).Count < 30);
        AssertTrue(scene.Nodes.Any(node => node.Text is { } text && text.StartsWith("fixture-500", StringComparison.Ordinal)));
        AssertClose(extent, FindByKey(fixture.Shell, scene, "JavaMinecraftPage").Scroll!.Value.ContentHeight);
    }
    private sealed class FixtureInstallSource : IInstallCatalogSource
    {
        public Task<IReadOnlyList<InstallCatalogVersion>> GetGamesAsync(CancellationToken token) =>
            Task.FromResult<IReadOnlyList<InstallCatalogVersion>>([new("1.21.1", "release"), new("1.20.6", "release"), new("1.20.1", "release")]);
        public Task<IReadOnlyList<InstallCatalogVersion>> GetLoadersAsync(InstallLoader loader, string game, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<InstallCatalogVersion>>([new("fixture.1", loader.ToString()), new("fixture.2", loader.ToString())]);
    }
}
