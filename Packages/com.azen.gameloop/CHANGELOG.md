# Changelog

## [1.0.0] - 2026-09-12

### Added
- `GameLoop` facade: lifecycle state machine (`Off → Play ⇄ Pause → Stop`), restart, sub-state.
- Element interfaces for `Tick`, `FixedTick`, `LateTick`, `TickAsync`, throttled ticks and lifecycle callbacks.
- Channels (`Default`, `Gameplay`, `UI`, `Cutscene`, custom) with per-channel pause, time scale and tick-while-paused.
- Pause reasons: the game resumes only when every reason is removed.
- Scheduler (`Every`, `Once`, `Cancel`) that follows channel pause and time scale.
- Queued state transitions with reentrancy detection.
- `GameLoopDriver`, `GameLoopElement`, `GameLoopFocusHandlerBase`.
- GameLoop Debugger window and Profiler markers per channel and per system.
- Samples: Manual Bootstrap, VContainer, Zenject.
- EditMode test suite.
