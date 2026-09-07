# XSR-717 Launch and interaction completion

This unit completes LittleSkin profile launch by reusing OAuth/Yggdrasil account services and
the existing Authlib Injector launch arguments. Provider credentials remain in Services. The
injector is downloaded through the verified artifact pipeline before process launch. Offline
and Microsoft behavior is preserved.

When compatible Java is absent, the shared dialog provides Cancel at the left and Select Java /
Automatic download at the right. Manual selection is validated by the existing Java locator and
requirement evaluator; cancelling a file picker keeps the launch decision pending.

UI.Next gains a backend-neutral anchored-overlay layout fact so directory dropdowns follow the
selector's presented bounds during resize and capsule motion. Background pointer presses clear
text focus. Caret blinking belongs to the native text presenter and stops on blur/detach.
Unmigrated destinations share an honest, deliberate placeholder design; they expose no fake
functional controls. The about card uses concise product copy.

Validation covers provider token separation, injector arguments, Java choice outcomes, dropdown
alignment/containment, input focus/caret lifecycle, and managed/architecture/AOT gates.


## Post-review behavior corrections (2026-09)

- **Success closes the launching page immediately.** The window confirmation (or its platform
  fallback) already passed, so the page pops on success — the user returns to the library while
  the game warms up. The previous behavior parked the narration on a 游戏已启动 card until the
  process session ended.
- **The legacy 补全文件 step is restored in `complete_files`.** Before the JVM starts, every
  file the plan references is verified on disk and repaired through the shared download engine:
  client jar, asset index, asset objects, and the full inheritance chain's libraries (natives
  included). A missing library used to kill the JVM before its window appeared, which surfaced
  as the opaque "JVM 在窗口出现前意外退出" failure. Existing files are never re-downloaded, so
  completion after a partial install resumes rather than restarts.
