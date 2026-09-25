# AGENTS.md

## Project

Cadence is a .NET 8 time-blocking / daily routine management system. Solution-level build with 5 projects following Clean Architecture.

## Commands

```bash
dotnet build Cadence.sln
dotnet test Cadence.Tests
```

Run a single test class:

```bash
dotnet test Cadence.Tests --filter "FullyQualifiedName~RoutineClockTests"
```

Or use the convenience wrapper: `cadence.bat <command>` (prefers `publish/Cadence/cadence.exe` when present, else `dotnet run`).

Publish a portable folder with `publish.bat` (both exes into `publish/Cadence/` — copy it anywhere).

No linter, formatter, or CI config exists. No codegen or migrations yet.

## Solution Layout

| Project | Role | Dependencies |
|---|---|---|
| `Cadence.Core` | Domain models, interfaces, scheduling logic | None |
| `Cadence.Infrastructure` | EF Core SQLite persistence, JSON routine loading | Core |
| `Cadence.Worker` | Background service, RuleEngine host, DI wiring | Core, Infrastructure |
| `Cadence.Cli` | Interactive CLI — no args opens a `cadence>` prompt (`help`, `exit`/`quit`, `clear`); with args it runs one command and exits. Output exe is `cadence.exe` via `<AssemblyName>` | Core, Infrastructure |
| `Cadence.Tests` | xUnit tests | Core, Infrastructure |

Core targets `net8.0`. Worker, Cli, Infrastructure, and Tests target `net8.0-windows10.0.17763`. All enable `<Nullable>` and `<ImplicitUsings>`.

## Architecture Rules

- **Cadence.Core has zero external dependencies.** Never add NuGet packages or project references to Core.
- **Infrastructure implements Core interfaces** (`ICadenceStore`, `IRoutineSource`, etc.).
- **Worker and Cli are application shells** that wire up Core + Infrastructure.
- **Infrastructure depends on `Microsoft.WindowsAppSDK`** for toast notifications (Windows-only).

## DI Lifetime Rationale

- **`RuleEngine` is singleton** because it holds mutable state (`_lastBlockLabel`, `_lastCycleId`, `_initialized`) that must survive across ticks. If scoped, each tick gets a fresh instance and the `_initialized` flag resets — block transitions are never detected.
- **`CadenceDbContext` is singleton** because SQLite is a file-based, single-writer database. No client-server connection pool, no concurrent scope conflicts. This is safe for a single-threaded background worker + CLI.
- **`ICadenceStore` is singleton** to match the DbContext lifetime it wraps.
- **Migration path:** If Cadence ever becomes a web app or multi-threaded service, switch `RuleEngine` to scoped and inject `IServiceScopeFactory` to resolve `ICadenceStore` per tick. The constructor signature does not change — only the DI registration and worker scope management change.

## Rule Engine

`RuleEngine` (`Cadence.Core/Scheduling/RuleEngine.cs`) is a stateful class — not a service — that detects block transitions and fires notifications.

- **Cold start:** First `TickAsync()` call snapshots current state silently. No retroactive catch-up for missed transitions.
- **Wake suppression:** Transitions into `BlockRole.Wake` never fire notifications (product decision — waking is biological, not software).
- **Notification flow:** `BlockTransition` always fires on block change. `TaskSurfaced` fires only when pending tasks exist for the active container. `CycleRoll` is logged (not sent) when the day rolls over.
- **State update before side effects:** `_lastBlockLabel` and `_lastCycleId` are updated *before* `SendAsync` to prevent double-fire on crash.
- **Requires `TaskStatus` alias** in any file that also imports async LINQ.

`RuleEngineWorker` (`Cadence.Worker/RuleEngineWorker.cs`) is a thin `BackgroundService` that calls `TickAsync()` every 30 seconds with a try/catch to survive transient failures. It also calls `RecordHeartbeatAsync` every tick.

## Domain Model Gotchas

