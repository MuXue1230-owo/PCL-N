# XSR-719 Pointer focus lifetime and Windows startup

Page replacement may destroy the currently focused entity before the next native pointer
event. Pointer handling now discards that stale generation before accessing its components;
clicking empty space still blurs a live search field. The renderer owns this lifecycle rule,
with no service or backend-specific workaround.

The desktop output uses `WinExe`, selecting the Windows GUI subsystem for both the managed
apphost and NativeAOT publication. Launching from Explorer does not allocate a console window.
Redirected validation output and the existing persistent launcher log remain available.

Regression coverage exercises live-field blur, destruction of the focused field, and pointer
events before and after scene rebuilding. Validation also checks the published PE subsystem.
The architecture gate requires `WinExe` for Desktop and retains `Exe` for console tools.
