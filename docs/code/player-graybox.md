# Player graybox

## Goal

Move and look in 3rd person on a flat test scene. **No combat, no lock-on, no animator.** Input only when `RunCoordinator.State == FloorActive`.

**Asmdef:** `AF.Player` → references `AF.Core`, `Unity.InputSystem`.

---

## Prerequisite

- `RunCoordinator.Oestroy` renamed to `OnDestroy` (if not done yet).
- Title screen working (or call `NewRun()` manually to reach `FloorActive`).

---

## Files

```
Assets/_Project/Player/
├── AF.Player.asmdef                       ← parent folder: covers Input + Runtime
├── Input/
│   └── PlayerInputActions.inputactions    ← create in Editor (steps below)
└── Runtime/
    ├── PlayerIntent.cs
    ├── PlayerInputAdapter.cs
    ├── PlayerMotor.cs
    ├── PlayerCameraRig.cs
    └── PlayerControlGate.cs
```

---

## Step 1 — Input Actions asset (Editor)

Unity does not hand-edit `.inputactions` JSON well. Create it in the Editor:

1. Folder: `Assets/_Project/Player/Input/`
2. **Create → Input Actions** → name `PlayerInputActions`
3. Open asset. One map: **Gameplay**

| Action | Type | Bindings |
|--------|------|----------|
| **Move** | Value / Vector2 | WASD composite, Left Stick |
| **Look** | Value / Vector2 | Mouse delta, Right Stick |
| **Dodge** | Button | Space, South (A) — stub, not used yet |

**Move bindings (2D Vector Composite):**
- Up → W, Down → S, Left → A, Right → D
- Also: Gamepad Left Stick

**Look bindings:**
- Mouse → Delta
- Gamepad Right Stick

4. Asset Inspector → **Generate C# Class** ✓
5. Class name: `PlayerInputActions`, namespace: `AF.Player`, path: same `Input/` folder
6. **Save Asset** → allow Unity to generate `PlayerInputActions.cs`

---

### `Assets/_Project/Player/AF.Player.asmdef`

```json
{
    "name": "AF.Player",
    "rootNamespace": "AF.Player",
    "references": [
        "AF.Core",
        "Unity.InputSystem"
    ],
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

### `Assets/_Project/Player/Runtime/PlayerIntent.cs`

```csharp
using UnityEngine;

namespace AF.Player
{
    public struct PlayerIntent
    {
        public Vector2 Move;
        public Vector2 Look;
        public bool Dodge;
    }
}
```

### `Assets/_Project/Player/Runtime/PlayerInputAdapter.cs`

```csharp
using UnityEngine;

namespace AF.Player
{
    /// <summary>Reads Input System → PlayerIntent. Enable/disable via ControlGate.</summary>
    public sealed class PlayerInputAdapter : MonoBehaviour
    {
        PlayerInputActions _actions;
        bool _isEnabled;

        public PlayerIntent Intent { get; private set; }

        void Awake()
        {
            _actions = new PlayerInputActions();
        }

        void OnDestroy()
        {
            _actions?.Dispose();
        }

        void Update()
        {
            if (!_isEnabled)
            {
                Intent = default;
                return;
            }

            Intent = new PlayerIntent
            {
                Move = _actions.Gameplay.Move.ReadValue<Vector2>(),
                Look = _actions.Gameplay.Look.ReadValue<Vector2>(),
                Dodge = _actions.Gameplay.Dodge.WasPressedThisFrame()
            };
        }

        public void SetInputEnabled(bool enabled)
        {
            if (_isEnabled == enabled)
            {
                return;
            }

            _isEnabled = enabled;

            if (enabled)
            {
                _actions.Gameplay.Enable();
            }
            else
            {
                _actions.Gameplay.Disable();
                Intent = default;
            }
        }
    }
}
```

### `Assets/_Project/Player/Runtime/PlayerMotor.cs`

```csharp
using UnityEngine;

namespace AF.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputAdapter))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] float _moveSpeed = 5f;
        [SerializeField] float _gravity = -20f;

        CharacterController _controller;
        PlayerInputAdapter _input;
        float _verticalVelocity;
        bool _isEnabled = true;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<PlayerInputAdapter>();
        }

        void Update()
        {
            if (!_isEnabled)
            {
                return;
            }

            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            Vector2 moveInput = _input.Intent.Move;
            Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);

            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            if (direction.sqrMagnitude > 0.01f && Camera.main != null)
            {
                Transform cam = Camera.main.transform;
                Vector3 forward = cam.forward;
                forward.y = 0f;
                forward.Normalize();

                Vector3 right = cam.right;
                right.y = 0f;
                right.Normalize();

                direction = forward * direction.z + right * direction.x;
            }

            _verticalVelocity += _gravity * Time.deltaTime;

            Vector3 velocity = direction * _moveSpeed;
            velocity.y = _verticalVelocity;

            _controller.Move(velocity * Time.deltaTime);
        }

        public void SetMotorEnabled(bool enabled)
        {
            _isEnabled = enabled;
        }
    }
}
```

### `Assets/_Project/Player/Runtime/PlayerCameraRig.cs`

Lives on **Main Camera**. References the player's `PlayerInputAdapter` in Inspector.

```csharp
using UnityEngine;

namespace AF.Player
{
    public sealed class PlayerCameraRig : MonoBehaviour
    {
        [SerializeField] Transform _target;
        [SerializeField] PlayerInputAdapter _input;
        [SerializeField] float _distance = 4f;
        [SerializeField] float _height = 2f;
        [SerializeField] float _lookSensitivity = 0.15f;
        [SerializeField] float _minPitch = -30f;
        [SerializeField] float _maxPitch = 60f;

