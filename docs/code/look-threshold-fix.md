# Look threshold fix

## Goal

Camera look should respond to **small** mouse or stick input while walking. No “dead zone until I flick hard.”

---

## Why it happens

### 1. Hard cutoff in `PlayerCameraRig` (your code)

```csharp
if (look.sqrMagnitude < 0.0001f)  // magnitude must exceed ~0.01
{
    return;  // skips rotation entirely
}
```

Small stick input (especially after Unity’s default stick deadzone ~0.125) and gentle mouse movement get thrown away.

### 2. One `Look` action reads mouse + stick together

`ReadValue<Vector2>()` on an action bound to **both** `<Mouse>/delta` and `<Gamepad>/rightStick` can behave poorly when a gamepad is plugged in — small mouse deltas get eaten until you move aggressively.

### 3. Same sensitivity for pixels and stick

Mouse delta = pixels/frame (e.g. 5–50). Stick = -1..1. Both × `0.15f` makes stick feel dead; you push harder until it “activates.”

### 4. Bonus bug: `PlayerControlGate.Update` never runs

`runCoordinator` field is **never assigned** — always null — so `Update` always returns at line 22. `Start()` still applies state once, but state changes after that are ignored.

---

## Fixes

### `Assets/_Project/Player/Runtime/PlayerInputAdapter.cs`

Read mouse and stick **separately**. Mouse wins when it moved this frame.

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace AF.Player
{
    public sealed class PlayerInputAdapter : MonoBehaviour
    {
        PlayerInputActions actions;
        bool isEnabled;

        public PlayerIntent Intent { get; private set; }

        void Awake()
        {
            actions = new PlayerInputActions();
        }

        void OnDestroy()
        {
            actions?.Dispose();
        }

        void Update()
        {
            if (!isEnabled)
            {
                Intent = default;
                return;
            }

            Intent = new PlayerIntent
            {
                Move = actions.Gameplay.Move.ReadValue<Vector2>(),
                Look = ReadLook(),
                Dodge = actions.Gameplay.Dodge.WasPressedThisFrame(),
                LookFromStick = IsStickDrivingLook()
            };
        }

        Vector2 ReadLook()
        {
            Vector2 mouseDelta = Mouse.current != null
                ? Mouse.current.delta.ReadValue()
                : Vector2.zero;

            if (mouseDelta.sqrMagnitude > 0f)
            {
                return mouseDelta;
            }

            if (Gamepad.current != null)
            {
                return Gamepad.current.rightStick.ReadValue();
            }

            return Vector2.zero;
        }

        bool IsStickDrivingLook()
        {
            if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0f)
            {
                return false;
            }

            return Gamepad.current != null
                && Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0f;
        }

        public void SetInputEnabled(bool enabled)
        {
            if (isEnabled == enabled)
            {
                return;
            }

            isEnabled = enabled;

            if (enabled)
            {
                actions.Gameplay.Enable();
            }
            else
            {
                actions.Gameplay.Disable();
                Intent = default;
            }
        }
    }
}
```

---

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
        public bool LookFromStick;
    }
}
```

---

### `Assets/_Project/Player/Runtime/PlayerCameraRig.cs`

Remove the magnitude gate. Scale stick and mouse differently.

```csharp
using UnityEngine;

namespace AF.Player
{
    public sealed class PlayerCameraRig : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] PlayerInputAdapter input;
        [SerializeField] float mouseSensitivity = 0.15f;
        [SerializeField] float stickSensitivity = 180f;
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
            if (look.sqrMagnitude <= 0f)
            {
                ApplyTransform();
                return;
            }

            if (input.Intent.LookFromStick)
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

        public void SetCameraEnabled(bool enabled)
        {
            isEnabled = enabled;
        }
    }
}
```

---

### `Assets/_Project/Player/Runtime/PlayerControlGate.cs`

Fix coordinator lookup + cursor re-lock during gameplay.

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

### Optional — Input Actions (Editor)

On **Look** → **right stick** binding only, add processor:

```
stickDeadzone(min=0.05,max=0.95)
```

Lowers the built-in ~0.125 deadzone that zeroes small stick input.

You can **remove** mouse and stick bindings from the Look action entirely if you read them directly in `PlayerInputAdapter` (as above). Keep the Look action empty or delete bindings — Move/Dodge still use the asset.

---

## Verify

- [ ] Walk with **WASD** + gentle mouse → camera moves immediately
- [ ] Walk with **left stick** + gentle **right stick** → camera moves without a “push through” threshold
- [ ] No `look.sqrMagnitude < 0.0001f` gate in camera code
- [ ] `PlayerControlGate.Update` runs (state changes work after Start)

### Debug (temporary)

In `PlayerCameraRig.LateUpdate`:

```csharp
Debug.Log($"Look: {look} stick: {input.Intent.LookFromStick}");
```

Gentle mouse while walking should show non-zero `Look` every frame you move the mouse.

---

## Summary

| Problem | Fix |
|---------|-----|
| `0.0001f` cutoff | Removed — only skip when look is exactly zero |
| Combined Look ReadValue | Read `Mouse.current.delta` and `rightStick` separately |
| Stick × 0.15 | Stick uses `× stickSensitivity × deltaTime` |
| ControlGate `runCoordinator` always null | Use `GetCoordinator()` in `Update` |
