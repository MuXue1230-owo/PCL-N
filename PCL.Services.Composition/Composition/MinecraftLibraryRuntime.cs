using PCL.Services.Foundation;
using PCL.Services.Minecraft;
using PCL.Xsr.Runtime;

namespace PCL.Services.Composition;

public sealed class MinecraftLibraryRuntime(MinecraftLibraryService service, XsrCommandRouter commands) : IDisposable
{
    public XsrCommandRouter Commands { get; } = commands;
    public void Dispose() => service.Dispose();
}

public static class MinecraftLibraryRuntimeComposer
{
    public static MinecraftLibraryRuntime Compose(FoundationHost host, string defaultDirectory,
        IMinecraftInstanceSource? source = null, IXsrDispatchObserver? observer = null)
    {
        MinecraftLibraryService service = new(host.Settings, defaultDirectory, source ?? new MinecraftInstanceDiscovery(host.Logging));
        XsrCommandRouterBuilder commands = new();
        commands.Register<MinecraftLibraryRefreshCommand>(MinecraftLibraryRoutes.Refresh, async (_, token) => await service.RefreshAsync(token).ConfigureAwait(false));
        commands.Register<MinecraftLibraryDirectoryCommand>(MinecraftLibraryRoutes.Directory, async (command, token) =>
            await service.ChangeDirectoryAsync(command.Path, command.Add, token).ConfigureAwait(false));
        commands.Register<MinecraftLibraryForgetCommand>(MinecraftLibraryRoutes.Forget, async (command, token) =>
            await service.ForgetDirectoryAsync(command.Path, token).ConfigureAwait(false));
        commands.Register<MinecraftLibrarySelectCommand>(MinecraftLibraryRoutes.Select, (command, _) =>
            ValueTask.FromResult(service.SelectInstance(command.RootDirectory, command.InstanceId)));
        commands.Register<MinecraftLibraryRenameCommand>(MinecraftLibraryRoutes.Rename, (command, _) =>
            ValueTask.FromResult(service.RenameDirectory(command.Path, command.Name)));
        return new(service, commands.Build(observer ?? new Observer()));
    }
    private sealed class Observer : IXsrDispatchObserver { public void OnCompleted(XsrDispatchObservation observation) { } }
}