- `RoutineClock` enforces exactly **one Wake block and one Sleep block**. Wake must have the earliest offset from itself (it anchors the day); Sleep must be last. Duplicate start times throw.
- **CycleId rolls at Wake, not midnight.** Past-midnight blocks belong to the previous day's cycle. See `Cadence.Core/Scheduling/RoutineClock.cs:86-94`.
- `TaskStatus` conflicts with `System.Threading.Tasks.TaskStatus`. Any file importing both Core models and async LINQ must alias it: `using TaskStatus = Cadence.Core.Models.TaskStatus;`
- `TaskPriority` enum (`Low`, `Normal`, `High`) is set via CLI `--priority` flag and affects store ordering.
- `TaskItem.DueAt` is an optional `TimeOnly?` due-time.

## Testing

- Tests use **SQLite in-memory** (`DataSource=:memory:`) via a shared `SqliteConnection`. Each test class creates its own connection and context; no shared fixture.
- `CadenceDbContext.Database.EnsureCreated()` is called in test setup — no migration step required.
- Global `using Xunit;` is declared in `Cadence.Tests.csproj`.

## DI & Testing Patterns

- `SystemClock : IClock` exists for production. Tests use a `FakeClock` that returns a fixed `DateTimeOffset`.
- `RuleEngine` tests use **hand-written mocks** (no Moq). Mock classes are `private sealed` inner classes: `MockRoutineSource`, `MockCadenceStore`, `MockNotificationSender`, `FakeClock`.
- `RuleEngine` is registered as `AddSingleton` (not transient) because it holds mutable state (`_lastBlockLabel`, `_lastCycleId`).
- `INotificationSender` is implemented by `WindowsToastNotificationSender` (Windows App SDK toasts) in production. `ConsoleNotificationSender` (`Cadence.Infrastructure/Notifications/NotificationSender.cs`) is registered separately as its concrete type.

## Routine File Format

`JsonRoutineLoader` reads JSON with `{ "profile": "...", "blocks": [...] }`. Block times use 24-hour `HH:mm` format. Enum values are camelCase strings (e.g., `"wake"`, `"sleep"`).

Source file is `Cadence.Infrastructure/Routines/default.json` (embedded resource). At runtime `LoadDefault()` prefers `%LOCALAPPDATA%\Cadence\routine.json` (seeded from embedded on first run, editable without rebuild), then embedded, then a loose `Routines/default.json` next to the .exe. **Preserved but inactive in v0.2.x** — v0.2.x uses `cycles.json` + `areas.json` for the new cycle system. v0.3 will redesign parallel block system with `timetable.json`.

## Config vs State Separation

- **`Cadence.Infrastructure/Routines/default.json`** is Configuration as Code — edited in VS Code, version-controlled, defines the block schedule (times, labels, roles). **Preserved but inactive in v0.2.x**.
- **`%LOCALAPPDATA%\Cadence\cycles.json` + `areas.json`** — v0.2.x cycle system config (themes, areas, hot-reload).
- **`%LOCALAPPDATA%\Cadence\cadence.db`** is mutable runtime state — tasks, notification logs, **heartbeats**. The CLI writes here only.
- **CLI never touches config at runtime.** Routine edits go to `%LOCALAPPDATA%\Cadence\routine.json`; cycle edits go to `cycles.json` + `areas.json`; hot-reload picks them up (v0.2).

## CLI Command Surface

REPL and one-shot share `CommandParser.RunAsync` — same parsing, same behavior. REPL-only: `exit`/`quit`, `clear`. REPL tokenizer (`SplitCommandLine`) is quote-aware but has no escaped-quote support.

| Command | Syntax | Description |
|---|---|---|
| `status` | `status` | Show current block, cycle ID, and pending tasks |
| `add` | `add "Title" --container "Label" [--priority Low\|Normal\|High]` | Add a task (defaults to current block if no `--container`) |
| `complete` | `complete [Id]` | Mark task as completed; shows pending tasks if no ID given |
| `modify` | `modify [Id] "New Title"` | Modify a task's title; shows tasks if no ID given |
| `delete` | `delete [Id]` | Delete a task by its ID |
| `containers` | `containers` | List all blocks with pending counts + orphan detection |
| `start` | `start` | Launch background worker (sibling exe) |
| `stop` | `stop` | Kill background worker via PID file |
| `heartbeat` | `heartbeat` | Check if worker is alive (last tick ≤ 33s ago) |

## Containers Command Design

The `containers` command merges two data sources:
- **Blocks** come from `IRoutineSource.Blocks` (in-memory, loaded from `default.json` at startup)
- **Task counts** come from `ICadenceStore.GetContainerTaskCountsAsync()` (one SQL `GROUP BY` query)

