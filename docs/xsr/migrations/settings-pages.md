# Compact global settings pages

The global Settings destination now opens its real PXML page instead of the migration placeholder. The nine categories and their final groups are projected from the sealed settings catalog query.

## Presentation

- A 152 px category rail, 38 px navigation rows, 44 px setting rows, 16 px outer insets and 14 px group spacing keep the existing small window useful. The content host's former extra padding is removed only while this page is active and restored on exit.
- Typography uses 14 px setting labels and 12 px secondary group labels, the existing shell accent, quiet white grouped surfaces and inset separators. There is no Hero or repeated page title.
- Category transitions use a zero-offset fade. Scroll uses the existing drag/inertia component and visible indicator, with no per-row entry animation. Each category remembers its current scroll position during the session.
- The developer preference uses a compact switch with an independently moving thumb. Adding developer groups preserves the triggering control's focus and scroll position. Window mode uses an anchored, dismissible menu, with a four-pixel gap and intrinsic height.
- Unsupported entries remain in their final groups with readable “尚未可用” status and no write intent. No new renderer or Avalonia dependency enters Services.

## Active settings

Only catalog entries with a verified consumer are enabled: global window width/height/mode, JVM/game arguments, and developer visibility. The first five preserve their existing launch-setting keys; developer visibility uses the new layered contract. Other stored legacy preferences are not enabled merely because their keys exist.

Edits dispatch through Foundation settings commands. Text drafts are not overwritten while focused and are persisted only by Apply; failure reports through the shared feedback surface. Service state revisions update the presented values. The UI does not evaluate inheritance, import files, or call SettingsService directly.

## Verification

Desktop regressions cover all nine categories, narrow-window geometry, command-to-persistence behavior, draft focus, developer-section continuity, unavailable rows, and anchored choice menu geometry/selection. Visual review uses the actual Avalonia scene surface rendered to a bitmap in an external headless probe, not a separate HTML mock-up. Standard architecture, formatting, native and trim checks still apply.
