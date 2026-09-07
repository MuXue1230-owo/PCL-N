using PCL.Services.Tasks;
using PCL.UI.Next;
using PCL.Xsr;
using PCL.Xsr.State;

namespace PCL.Desktop.Ui;

/// <summary>Desktop-owned presentation cells for the task bubble.</summary>
internal static class TaskBubbleState
{
    internal const string OwnerName = "PCL.Desktop.TaskBubble";

    /// <summary>Wake revision: timer and controller threads ask for a frame through this cell.</summary>
    public static readonly XsrSemanticId Revision = XsrSemanticId.Parse("ui.tasks.bubble.revision");

    public static void DeclareState(XsrStateStoreBuilder builder) => builder.Cell<long>(Revision, OwnerName);
}

/// <summary>
/// Render-thread projection of the task center summary into the bottom-right progress
/// bubble. The bubble is a persistent shell-level overlay: a round button whose translucent
/// fill rises from the bottom with aggregated task progress (parity with the legacy extra
/// dock's task bubble, hidden while the task page is open). All tree mutations happen at
/// <see cref="XsrUiRenderer.FramePreparing"/>; worker threads only publish the wake cell.
/// </summary>
internal sealed class DesktopTaskBubblePresenter : IDisposable
{
    private const double BubbleSize = 44;
    private const double DockInset = 18;
    private static readonly TimeSpan ExitSettle = TimeSpan.FromMilliseconds(360);
    private static readonly XsrSemanticId OpenCommand = XsrSemanticId.Parse("ui.tasks.open");

    private readonly XsrUiShell _shell;
    private readonly XsrStateStore _store;
    private readonly XsrStateId _summaryId;
    private readonly XsrStateId _wakeState;
    private readonly XsrUiEntityId _root;
    private readonly XsrUiEntityId _fill;
    private readonly XsrUiOverlayMotion _motion;
    private readonly TimeProvider _timeProvider;
    private long _wakeRevision;
    private volatile bool _exitPending;
    private ITimer? _exitTimer;
    private bool _pageVisible;
    private bool _closing;
    private int _lastAnnouncedPercent = -1;
    private bool _disposed;

    public DesktopTaskBubblePresenter(XsrUiShell shell, XsrStateStore store, TimeProvider? timeProvider = null)
    {
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _summaryId = store.Resolve(TaskCenterStateContract.SummaryKey);
        _wakeState = store.Resolve(TaskBubbleState.Revision);

        // Children of a plain element overlap the full content rect in attach order, so the
        // fill sits below the centered icon inside the same round button.
        _root = shell.Tree.Create("task-bubble");
        shell.Tree.SetComponent(_root, new XsrUiElement
        {
            Width = BubbleSize,
            Height = BubbleSize,
            Margin = new XsrUiThickness(0, 0, DockInset, DockInset),
            HorizontalAlignment = XsrUiAlignment.End,
            VerticalAlignment = XsrUiAlignment.End,
            IsVisible = false,
        });
        shell.Tree.SetComponent(_root, new XsrUiInput { Focusable = true, Clickable = true });
        shell.Tree.SetComponent(_root, new XsrUiCommandBinding(OpenCommand));
        shell.Tree.SetComponent(_root, new XsrUiSemantic(XsrUiSemanticRole.Button, "打开任务中心"));
        _motion = new XsrUiOverlayMotion(XsrUiOverlayMotionKind.Notification);
        shell.Tree.SetComponent(_root, _motion);
        shell.Tree.SetComponent(_root, Style(
            background: new XsrUiColor(30, 36, 46, 235),
            foreground: new XsrUiColor(255, 255, 255),
            border: new XsrUiColor(255, 255, 255, 36),
            cornerRadius: BubbleSize / 2,
            borderWidth: 1,
            hover: new XsrUiColor(255, 255, 255, 26)));

        _fill = shell.Tree.Create("task-bubble-fill");
        shell.Tree.Attach(_fill, _root);
        shell.Tree.SetComponent(_fill, new XsrUiElement { Width = BubbleSize, Height = BubbleSize });
        shell.Tree.SetComponent(_fill, new XsrUiProgress
        {
            Anchor = XsrUiProgressFillAnchor.Bottom,
        });
        shell.Tree.SetComponent(_fill, Style(
            background: new XsrUiColor(255, 255, 255, 64),
            foreground: XsrUiColor.Transparent,
            border: XsrUiColor.Transparent,
            cornerRadius: BubbleSize / 2));

        XsrUiEntityId icon = shell.Tree.Create("task-bubble-icon");
        shell.Tree.Attach(icon, _root);
        shell.Tree.SetComponent(icon, new XsrUiElement
        {
            Width = 20,
            Height = 20,
            HorizontalAlignment = XsrUiAlignment.Center,
            VerticalAlignment = XsrUiAlignment.Center,
        });
        shell.Tree.SetComponent(icon, new XsrUiImage("lucide/list-checks"));

        shell.Stage.Show(_root);
        _shell.Renderer.FramePreparing += OnFramePreparing;
    }

