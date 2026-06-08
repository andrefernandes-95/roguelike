# Core bootstrap

## Goal

Minimal run lifecycle for the jam: plain C# state + one MonoBehaviour that loads scenes and exposes public methods (`NewRun`, `NotifyPlayerDied`, etc.). No interfaces, no event bus, no ScriptableObject config.

**Asmdef:** `Rogue.Core` — no assembly references.

---

## Files

Create this folder structure:

```
Assets/_Project/Core/Runtime/
├── Rogue.Core.asmdef
├── RunState.cs
├── RunSession.cs
├── RunStateMachine.cs
└── RunCoordinator.cs
```

### `Assets/_Project/Core/Runtime/Rogue.Core.asmdef`

```json
{
    "name": "Rogue.Core",
    "rootNamespace": "Rogue.Core",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

### `Assets/_Project/Core/Runtime/RunState.cs`

```csharp
namespace Rogue.Core
{
    public enum RunState
    {
        Boot,
        MainMenu,
        RunStarting,
        FloorActive,
        Encounter,
        FloorCleared,
        PlayerDead,
        RunEnded
    }
}
```

### `Assets/_Project/Core/Runtime/RunSession.cs`

```csharp
namespace Rogue.Core
{
    /// <summary>Mutable data for the current run. Lives for one attempt.</summary>
    public sealed class RunSession
    {
        public int Seed { get; private set; }
        public int FloorIndex { get; private set; }

        public void Begin(int seed)
        {
            Seed = seed;
            FloorIndex = 0;
        }

        public void NextFloor()
        {
            FloorIndex++;
        }
    }
}
```

### `Assets/_Project/Core/Runtime/RunStateMachine.cs`

```csharp
using System;

namespace Rogue.Core
{
    /// <summary>Plain C# run lifecycle. No Unity references.</summary>
    public sealed class RunStateMachine
    {
        public RunState State { get; private set; } = RunState.Boot;

        public event Action<RunState> StateEntered;

        public void GoTo(RunState next)
        {
            if (State == next)
            {
                return;
            }

            State = next;
            StateEntered?.Invoke(next);
        }
    }
}
```

### `Assets/_Project/Core/Runtime/RunCoordinator.cs`

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rogue.Core
{
    /// <summary>
    /// Unity glue for the run lifecycle. Put on a DontDestroyOnLoad object in the boot scene.
    /// Other systems call public methods — no global event bus.
    /// </summary>
    public sealed class RunCoordinator : MonoBehaviour
    {
        [SerializeField] string _mainMenuScene = "";
        [SerializeField] string _dungeonScene = "";

        readonly RunStateMachine _stateMachine = new();
        readonly RunSession _session = new();

        public RunState State => _stateMachine.State;
        public RunSession Session => _session;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _stateMachine.StateEntered += OnStateEntered;
        }

        void OnDestroy()
        {
            _stateMachine.StateEntered -= OnStateEntered;
        }

        void Start()
        {
            _stateMachine.GoTo(RunState.MainMenu);
        }

        public void NewRun()
        {
            _session.Begin(Random.Range(1, int.MaxValue));
            _stateMachine.GoTo(RunState.RunStarting);
            _stateMachine.GoTo(RunState.FloorActive);
        }

        public void EnterEncounter()
        {
            _stateMachine.GoTo(RunState.Encounter);
        }

        public void ExitEncounter()
        {
            _stateMachine.GoTo(RunState.FloorActive);
        }

        public void NotifyFloorCleared()
        {
            _stateMachine.GoTo(RunState.FloorCleared);
        }

        public void NotifyPlayerDied()
        {
            _stateMachine.GoTo(RunState.PlayerDead);
        }

        public void FinishRun()
        {
            _stateMachine.GoTo(RunState.RunEnded);
            ReturnToMenu();
        }

        public void ReturnToMenu()
        {
            _stateMachine.GoTo(RunState.MainMenu);
        }

        void OnStateEntered(RunState state)
        {
            switch (state)
            {
                case RunState.MainMenu:
                    LoadSceneIfSet(_mainMenuScene);
                    break;
                case RunState.FloorActive:
                    LoadSceneIfSet(_dungeonScene);
                    break;
            }
        }

        void LoadSceneIfSet(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            if (SceneManager.GetActiveScene().name == sceneName)
            {
                return;
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}
```

---

## Unity setup

1. Create folder `Assets/_Project/Core/Runtime/` and add the five files above.
2. Let Unity import and compile. Fix any asmdef issues if prompted.
3. Open your boot scene (e.g. `SampleScene`).
4. Create empty GameObject named **Run**.
5. Add component **Run Coordinator** (`Rogue.Core.RunCoordinator`).
6. Leave **Main Menu Scene** and **Dungeon Scene** blank while working in a single scene.
7. Add scenes to **File → Build Settings** when you split menu/dungeon later.

### Optional: test from Inspector

While in Play Mode, you can call public methods via a tiny debug script or Unity Event — or wait for the title screen UI delivery.

---

## Public API (for other systems later)

| Method | When to call |
|--------|----------------|
| `NewRun()` | Title screen "New Run" button |
| `EnterEncounter()` / `ExitEncounter()` | Room combat start/end |
| `NotifyFloorCleared()` | Boss or floor exit reached |
| `NotifyPlayerDied()` | Player HP hits zero |
| `FinishRun()` | After death/summary screen — returns to menu |
| `ReturnToMenu()` | Quit run / back button |

Read-only: `State`, `Session.Seed`, `Session.FloorIndex`.

---

## Verify

- [ ] Project compiles with zero errors
- [ ] `Rogue.Core.asmdef` exists; no extra asmdef references
- [ ] Play boot scene → Inspector shows `State = MainMenu` on **Run**
- [ ] `NewRun()` in Play Mode → `State = FloorActive`, `Session.Seed` is non-zero
- [ ] Scene fields blank → no errors on play (no scene load attempted)

---

## Next delivery

Ask for `docs/code/title-screen.md` (UI Toolkit + `NewRun` wiring) or `docs/code/player-motor.md`.
