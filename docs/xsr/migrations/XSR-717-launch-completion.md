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