        float _yaw;
        float _pitch = 15f;
        bool _isEnabled = true;

        void LateUpdate()
        {
            if (!_isEnabled || _target == null || _input == null)
            {
                return;
            }

            Vector2 look = _input.Intent.Look;
            _yaw += look.x * _lookSensitivity;
            _pitch -= look.y * _lookSensitivity;
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, _height, -_distance);
            Vector3 focus = _target.position + Vector3.up * 1.5f;

            transform.position = focus + offset;
            transform.LookAt(focus);
        }

        public void SetCameraEnabled(bool enabled)
        {
            _isEnabled = enabled;
        }
    }
}
```

### `Assets/_Project/Player/Runtime/PlayerControlGate.cs`

```csharp
using AF.Core;
using UnityEngine;

namespace AF.Player
{
    /// <summary>Player control only during FloorActive. Locks cursor in gameplay.</summary>
    public sealed class PlayerControlGate : MonoBehaviour
    {
        [SerializeField] RunCoordinator _runCoordinator;
        [SerializeField] PlayerInputAdapter _input;
        [SerializeField] PlayerMotor _motor;
        [SerializeField] PlayerCameraRig _cameraRig;

        RunState _lastState;

        void Update()
        {
            if (_runCoordinator == null)
            {
                return;
            }

            RunState state = _runCoordinator.State;
            if (state == _lastState)
            {
                return;
            }

            _lastState = state;
            bool gameplay = state == RunState.FloorActive;

            _input.SetInputEnabled(gameplay);
            _motor.SetMotorEnabled(gameplay);
            _cameraRig.SetCameraEnabled(gameplay);

            Cursor.lockState = gameplay ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !gameplay;
        }
    }
}
```

---

## Unity setup

### 1. Graybox scene

1. Duplicate or create scene: `Assets/_Project/Scenes/Graybox.unity`
2. **3D Object → Plane** (scale 5,5,5) — name `Ground`
3. Add to **Build Settings**

### 2. RunCoordinator dungeon scene

On **Run** (`RunCoordinator`):

- **Dungeon Scene** → `Graybox` (exact scene name)
- **Main Menu Scene** → leave empty if title + run live in same boot scene, or set boot scene name

### 3. Player prefab / hierarchy

Create **Player** in `Graybox` scene:

```
Player                          ← Y = 1 so capsule sits on plane
├── CharacterController         height 2, radius 0.35, center (0, 1, 0)
├── PlayerInputAdapter
├── PlayerMotor
├── PlayerControlGate
└── (optional) Visual — Capsule child for sighting
```

**PlayerControlGate** Inspector:

| Field | Assign |
|-------|--------|
| Run Coordinator | **Run** (DontDestroyOnLoad object from boot scene) |
| Input | self `PlayerInputAdapter` |
| Motor | self `PlayerMotor` |
| Camera Rig | **Main Camera** `PlayerCameraRig` |

### 4. Main Camera

On **Main Camera** in Graybox:

1. Add **PlayerCameraRig**
2. **Target** → `Player` transform
3. **Input** → Player's `PlayerInputAdapter` (drag from Player object)

**PlayerControlGate** on Player → **Camera Rig** → Main Camera's `PlayerCameraRig`

### 5. Boot flow test

**Option A — two scenes (recommended):**

1. Boot scene: **Run** + **TitleScreen**
2. Graybox: **Player** + **Main Camera** + plane
3. `RunCoordinator.dungeonScene` = `Graybox`
4. Play boot → New Run → loads Graybox → move

**Option B — single scene:**

1. Everything in one scene; leave `dungeonScene` empty
2. Title hides on New Run; player already on plane
3. `State` becomes `FloorActive` → gate enables control

### 6. Project Input settings

**Edit → Project Settings → Player → Active Input Handling** → **Input System Package** (or Both).

---

## Controls (FloorActive only)

| Input | Action |
|-------|--------|
| WASD / Left stick | Move (camera-relative) |
| Mouse / Right stick | Look |
| Space / A | Dodge (read, not used yet) |

Cursor locked during `FloorActive`. Menu / title: cursor free.

---

## Verify

- [ ] `AF.Player.asmdef` compiles
- [ ] `PlayerInputActions.cs` generated in `Input/` folder
- [ ] Boot → Main Menu: player **cannot** move (gate off)
- [ ] New Run → `FloorActive`: player moves, camera orbits
- [ ] Return to menu / `MainMenu` state: control stops, cursor unlocks
- [ ] No console errors about missing `RunCoordinator` on gate
- [ ] Gamepad: left stick move, right stick look

---

## Troubleshooting

### CS0246: `PlayerInputActions` could not be found

**Cause:** `AF.Player.asmdef` was inside `Runtime/` only. Generated `PlayerInputActions.cs` in `Input/` compiled into **Assembly-CSharp**. Custom assemblies cannot reference Assembly-CSharp.

**Fix:** Put `AF.Player.asmdef` on **`Assets/_Project/Player/`** (parent of both `Input/` and `Runtime/`). Let Unity recompile.

Optional: on the `.inputactions` asset, set **C# Class Namespace** to `AF.Player` and click **Apply** to regenerate.

### Wrong constructor in `PlayerInputAdapter`

Use `new PlayerInputActions()` — not `new PlayerInput()`.

---

## Known limitations (intentional)

- No sprint, jump, dodge movement
- No animator blend tree
- `Camera.main` used by motor — only one camera in scene
- No player spawn from `RunCoordinator` — player placed in scene manually (spawn helper later)

---

## Next delivery

`docs/code/dungeon-slice.md` — port simplified `DungeonLayoutSolver`, seed from `RunSession`.
