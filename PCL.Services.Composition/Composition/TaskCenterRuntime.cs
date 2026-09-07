using PCL.Services.Foundation;
using PCL.Services.Tasks;
using PCL.Xsr.Runtime;

namespace PCL.Services.Composition;

public sealed class TaskCenterRuntime(XsrCommandRouter commands) : IDisposable
{
    public XsrCommandRouter Commands { get; } = commands;
    public void Dispose() { }
}

public static class TaskCenterRuntimeComposer
{
    public static TaskCenterRuntime Compose(FoundationHost host, IXsrDispatchObserver? observer = null)
    {
        XsrCommandRouterBuilder commands = new();
        commands.Register<TaskCenterCancelCommand>(TaskCenterRoutes.Cancel,
            (command, _) => ValueTask.FromResult(host.Tasks.RequestCancel(command.TaskId)
                ? PCL.Xsr.XsrResult.Success()
                : PCL.Xsr.XsrResult.Failure(PCL.Xsr.XsrRuntimeErrors.TargetNotFound("任务不存在或已结束。"))));
        commands.Register<TaskCenterDismissCommand>(TaskCenterRoutes.Dismiss,
            (command, _) => ValueTask.FromResult(host.Tasks.Dismiss(command.TaskId)
                ? PCL.Xsr.XsrResult.Success()
                : PCL.Xsr.XsrResult.Failure(PCL.Xsr.XsrRuntimeErrors.TargetNotFound("任务不存在或已结束。"))));
        var dispatchObserver = observer ?? new Observer();
        return new TaskCenterRuntime(commands.Build(dispatchObserver));
    }

    private sealed class Observer : IXsrDispatchObserver
    {
        public void OnCompleted(XsrDispatchObservation observation) { }
    }
}

public sealed record TaskCenterCancelCommand(string TaskId);
public sealed record TaskCenterDismissCommand(string TaskId);
