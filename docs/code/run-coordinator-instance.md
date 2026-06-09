# RunCoordinator — single instance across scenes

## Goal

One `RunCoordinator` for the whole play session. The boot scene owns it (DontDestroyOnLoad). Graybox must **not** keep a second copy. Player/UI always talk to the instance that holds `State` and `Session`.

**No new abstractions** — static `Instance` + destroy duplicates in `Awake`.

---

## Root cause

| Scene | What happens |
|-------|----------------|
| Boot | `Run` DDOL → holds state after `NewRun()` |
| Graybox | Second `RunCoordinator` in hierarchy |
| Player `PlayerControlGate` | Inspector ref points at **graybox** copy (fresh `MainMenu`, seed 0) |

---

## Scene fix (do this first)

1. Open **Graybox** scene.
2. **Delete** any `Run` / `RunCoordinator` GameObject.
3. Only **Boot** (title) scene has `Run` + `RunCoordinator`.

`RunCoordinator.dungeonScene` still points to `Graybox` — that is correct.

---

## Code changes

### `Assets/_Project/Core/Runtime/RunCoordinator.cs`

Replace the full file with:

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AF.Core
{
    /// <summary>
    /// One per play session. Boot scene only — DontDestroyOnLoad.
    /// Duplicates in loaded scenes self-destruct in Awake.
    /// </summary>
    public sealed class RunCoordinator : MonoBehaviour
    {
        public static RunCoordinator Instance { get; private set; }

        [SerializeField] string mainMenuScene = "";
        [SerializeField] string dungeonScene = "";

        readonly RunStateMachine stateMachine = new();
        readonly RunSession session = new();

        public RunState State => stateMachine.State;
        public RunSession Session => session;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            stateMachine.StateEntered += OnStateEntered;
        }

        void OnDestroy()
        {
            stateMachine.StateEntered -= OnStateEntered;

            if (Instance == this)
            {
                Instance = null;
            }
        }

        void Start()
        {
            if (stateMachine.State == RunState.Boot)
            {
                stateMachine.GoTo(RunState.MainMenu);
            }
        }

        public void NewRun()
        {
            session.Begin(Random.Range(1, int.MaxValue));
            stateMachine.GoTo(RunState.RunStarting);
            stateMachine.GoTo(RunState.FloorActive);
        }

        public void NotifyFloorCleared()
        {
            stateMachine.GoTo(RunState.FloorCleared);
        }

        public void NotifyPlayerDied()
        {
            stateMachine.GoTo(RunState.PlayerDead);
        }

        public void FinishRun()
        {
            stateMachine.GoTo(RunState.RunEnded);
            ReturnToMenu();
        }

        public void ReturnToMenu()
        {
            stateMachine.GoTo(RunState.MainMenu);
        }

        void OnStateEntered(RunState state)
        {
            switch (state)
            {
                case RunState.MainMenu:
                    LoadSceneIfSet(mainMenuScene);
                    break;
                case RunState.FloorActive:
                    LoadSceneIfSet(dungeonScene);
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

**Changes vs before:**
- `Instance` + duplicate `Destroy`
- `Start` only transitions `Boot → MainMenu` (won’t reset state if duplicate somehow ran — belt and suspenders)
- `OnDestroy` clears `Instance` when the real one dies

---

### `Assets/_Project/Player/Runtime/PlayerControlGate.cs`

Resolve coordinator in `Awake` — no cross-scene Inspector wire needed.

```csharp
using AF.Core;
using UnityEngine;

namespace AF.Player
{
    public sealed class PlayerControlGate : MonoBehaviour
    {
        [SerializeField] PlayerInputAdapter input;
        [SerializeField] PlayerMotor motor;
        [SerializeField] PlayerCameraRig cameraRig;

        RunCoordinator runCoordinator;
        RunState lastState;

        void Awake()
        {
            runCoordinator = RunCoordinator.Instance;
        }

        void Update()
        {
            if (runCoordinator == null)
            {
                return;
            }

            RunState state = runCoordinator.State;
            if (state == lastState)
            {
                return;
            }

            lastState = state;
            bool gameplay = state == RunState.FloorActive;

            input.SetInputEnabled(gameplay);
            motor.SetMotorEnabled(gameplay);
            cameraRig.SetCameraEnabled(gameplay);

            Cursor.lockState = gameplay ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !gameplay;
        }
    }
}
```

**Inspector:** remove **Run Coordinator** field — only input, motor, camera rig.

---

### `Assets/_Project/UI/Runtime/TitleScreenPresenter.cs` (optional but consistent)

Boot scene can keep a serialized ref, but fallback avoids nulls:

```csharp
void Awake()
{
    document = GetComponent<UIDocument>();

    if (runCoordinator == null)
    {
        runCoordinator = RunCoordinator.Instance;
    }
}
```

Or drop `[SerializeField] RunCoordinator` entirely and use `RunCoordinator.Instance` in `OnNewRunClicked`.

---

## Verify

- [ ] Graybox scene has **no** `RunCoordinator`
- [ ] Boot scene has exactly one **Run** (DDOL)
- [ ] Play from **Boot** → New Run → Graybox loads
- [ ] Player moves (`FloorActive` + gate sees same seed as before scene load)
- [ ] In Play Mode, only one `Run` in hierarchy (DDOL, dontdestroyonload)
- [ ] If you accidentally add Run to Graybox again, duplicate is destroyed on load — still only one `Instance`

---

## Play from Graybox only (editor testing)

If Build Settings start scene is Graybox and it has no boot `Run`:

- Add a temporary `Run` to Graybox **or** always press Play from Boot scene.
- Jam rule: **Play from Boot** for real flow.