    internal XsrUiEntityId Root => _root;

    internal XsrUiEntityId FillEntity => _fill;

    /// <summary>Called by the task center controller: the bubble yields while the page is open.</summary>
    public void SetPageVisible(bool visible)
    {
        if (_pageVisible == visible)
        {
            return;
        }

        _pageVisible = visible;
        RequestFrame();
    }

    private void RequestFrame()
    {
        if (_disposed)
        {
            return;
        }

        long revision = Interlocked.Increment(ref _wakeRevision);
        try
        {
            _store.Publish(_wakeState, revision);
        }
        catch (ObjectDisposedException)
        {
            // Host teardown won the race with the controller thread.
        }
    }

    private void OnFramePreparing(object? sender, EventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        if (_exitPending)
        {
            _exitPending = false;
            _closing = false;
            _motion.IsClosing = false;
            SetVisible(false);
            SetEnabled(true);
        }

        TaskCenterSummary summary =
            (TaskCenterSummary?)_store.ReadAppliedValue(_summaryId) ?? new TaskCenterSummary(0, 0, 0, 0, 0);
        Reconcile(summary);
    }

    private void Reconcile(TaskCenterSummary summary)
    {
        bool wanted = summary.VisibleCount > 0 && !_pageVisible;
        if (wanted && !_closing && !IsVisible(_root))
        {
            SetVisible(true);
        }
        else if (!wanted && IsVisible(_root) && !_closing)
        {
            BeginClose();
        }

        // With no active task the bubble reads as filled (completion is acknowledged by the
        // user on the page, not by a timeout); with active work it follows aggregated progress.
        double target = summary.ActiveCount > 0 ? Math.Clamp(summary.Progress, 0d, 1d) : 1d;
        if (_shell.Tree.GetComponent<XsrUiProgress>(_fill) is { } fill)
        {
            fill.SetTarget(target);
            _shell.Tree.MarkDirty(_fill, XsrUiDirtyKinds.Layout);
        }

        int percent = (int)Math.Round(target * 100d);
        if (percent != _lastAnnouncedPercent
            && _shell.Tree.GetComponent<XsrUiSemantic>(_root) is { } semantic)
        {
            _lastAnnouncedPercent = percent;
            semantic.Label = summary.ActiveCount > 0
                ? $"任务进行中 {percent}%，打开任务中心"
                : "任务已完成，打开任务中心";
        }
    }

    private void BeginClose()
    {
        _closing = true;
        _motion.IsClosing = true;
        SetEnabled(false);
        _shell.Tree.MarkDirty(_root, XsrUiDirtyKinds.Layout | XsrUiDirtyKinds.Paint);
        if (_shell.Renderer.ReducedMotion)
        {
            _exitPending = false;
            _closing = false;
            _motion.IsClosing = false;
            SetVisible(false);
            SetEnabled(true);
            return;
        }

        _exitTimer?.Dispose();
        _exitTimer = _timeProvider.CreateTimer(
            static state =>
            {
                DesktopTaskBubblePresenter owner = (DesktopTaskBubblePresenter)state!;
                owner._exitPending = true;
                owner.RequestFrame();
            },
            this,
            ExitSettle,
            Timeout.InfiniteTimeSpan);
    }

    private void SetVisible(bool visible)
    {
        XsrUiElement element = _shell.Tree.GetComponent<XsrUiElement>(_root) ?? new XsrUiElement();
        if (element.IsVisible == visible)
        {
            return;
        }

        element.IsVisible = visible;
        _shell.Tree.SetComponent(_root, element);
        _shell.Tree.MarkDirty(_root, XsrUiDirtyKinds.Layout | XsrUiDirtyKinds.Paint);
    }

    private void SetEnabled(bool enabled)
    {
        if (_shell.Tree.GetComponent<XsrUiInput>(_root) is { } input)
        {
            input.Enabled = enabled;
        }
    }

    private bool IsVisible(XsrUiEntityId entity) =>
        (_shell.Tree.GetComponent<XsrUiElement>(entity) ?? new XsrUiElement()).IsVisible;

    private static XsrUiVisualStyle Style(
        XsrUiColor background, XsrUiColor foreground, XsrUiColor border, double cornerRadius,
        double borderWidth = 0, XsrUiColor hover = default) => new()
        {
            Surface = background.Alpha == 0 ? XsrUiSurfaceKind.None : XsrUiSurfaceKind.Solid,
            Background = background,
            Foreground = foreground,
            Border = border,
            BorderWidth = borderWidth,
            Hover = hover,
            CornerRadius = cornerRadius,
        };

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _shell.Renderer.FramePreparing -= OnFramePreparing;
        _exitTimer?.Dispose();
        if (_shell.Tree.IsAlive(_root))
        {
            _shell.Tree.Destroy(_root);
        }
    }
}
