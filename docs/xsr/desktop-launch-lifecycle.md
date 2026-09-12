# Desktop launch lifecycle follow-up

Read together with architecture-updated.md, capability-fabric-updated.md and sidecar-protocol-updated.md.

Host Services own Java selection, runtime acquisition, process supervision and crash classification. Desktop projects sealed state and dispatches commands; it never inspects a Process or classifies log text. No Sidecar or remote analysis dependency is introduced.

- Java major selection is checked against the pending requirement. An installed matching runtime wins; otherwise a supported runtime is acquired after the explicit version choice. Stale choices cannot resolve another launch.
- Redirected process output is continuously drained with bounded retention. Abnormal exit triggers asynchronous Service analysis; normal exit and user cancellation do not. Analysis reports are retained per session in sealed collection state.
- The task capsule has one continuous input owner, including its expanded area. Process controls address a specific session; logs remain an unavailable entry.
- The selected launching instance exposes a disabled launch action with progress. Version rows place the selected check before the name and expose modify, settings and delete actions.

Validation: Desktop input/navigation regressions, Service process/Java contracts, architecture checks, trim and NativeAOT shell smoke.
