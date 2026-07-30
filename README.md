# Test Station Monitor

A desktop application that monitors and controls a set of remote "test station" processes over a pluggable communication transport (named pipes or TCP). Built with WPF and .NET, structured to keep the UI, the communication layer, and the shared protocol cleanly separated.

The monitor connects to one or more independent station processes, sends them commands, and reflects their status in real time. The transport (how the monitor talks to a station) sits behind an interface, so named pipes and TCP are fully interchangeable. A station is addressed with a pipe name or an `ip:port`, and the right transport is chosen automatically.

---

## Overview

Each **test station** is a standalone console process that simulates a piece of test equipment. It waits for a connection, then responds to commands: on `RUN` it reports `RUNNING`, performs a simulated test, and reports `PASSED` or `FAILED`.

The **monitor** is a WPF application. For each station it connects to, it sends commands (run a single station, or run all of them) and updates the UI live as status messages arrive from the station. Because each station runs in its own process, tests execute concurrently and report back independently.

The point of the project is the **design**: the communication mechanism is abstracted so the transport can be swapped without touching the UI or the application logic, and every external dependency is injected, which makes the view models unit-testable without any real I/O.

---

## Project structure

| Project | Purpose |
|---|---|
| `TestStationMonitor` | The WPF desktop application (view models, views, UI). Depends only on the shared protocol abstraction, not on any concrete transport. |
| `TestStation.Cli` | The console process that acts as a test station. Hosts a pipe or TCP server depending on how it's launched. |
| `TestStation.Protocol` | The shared contract used by both sides: the status enum, the command constants, the `IConnection` abstraction, its pipe and TCP implementations, and the factory that chooses between them. |
| `TestStation.Tests` | MSTest unit tests for the view models, using fake implementations of the injected dependencies. |

Keeping the protocol in its own library means the monitor and the station can't drift on the command vocabulary or status values. There is one definition, referenced by both.

---

## Key design points

**Transport behind an interface.** Communication is defined by `IConnection` (connect, send, message-received event, disconnected event). There are two implementations — `PipesConnection` and `TcpConnection` — that share their send and receive logic entirely; they differ only in how the connection is established and torn down. A factory inspects the endpoint (`ip:port` vs. a plain name) and returns the appropriate implementation. Everything above the interface is unaware of which transport is in use; switching a station between pipes and TCP is a matter of how it's addressed, and no application code changes.

**Dependency injection for testability.** The station view model depends on abstractions — the connection and a UI dispatcher — rather than concrete types. In the running application these are the real pipe/TCP connection and the WPF dispatcher; in tests they're lightweight fakes. This lets the view model's behaviour be verified deterministically, with no real processes, sockets, or UI thread involved.

**Asynchronous, non-blocking communication.** The monitor listens for status messages on a background read loop and marshals updates back to the UI thread, so the interface stays responsive while stations run. Status changes are driven by messages arriving from the station rather than assumed locally — the station is the source of truth for its own state.

---

## Requirements

- .NET 10 SDK (the WPF application targets `net10.0-windows` and runs on Windows)

---

## Running it

Build the whole solution:

```bash
dotnet build
```

### Start one or more stations

Each station is launched with an endpoint that also determines its transport.

Named pipe (endpoint is a pipe name):

```bash
dotnet run --project TestStation.Cli Station1
```

TCP (endpoint is `ip:port`):

```bash
dotnet run --project TestStation.Cli 127.0.0.1:5000
```

Run several in separate terminals, each with a unique name or port:

```bash
dotnet run --project TestStation.Cli Station1
dotnet run --project TestStation.Cli Station2
dotnet run --project TestStation.Cli 127.0.0.1:5000
```

### Start the monitor

```bash
dotnet run --project TestStationMonitor
```

In the monitor, add a station by entering the same endpoint used to launch it (the pipe name, or the `ip:port`). Once connected, use the per-station **Run** button to run a single test, or **Run All** to run every idle station concurrently. Status updates as each station reports back.

### Run the tests

```bash
dotnet test
```


---

## Notes

- A station currently serves a single connection for its lifetime; addressing, framing (newline-delimited messages), and the command set are intentionally simple to keep the focus on structure.
- The TCP transport is intended for local testing against the loopback address; a station meant to accept connections from another machine would bind to all interfaces rather than loopback.