Blocks are **never** inserted into SQLite. They are Configuration as Code — the JSON file is the single source of truth. The CLI joins the two sources in memory to produce the output. Orphan detection finds task `ContainerLabel` values that don't match any block in the routine.

## Worker Liveness Pattern

`RuleEngineWorker` writes a `Heartbeat` record (singleton row, `WorkerId = 1`) to SQLite every tick (30s). The CLI `heartbeat` command reads `LastTickAt` and compares against `DateTimeOffset.Now` — if ≤ 33s (interval + 3s grace), worker is alive. If no heartbeat row exists, worker has never ticked.

## Worker Process Model

- **`cadence start`** launches the sibling `Cadence.Worker.exe` (same folder) in its own console window. Dev fallback: `dotnet run --project` when no published exe is present. `start` refuses if the PID points at a live process; stale PIDs are cleaned.
- **`cadence stop`** reads `CadenceDB/worker.pid`, calls `Process.Kill()`, deletes PID file.
- **Ctrl+C** in the Worker's console window triggers graceful shutdown via Generic Host cancellation. `OperationCanceledException` is caught; PID file deleted in `finally` block.
- **Terminal close** kills the Worker's console window.

## Shared Data Directory

CLI and Worker share `%LOCALAPPDATA%\Cadence\` (`cadence.db`, `worker.pid`, `routine.json`) via `CadencePaths` (`Cadence.Infrastructure/CadencePaths.cs`) — the single source of truth. No source-tree lookup, so the published folder works from anywhere.

First run auto-copies the legacy solution-root `CadenceDB/cadence.db` if present (one-time, best-effort). `ServiceCollectionExtensions.GetCadenceDbDirectory()` is obsolete and delegates to `CadencePaths`.

## Known Debt (residual)

- `CommandParser.FindWorkerProject()` source walk-up is now dev-fallback only (used when no sibling `Cadence.Worker.exe` exists).
- `CadencePaths.FindLegacyDbPath()` still contains a walk-up, but it is a one-time read-only migration check — not a runtime dependency.
- `WindowsToastNotificationSender` is untested unpackaged; if toasts fail from the published folder, plan is a custom in-app notification surface in the UI update (`INotificationSender` seam makes this a swap, not a rewrite).

## Naming

The repo directory is `Cadence`. The solution and namespaces use `Cadence` without the suffix.

## Roadmap (agent context — keep current when implementing)

### v0.2.x (next — Cycle + Area Foundation)
- Spectre.Console in `Cadence.Cli` (tables, styled output, interactive prompts). Core stays zero-dep.
- `ICycleSource` + `CycleStore` replace `IRoutineSource` + routine loader.
- `CycleEngine` replaces `RuleEngine` — ticks only during active cycle.
- Spectre.Console CLI: `cycle start/end/pause/resume/status/rename`, `area list/create/archive`, `task add/complete/delete` (area-scoped), `routine` (themes + areas).
- Hot-reload: `FileSystemWatcher` on `cycles.json` + `areas.json`.
- CI: `.github/workflows/ci.yml`. Demo GIF.

### v0.3 (candidate — Guardrails + Block System Design)
- Pomodoro timer / half-cycle prompt (optional, in-cycle).
- Idle-gap nudge (configurable threshold, default 60min, Snooze/Dismiss).
- Cycle attendance heatmap, weekly review CLI (`cadence review`).
- **Block system redesign** — parallel timetable system (fixed blocks: Sleep, Meds, Appointments) alongside cycles. Unified notification queue. Separate config (`timetable.json`), CLI (`cadence block ...`), task scope. Decision: v0.4 impl or v1.0 merge.

### v1.0 — first packaged release with UI
- GUI: Blazor or Tauri (leaning Tauri). Tray icon + global hotkey.
- Cycle + Area visual editor (drag-and-drop themes, assign areas, manage areas).
- Live cycle view (timer, areas, tasks, progress).
- Installer.

### Backlog (v1.x+)
- ntfy.sh mobile push (HTTP POST → phone push when PC is on).
- Weekly review dashboard (heatmap, completion rates, actual vs target).
- `.ics` calendar import.
- Cloud sync (`cadence.db` + `cycles.json` + `areas.json`).
