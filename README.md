# Cadence

Time-blocking routine manager for Windows. Define your day as fixed blocks (Wake, Work, Gaming, Sleep, …), attach tasks to blocks, and get notified when blocks change.

> `v0.1` CLI + background worker. No UI yet.

## Install

Requires **.NET 8 Runtime** (framework-dependent build).

```bat
publish.bat
```

Copy `publish\Cadence\` anywhere, it contains `cadence.exe` + `Cadence.Worker.exe` and works from any folder. Or run from source:

```bat
cadence.bat <command>
```

## Use

**One-shot** (from any terminal, inside `publish\Cadence\`):

```bat
cadence.exe status
cadence.exe add "Sketch layout" --container "Art" --priority High
cadence.exe complete 3
cadence.exe modify 3 "Sketch final" --priority Normal
cadence.exe delete 3
cadence.exe containers
cadence.exe start
cadence.exe heartbeat
cadence.exe stop
```

Add `publish\Cadence\` to `PATH` to use `cadence` without `.exe`.

**Interactive** — double-click `cadence.exe` to open a `cadence>` prompt. Same commands, plus `help`, `clear`, `exit`.

| Command | What it does |
|---|---|
| `status` | Current block + its tasks |
| `add` | Add a task (defaults to the current block if no `--container`) |
| `complete` | Mark a task done |
| `modify` | Change a task's title and/or priority |
| `delete` | Delete a task |
| `containers` | All blocks with pending counts; flags tasks in unknown containers |
| `start` / `stop` | Start/stop the background worker (its own window) |
| `heartbeat` | Check if the worker is alive |

## Where your data lives

`%LOCALAPPDATA%\Cadence\`

- `cadence.db` tasks and history
- `routine.json` your editable copy of the routine (see below)
- `worker.pid` worker process id


## Editing your routine

The default is `Cadence.Infrastructure\Routines\default.json` (also embedded in the build). At runtime the app prefers `%LOCALAPPDATA%\Cadence\routine.json` seeded from the default on first run. Edit that file to change block times/labels, then restart the worker (`stop` → `start`).

Times are `HH:mm` (24h). Each routine needs exactly one `wake` block (first) and one `sleep` block (last); start times must be unique.
