using PCL.Services.Tasks;
using PCL.Xsr;
using PCL.Xsr.State;

namespace PCL.Services.Tests;

// XSR-723: task center capability contract — registration, progress aggregation, stage
// advance monotonicity, cancellation, dismissal, and the published summary. Everything runs
// on one built store so the collection/summary cells behave exactly as the renderer sees them.
internal static partial class Program
{
    private static TaskCenterService NewTaskCenter(out XsrStateStore store)
    {
        XsrStateStoreBuilder builder = new();
        TaskCenterStateContract.DeclareState(builder);
        store = builder.Build();
        return new TaskCenterService(store);
    }

    private static ValueTask TaskCenterTracksLifecycleAndSummary()
    {
        TaskCenterService service = NewTaskCenter(out XsrStateStore store);
        XsrStateId entries = store.Resolve(TaskCenterStateContract.EntriesKey);
        XsrStateId summary = store.Resolve(TaskCenterStateContract.SummaryKey);

        using ITaskCenterTask install = service.Begin(new TaskCenterStart(
            "install:1", "安装 Minecraft 1.20.1", ["版本信息", "游戏文件", "完成"]));
        install.Report("游戏文件", "下载中", 0.5, 5, 10, 1024);
        using ITaskCenterTask skin = service.Begin(new TaskCenterStart(
            "download:1", "下载皮肤", ["下载文件"], CanCancel: false));

        TaskCenterSummary live = (TaskCenterSummary)store.ReadAppliedValue(summary)!;
        AssertEqual(2, live.ActiveCount);
        AssertEqual(2, live.VisibleCount);
        AssertTrue(Math.Abs(live.Progress - 0.25) < 0.0001, "summary averages active progress");
        AssertEqual(1024L, live.SpeedBytesPerSecond);
        AssertEqual(5, live.RemainingFiles);

        XsrCollectionSnapshot<TaskCenterEntry> snapshot = store.ReadCollection<TaskCenterEntry>(entries);
        AssertEqual(2, snapshot.Items.Count);
        TaskCenterEntry installEntry = snapshot.Items.Single(entry => entry.TaskId == "install:1");
        AssertEqual(TaskCenterEntryState.Running, installEntry.State);
        AssertEqual(3, installEntry.Steps!.Count);
        AssertEqual(TaskCenterEntryState.Finished, installEntry.Steps[0].State);
        AssertEqual(TaskCenterEntryState.Running, installEntry.Steps[1].State);

        install.Complete("安装完成");
        live = (TaskCenterSummary)store.ReadAppliedValue(summary)!;
        AssertEqual(1, live.ActiveCount);
        TaskCenterEntry finished = store.ReadCollection<TaskCenterEntry>(entries)
            .Items.Single(entry => entry.TaskId == "install:1");
        AssertTrue(TaskCenterEntryState.Finished == finished.State && 1d == finished.Progress,
            "finished entry is complete");
        AssertEqual(TaskCenterEntryState.Finished, finished.Steps![2].State);

        // Terminal entries stay visible until dismissed; dismissal removes them.
        AssertTrue(service.Dismiss("install:1"), "terminal entry dismisses");
        AssertFalse(service.Dismiss("download:1"));
        AssertEqual(1, store.ReadCollection<TaskCenterEntry>(entries).Items.Count);
        return ValueTask.CompletedTask;
    }

    private static ValueTask TaskCenterCancelRoutesToOwnerToken()
    {
        TaskCenterService service = NewTaskCenter(out XsrStateStore store);
        using ITaskCenterTask task = service.Begin(new TaskCenterStart(
            "install:2", "安装 Minecraft 1.20.1", ["游戏文件"]));
        AssertFalse(task.CancellationToken.IsCancellationRequested);

        AssertTrue(service.RequestCancel("install:2"), "cancel accepted");
        AssertTrue(task.CancellationToken.IsCancellationRequested, "owner token fired");

        task.Canceled();
        TaskCenterEntry entry = store.ReadCollection<TaskCenterEntry>(
            store.Resolve(TaskCenterStateContract.EntriesKey)).Items.Single();
        AssertEqual(TaskCenterEntryState.Canceled, entry.State);
        return ValueTask.CompletedTask;
    }

    private static ValueTask TaskCenterStagesNeverMoveBackwards()
    {
        TaskCenterService service = NewTaskCenter(out XsrStateStore store);
        using ITaskCenterTask task = service.Begin(new TaskCenterStart(
            "install:3", "安装", ["版本信息", "游戏文件", "附加组件"]));
        task.Report("游戏文件", "half", 0.5, 1, 2, 0);
        // A later phase reusing the generic "下载文件" name must not rewind the step list.
        task.Report("下载文件", "addon", 0.1, 0, 9, 0);
        TaskCenterEntry entry = store.ReadCollection<TaskCenterEntry>(
            store.Resolve(TaskCenterStateContract.EntriesKey)).Items.Single();
        AssertEqual(TaskCenterEntryState.Finished, entry.Steps![0].State);
        AssertEqual(TaskCenterEntryState.Running, entry.Steps[1].State);
        AssertEqual(TaskCenterEntryState.Waiting, entry.Steps[2].State);
        return ValueTask.CompletedTask;
    }

    private static ValueTask TaskCenterAbandonedHandlesSurfaceAsFailed()
    {
        TaskCenterService service = NewTaskCenter(out XsrStateStore store);
        XsrStateId entries = store.Resolve(TaskCenterStateContract.EntriesKey);
        ITaskCenterTask task = service.Begin(new TaskCenterStart("install:4", "安装", []));
        task.Dispose();
        TaskCenterEntry entry = store.ReadCollection<TaskCenterEntry>(entries).Items.Single();
        AssertEqual(TaskCenterEntryState.Failed, entry.State);
        AssertEqual("任务意外结束。", entry.ErrorMessage);

        // The abandoned registration released its id: reuse replaces the failed card with a
        // fresh running one under the same id (one card, not two).
        using ITaskCenterTask reused = service.Begin(new TaskCenterStart("install:4", "安装", []));
        TaskCenterEntry reentered = store.ReadCollection<TaskCenterEntry>(entries).Items.Single();
        AssertEqual(TaskCenterEntryState.Running, reentered.State);
        AssertEqual(null, reentered.ErrorMessage);
        return ValueTask.CompletedTask;
    }

    private static ValueTask TaskCenterClearFinishedLeavesActive()
    {
        TaskCenterService service = NewTaskCenter(out XsrStateStore store);
        ITaskCenterTask first = service.Begin(new TaskCenterStart("a", "a", []));
        using ITaskCenterTask second = service.Begin(new TaskCenterStart("b", "b", []));
        first.Complete();
        AssertEqual(1, service.ClearFinished());
        AssertEqual(1, store.ReadCollection<TaskCenterEntry>(
            store.Resolve(TaskCenterStateContract.EntriesKey)).Items.Count);
        second.Fail("boom");
        AssertEqual(TaskCenterEntryState.Failed, store.ReadCollection<TaskCenterEntry>(
            store.Resolve(TaskCenterStateContract.EntriesKey)).Items.Single().State);
        return ValueTask.CompletedTask;
    }
}
