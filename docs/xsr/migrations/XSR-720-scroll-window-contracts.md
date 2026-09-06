# XSR-720 Scroll viewport and optional window integration

Vertical indicators reserve 12 logical pixels on the right, independent of stack direction.
Measure and arrange use the same reduced child viewport; scroll extents include wrapped
content measured at that width. The indicator remains in the full container paint rectangle.

Windows AUMID uses source-generated COM and ComWrappers, never built-in COM/RCW. Property
store writes use REFPROPVARIANT and native-sized PROPVARIANT storage. Window integration is
optional host decoration: once the probe confirms the game window, nonfatal decoration
failures are logged and cannot change the Visible result or launch truth.

Forgetting the active library root delegates its single loading publication to BeginScanLocked.
Regression tests cover both stack orientations, wrapping/extent, callback failure isolation,
native property-store roundtrips, and the loading revision count.

Validation: 81 renderer, 57 Desktop, 221 Services and 7 Avalonia backend checks pass;
the architecture gate covers 29 projects and rejects built-in COM in Desktop sources.
The Windows NativeAOT test enumerates a real off-screen test window, assigns its AUMID,
and reads the property back through the generated interface. Both Windows CI architectures
run this test before packaging. Source-generated interop follows the
[Microsoft COM source-generation contract](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/comwrappers-source-generation).
