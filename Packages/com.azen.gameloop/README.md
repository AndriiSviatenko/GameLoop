# Azen.GameLoop

Portable, DI-agnostic game-loop kernel for Unity 6.

One facade owns lifecycle state, multi-channel pause with reason stack, per-frame ticking and timers. No `Update()` scattered across MonoBehaviours.

| | |
|---|---|
| Package | `Packages/com.azen.gameloop` |
| Unity | 6000.0+ / URP |
| Dependencies | UniTask |
| Demo | Repository project: `Assets/Demo/Scenes/DemoScene` — keys Space, 1, 2, C, S, R |

Documentation: https://andriisviatenko.github.io/GameLoop/

## Install

**Package Manager (git URL).** The package depends on UniTask, so add the OpenUPM registry to `Packages/manifest.json` first:

```json
"scopedRegistries": [
  { "name": "package.openupm.com", "url": "https://package.openupm.com", "scopes": ["com.cysharp"] }
]
```

Then **Package Manager → + → Add package from git URL**:

```
https://github.com/AndriiSviatenko/GameLoop.git?path=Packages/com.azen.gameloop#v1.0.0
```

**Unity package.** Download `Azen.GameLoop-1.0.0.unitypackage` from the [latest release](https://github.com/AndriiSviatenko/GameLoop/releases/latest) and import it via **Assets → Import Package → Custom Package**. It lands in `Assets/Azen.GameLoop`. Install UniTask before importing.

## Usage

Bootstrap once:

```csharp
var driver = gameObject.AddComponent<GameLoopDriver>();
var loop = driver.Bootstrap(logger: DebugLogGameLoopLogger.Instance);
```

Register elements (any class implementing the marker interfaces), then start:

```csharp
loop.Add(new EnemyAI(), GameLoopChannels.Gameplay, GameLoopPriority.High);
await loop.StartAsync(destroyCancellationToken);
```

MonoBehaviour elements register themselves — inherit `GameLoopElement` and call `Configure(channel, priority)` in `Awake` if needed:

```csharp
public sealed class SpinningCube : GameLoopElement, IGameTickable
{
    private void Awake() => Configure(GameLoopChannels.Gameplay);
    public void Tick(float dt) => transform.Rotate(0f, 90f * dt, 0f);
}
```

`GameLoopElement` waits for an active loop if its `OnEnable` runs before bootstrap. Game lifecycle callbacks are not replayed: an element registered before `StartAsync` receives init/start, while an element spawned during Play receives ticks and future state transitions. Initialize spawned objects in their factory, constructor or `Awake`; `OnStartGame` represents the game-session transition, not Unity's component `Start`.

Lifecycle — implement only what you need:

```csharp
class EnemyAI : IGameElement, IGameTickable, IGameStartedListener, IGamePausedListener
{
    public UniTask OnStartGame(CancellationToken ct) { /* ... */ }
    public UniTask OnPauseGame(CancellationToken ct) { /* ... */ }
    public void Tick(float dt) { /* ... */ }
}
```

Pause:

```csharp
await loop.PauseAsync(ct);                       // global
await loop.PauseFor(PauseReason.Menu, ct);       // reason stack: game resumes
await loop.PauseFor(PauseReason.Dialog, ct);     // only when BOTH reasons are gone
await loop.ResumeFor(PauseReason.Menu, ct);      // custom: PauseReason.Custom("x")
await loop.PauseChannelAsync(GameLoopChannels.Gameplay, ct); // UI keeps ticking
loop.SetChannelTimeScale(GameLoopChannels.Gameplay, 0.25f);  // slow-mo
loop.SetChannelTickWhenPaused(GameLoopChannels.UI, true);    // UI/input can resume the game
```

Transitions (start, pause, resume, stop, restart, channel pause) are queued: a call made while another transition is awaiting its listeners runs after it. Calling a transition synchronously from inside a listener callback throws (or is ignored when `ThrowOnReentrancy = false`). A paused channel stays paused through a global resume; stop and restart clear channel pauses.

Timers advance only while the game and their channel are active. A channel timer uses the same channel time scale as its tick systems:

```csharp
var h = loop.Every(2f, SpawnEnemy, GameLoopChannels.Gameplay);
loop.Once(5f, () => Debug.Log("boo"));
loop.Cancel(h);
```

Restart / stop:

```csharp
await loop.RestartAsync(ct);  // stop callbacks -> reset -> init -> start
loop.Destroy();
```

## Project integration

Create one composition root, bootstrap one `GameLoopDriver`, register systems before `StartAsync`, and bind the same loop instance as both `IGameLoop` and `IGameManager` when using DI. Do not create a loop per system or per scene object.

Wiring examples are separate samples in Package Manager → **Azen GameLoop → Samples**:

| Sample | Compiles when |
|---|---|
| Manual Bootstrap | always |
| VContainer | `jp.hadashikick.vcontainer` is installed |
| Zenject | `com.svermeulen.extenject` is installed, or `AZEN_ZENJECT` is added to Scripting Define Symbols |

Each sample has its own asmdef with a define constraint, so importing one without its container does not break compilation.

`GameLoop.Active` exists for `GameLoopElement` auto-registration and editor diagnostics. Application code should prefer an injected `IGameLoop` reference.

## Editor

`Window → Azen → GameLoop Debugger` — live state, channels, counts, time scales.

## Layout

```
Runtime/
├── Core/           IGameLoop, IGameManager, GameState, PauseReason, channels, priorities, handles
├── Elements/       marker interfaces: tickers + lifecycle listeners
├── Loop/           GameLoop facade, GameStateMachine, GameLoopComposite, GameLoopGroup
├── Scheduling/     GameLoopScheduler, ScheduleHandle
├── Registration/   fluent registration and IGameLoop extensions
├── Unity/          GameLoopDriver, GameLoopElement, focus handler, Unity time/logger
└── Infrastructure/ ITimeProvider, IGameLoopLogger, NullGameLoopLogger
```

## License

MIT — see [LICENSE](LICENSE).
