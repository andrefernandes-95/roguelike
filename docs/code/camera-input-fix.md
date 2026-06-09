# Camera input fix

## Goal

Fix look stopping while move still works. Root causes in your current code — not imaginary bugs.

---

## What’s going wrong

### 1. `PlayerControlGate` only runs on state **change**

```csharp
if (state == lastState) return;
```

If anything skips the first transition (null coordinator, wrong state on load), input/cursor never get configured. Also **cursor lock is set once** — press Escape / Alt-Tab / click outside → cursor unlocks → **WASD still moves, mouse look dies**. That matches “sometimes when I move.”

### 2. `RunCoordinator.Instance` cached once in `Awake`

If `Instance` is null that frame (load order), `runCoordinator` stays null forever — gate does nothing. Less likely in your boot flow, but cheap to harden.

### 3. Default `isEnabled` mismatch

| Component | Default | Result before gate runs |
|-----------|---------|-------------------------|
| `PlayerMotor` | `true` | ready |
| `PlayerCameraRig` | `true` | runs with zero look |
| `PlayerInputAdapter` | `false` | **Intent always zero** |

Gate must run before look works. Motor default should be `false` too.

### 4. Look action type should be **PassThrough** (Input System)

Your `Look` action is **Value**. Unity recommends **PassThrough** for `<Mouse>/delta`. Value can behave oddly frame-to-frame.

**Editor fix:** `PlayerInputActions` → **Look** → Action Type → **PassThrough** → Save → regenerate C# if prompted.

### 5. One sensitivity for mouse and stick

Mouse delta = pixels per frame (large numbers). Stick = -1..1 per frame. Both use `* 0.15f` in `PlayerCameraRig` → stick feels dead; mouse OK. Not the main “sometimes” bug, but fix while here.

---

## Files to change

### `PlayerInputActions.inputactions` (Editor)

1. Select **Look** action
2. **Action Type** → **PassThrough**
3. Save asset

---

### `Assets/_Project/Player/Runtime/PlayerControlGate.cs`

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

        RunState lastState = RunState.Boot;

        void Start()
        {
            ApplyState(GetCoordinator()?.State ?? RunState.Boot);
        }

        void Update()
        {
            RunCoordinator coordinator = GetCoordinator();
            if (coordinator == null)
            {
                return;
            }

            RunState state = coordinator.State;
            if (state != lastState)
            {
                ApplyState(state);
            }

            // Re-lock every frame during gameplay (Escape / Alt-Tab unlocks cursor)
            if (state == RunState.FloorActive)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void ApplyState(RunState state)
        {
            lastState = state;
            bool gameplay = state == RunState.FloorActive;

            input.SetInputEnabled(gameplay);
            motor.SetMotorEnabled(gameplay);
            cameraRig.SetCameraEnabled(gameplay);

            if (!gameplay)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        static RunCoordinator GetCoordinator()
        {
            return RunCoordinator.Instance;
        }
    }
}
```

---

### `Assets/_Project/Player/Runtime/PlayerMotor.cs`

Change default:

```csharp
bool isEnabled = false;  // was true
```

---

### `Assets/_Project/Player/Runtime/PlayerCameraRig.cs`

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace AF.Player
{
    public sealed class PlayerCameraRig : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] PlayerInputAdapter input;
        [SerializeField] float mouseSensitivity = 0.15f;
        [SerializeField] float stickSensitivity = 120f;
        [SerializeField] float distance = 4f;
        [SerializeField] float height = 2f;
        [SerializeField] float minPitch = -30f;
        [SerializeField] float maxPitch = 60f;

        float yaw;
        float pitch = 15f;
        bool isEnabled;

        void LateUpdate()
        {
            if (!isEnabled || target == null || input == null)
            {
                return;
            }

            Vector2 look = input.Intent.Look;
            if (look.sqrMagnitude < 0.0001f)
            {
                ApplyTransform();
                return;
            }

            if (IsGamepadLook())
            {
                yaw += look.x * stickSensitivity * Time.deltaTime;
                pitch -= look.y * stickSensitivity * Time.deltaTime;
            }
            else
            {
                yaw += look.x * mouseSensitivity;
                pitch -= look.y * mouseSensitivity;
            }

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            ApplyTransform();
        }

        void ApplyTransform()
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, height, -distance);
            Vector3 focus = target.position + Vector3.up * 1.5f;
            transform.position = focus + offset;
            transform.LookAt(focus);
        }

        bool IsGamepadLook()
        {
            return Gamepad.current != null
                && Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.01f;
        }

        public void SetCameraEnabled(bool enabled)
        {
            isEnabled = enabled;
        }
    }
}
```

---

## Inspector checklist

On **Main Camera** → `PlayerCameraRig`:

| Field | Must be |
|-------|---------|
| Target | Player transform |
| Input | Player’s `PlayerInputAdapter` (drag from Player object) |

If **Input** is empty, camera **never** rotates — but move still works (motor uses `GetComponent` on Player).

---

## Verify

- [ ] Play Boot → New Run → Graybox
- [ ] Click game view once if needed (first frame)
- [ ] WASD + mouse look together for 30 seconds
- [ ] Press Escape → look stops → click game view → look returns (with re-lock fix)
- [ ] Gamepad: left stick move + right stick look at reasonable speed
- [ ] `PlayerControlGate` has no null refs on Input / Motor / Camera Rig

---

## If still broken

1. **Game view focused?** Editor only — mouse look needs Game tab focused.
2. **Two cameras?** Only one tagged **MainCamera**.
3. **Gameplay map disabled?** Debug: log `input.Intent.Look` in `PlayerCameraRig` — if zero while moving mouse, binding or cursor issue; if non-zero but camera still, rig disabled or null target.
