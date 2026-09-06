using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace PCL.Desktop.Ui;

/// <summary>
/// Window-manager integration for the launched Minecraft process. The game window must be a
/// separate taskbar application — never grouped under the launcher — while keeping the game's
/// own (grass block) icon: the per-window AppUserModelID breaks the launcher group without
/// relabeling the window, which an icon-overriding launcher AUMID would do.
/// </summary>
internal static class MinecraftWindowIntegration
{
    /// <summary>
    /// Assigns a per-launch AppUserModelID to the game's top-level windows so the taskbar
    /// never groups them with the launcher. The property is written through the shell
    /// property store scoped to each game window, so the game keeps its own icon.
    /// </summary>
    public static void DetachGameWindows(int processId, string appId)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        DetachGameWindowsWindows(processId, appId);
    }

    [SupportedOSPlatform("windows")]
    private static void DetachGameWindowsWindows(int processId, string appId)
    {
        _ = EnumWindows((window, lParam) =>
        {
            _ = GameWindowOwnerPid(window, out uint owner);
            if (owner != (uint)processId || !IsWindowVisible(window) || GetWindowTextLength(window) == 0)
            {
                return true;
            }

            ApplyAppUserModelId(window, appId);
            return true;
        }, nint.Zero);
    }

    [SupportedOSPlatform("windows")]
    private static void ApplyAppUserModelId(nint window, string appId)
    {
        try
        {
            Guid storeGuid = new("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99");
            if (SHGetPropertyStoreForWindow(window, ref storeGuid, out nint storePtr) != 0 || storePtr == 0)
            {
                return;
            }

            IPropertyStore store = (IPropertyStore)Marshal.GetObjectForIUnknown(storePtr);
            try
            {
                if (PropVariantFromString(appId, out PropVariant variant) != 0)
                {
                    return;
                }

                try
                {
                    _ = store.SetValue(ref AppUserModelIdKey, variant);
                    _ = store.Commit();
                }
                finally
                {
                    _ = PropVariantClear(ref variant);
                }
            }
            finally
            {
                Marshal.Release(storePtr);
            }
        }
        catch (COMException)
        {
            // A game window that dies mid-write must not break the launch flow.
        }
    }

    private static PropertyKey AppUserModelIdKey = new(
        new Guid("9F4C285F-C90D-11D2-9D8B-2ED9F57BD72F"), 5);

    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyKey
    {
        public Guid FormatId;
        public int PropertyId;

        public PropertyKey(Guid formatId, int propertyId)
        {
            FormatId = formatId;
            PropertyId = propertyId;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct PropVariant
    {
        [FieldOffset(0)] public short ValueType;
        [FieldOffset(8)] public nint Pointer;
    }

    /// <summary>Minimal IPropertyStore surface: SetValue + Commit are all the AUMID write needs.</summary>
    [ComImport, Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        [PreserveSig] int GetCount(out int count);
        [PreserveSig] int GetAt(int index, out PropertyKey key);
        [PreserveSig] int GetValue(ref PropertyKey key, out PropVariant value);
        [PreserveSig] int SetValue(ref PropertyKey key, PropVariant value);
        [PreserveSig] int Commit();
    }

    [DllImport("propsys.dll")]
    private static extern int PropVariantFromString([MarshalAs(UnmanagedType.LPWStr)] string value, out PropVariant variant);

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PropVariant variant);

    [DllImport("shell32.dll")]
    private static extern int SHGetPropertyStoreForWindow(nint window, ref Guid interfaceId, out nint propertyStore);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc callback, nint lParam);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate bool EnumWindowsProc(nint window, nint lParam);

    [DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")]
    private static extern uint GameWindowOwnerPid(nint window, out uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint window);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(nint window);
}
