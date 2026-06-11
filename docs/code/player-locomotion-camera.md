# Player locomotion & camera collision (M1)

Base third-person locomotion: **move**, **jump**, **dodge**, and **camera occlusion** via sphere cast.

**Supersedes:** ad-hoc `PlayerMotor` / `PlayerCameraRig` behavior only — does not replace combat-minimum Part A if you have not typed that yet (this doc **includes** intent migration + `Jump`).

---

## Cacildes vs AF

| Cacildes | Problem | AF |
|----------|---------|-----|
| `ThirdPersonController` + `ILocomotionState` (ground/air/swim/climb) | God object, 300+ lines | `PlayerMotor` + `PlayerDodge` — two focused components |
| `PlayerDodgeController` + animation root motion + TigerForge events | Tied to animator pipeline | Velocity dodge for graybox; animator hooks later via `PlayerView` |
| `LockOnCameraCollision` + Cinemachine | Extra package + transposer coupling | Built into `PlayerCameraRig` — same sphere-cast idea, no Cinemachine |
| `Camera.main` in motor | Hidden dependency | `PlayerCameraRig` exposes yaw for camera-relative move |
| Space = dodge in current AF input | Wrong for soulslike | **Space = Jump**, **Left Ctrl = Dodge** |

Dodge will eventually become a `DodgeCombatAction` subclass that calls the same `PlayerDodge.TryStart(...)` API — locomotion owns movement; combat owns costs, i-frames, and busy gating with attacks.

**Note:** Velocity dodge in this doc is a **graybox placeholder**. Root-motion dodge + combat are in [character-animations.md](character-animations.md). Prefer **`CharacterMotor`** + **`PlayerLocomotionInput`** over `PlayerMotor` when typing.

---

## Files

```
Assets/_Project/Core/Runtime/
├── PlayerIntent.cs              ← MOVE from Player (add Jump)
├── IPlayerIntentSource.cs       ← MOVE from Player
└── LocomotionMath.cs            ← NEW (pure math, testable)

Assets/_Project/Player/
├── AF.Player.asmdef             ← unchanged refs
├── Input/
│   └── PlayerInputActions.inputactions   ← add Jump; rebind Dodge
├── Runtime/
│   ├── PlayerLocomotionSettings.cs       ← NEW ScriptableObject
│   ├── PlayerMotor.cs                    ← REWRITE
│   ├── PlayerDodge.cs                    ← NEW
│   ├── PlayerCameraRig.cs                ← UPDATE (collision + yaw API)
│   ├── PlayerInputAdapter.cs             ← UPDATE (Jump, Core namespace)
│   ├── PlayerControlGate.cs              ← UPDATE (cursor lock, dodge, serialized camera)
│   └── PlayerIntent.cs                   ← DELETE after move
│   └── IPlayerIntentSource.cs            ← DELETE after move
└── Tests/
    ├── AF.Player.Tests.asmdef
    └── LocomotionMathTests.cs
```

---

# Part A — Intent in Core (+ Jump)

### `Assets/_Project/Core/Runtime/PlayerIntent.cs`

```csharp
using UnityEngine;

namespace AF.Core
{
    public struct PlayerIntent
    {
        public Vector2 Move;
        public Vector2 Look;
        public bool Jump;
        public bool Dodge;
        public bool LightAttack;
        public bool Block;
    }
}
```

---

### `Assets/_Project/Core/Runtime/IPlayerIntentSource.cs`

```csharp
namespace AF.Core
{
    public interface IPlayerIntentSource
    {
        PlayerIntent Intent { get; }
    }
}
```

---

### `Assets/_Project/Core/Runtime/LocomotionMath.cs`

```csharp
using UnityEngine;

namespace AF.Core
{
    /// <summary>Frame-independent locomotion helpers — no MonoBehaviour, no Physics.</summary>
    public static class LocomotionMath
    {
        public static float ComputeJumpVelocity(float jumpHeight, float gravity)
        {
            return Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        /// <summary>Camera-relative horizontal move from stick input and camera yaw (degrees).</summary>
        public static Vector3 CameraRelativeMove(Vector2 moveInput, float cameraYawDegrees)
        {
            Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            if (direction.sqrMagnitude < 0.0001f)
            {
                return Vector3.zero;
            }

            Quaternion yawRotation = Quaternion.Euler(0f, cameraYawDegrees, 0f);
            return yawRotation * direction;
        }

        public static Vector3 FlattenForward(Vector3 worldForward)
        {
            worldForward.y = 0f;
            return worldForward.sqrMagnitude > 0.0001f ? worldForward.normalized : Vector3.forward;
        }

        /// <summary>Smooth camera distance after sphere cast (push in fast, pull out slow).</summary>
        public static float SmoothCameraDistance(
            float current,
            float target,
            float pushInSpeed,
            float pullOutSpeed,
            float deltaTime)
        {
            float speed = target < current ? pushInSpeed : pullOutSpeed;
            return Mathf.Lerp(current, target, deltaTime * speed);
        }
    }
}
```

Delete `Assets/_Project/Player/Runtime/PlayerIntent.cs` and `IPlayerIntentSource.cs` after creating the Core versions.

---

# Part B — Input

### `PlayerInputActions.inputactions` — add Jump, rebind Dodge

In the **Gameplay** map:

1. Add action **`Jump`** (type **Button**).
2. Bindings:

| Action | Keyboard | Gamepad |
|--------|----------|---------|
| `Jump` | `<Keyboard>/space` | `<Gamepad>/buttonSouth` |
| `Dodge` | `<Keyboard>/leftCtrl` | `<Gamepad>/buttonEast` |

3. **Remove** the old binding of `<Keyboard>/space` → `Dodge`.

**Save Asset** → Unity regenerates `PlayerInputActions.cs`.

---

### `Assets/_Project/Player/Runtime/PlayerInputAdapter.cs`

```csharp
using AF.Core;
using UnityEngine;

namespace AF.Player
{
    public sealed class PlayerInputAdapter : MonoBehaviour, IPlayerIntentSource
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
                Look = actions.Gameplay.Look.ReadValue<Vector2>(),
                Jump = actions.Gameplay.Jump.WasPressedThisFrame(),
                Dodge = actions.Gameplay.Dodge.WasPressedThisFrame(),
                LightAttack = actions.Gameplay.LightAttack.WasPressedThisFrame(),
                Block = actions.Gameplay.Block.IsPressed()
            };
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

# Part C — Locomotion settings

### `Assets/_Project/Player/Runtime/PlayerLocomotionSettings.cs`

```csharp
using UnityEngine;

namespace AF.Player
{
    [CreateAssetMenu(fileName = "PlayerLocomotionSettings", menuName = "AF/Player/Locomotion Settings")]
    public sealed class PlayerLocomotionSettings : ScriptableObject
    {
        [Header("Move")]
        public float moveSpeed = 5f;
        public float rotationSpeed = 12f;

        [Header("Jump")]
        public float jumpHeight = 1.2f;
        public float gravity = -20f;
        public float jumpTimeout = 0.25f;
        public float groundedStickVelocity = -2f;

        [Header("Dodge")]
        public float dodgeSpeed = 8f;
        public float dodgeDuration = 0.4f;
        public float dodgeCooldown = 0.5f;
        public float backstepSpeed = 6f;
        public float backstepDuration = 0.35f;
    }
}
```

Create asset: `Assets/Data/Player/DefaultLocomotionSettings.asset` (assign on Player).

---

# Part D — Motor & dodge

### `Assets/_Project/Player/Runtime/PlayerMotor.cs`

```csharp
using AF.Core;
using UnityEngine;

namespace AF.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputAdapter))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] PlayerLocomotionSettings settings;
        [SerializeField] PlayerCameraRig cameraRig;

        CharacterController controller;
        PlayerInputAdapter input;

        float verticalVelocity;
        float jumpTimeoutDelta;
        bool isEnabled;

        public bool IsGrounded => controller != null && controller.isGrounded;
        public bool IsLocomotionBusy { get; private set; }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputAdapter>();

            if (cameraRig == null)
            {
                cameraRig = FindAnyObjectByType<PlayerCameraRig>(FindObjectsInactive.Include);
            }
        }

        void Update()
        {
            if (!isEnabled || settings == null)
            {
                return;
            }

            if (IsLocomotionBusy)
            {
                ApplyGravity();
                controller.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
                return;
            }

            UpdateJumpTimeout();
            HandleJump();
            ApplyGravity();

            Vector3 horizontal = LocomotionMath.CameraRelativeMove(
                input.Intent.Move,
                cameraRig != null ? cameraRig.YawDegrees : 0f);

            if (horizontal.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(horizontal);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    settings.rotationSpeed * Time.deltaTime);
            }

            Vector3 velocity = horizontal * settings.moveSpeed;
            velocity.y = verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }

        void HandleJump()
        {
            if (!input.Intent.Jump)
            {
                return;
            }

            if (!IsGrounded || jumpTimeoutDelta > 0f)
            {
                return;
            }

            verticalVelocity = LocomotionMath.ComputeJumpVelocity(settings.jumpHeight, settings.gravity);
            jumpTimeoutDelta = settings.jumpTimeout;
        }

        void UpdateJumpTimeout()
        {
            if (jumpTimeoutDelta > 0f)
            {
                jumpTimeoutDelta -= Time.deltaTime;
            }
        }

        void ApplyGravity()
        {
            if (IsGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = settings.groundedStickVelocity;
            }

            verticalVelocity += settings.gravity * Time.deltaTime;
        }

        /// <summary>Called by PlayerDodge — overrides normal move for one frame slice.</summary>
        public void ApplyDodgeDisplacement(Vector3 worldVelocity)
        {
            if (!isEnabled)
            {
                return;
            }

            worldVelocity.y = verticalVelocity;
            controller.Move(worldVelocity * Time.deltaTime);
        }

        public void SetLocomotionBusy(bool busy)
        {
            IsLocomotionBusy = busy;
        }

        public void SetMotorEnabled(bool enabled)
        {
            isEnabled = enabled;
        }
    }
}
```

---

### `Assets/_Project/Player/Runtime/PlayerDodge.cs`

```csharp
using AF.Core;
using UnityEngine;

namespace AF.Player
{
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(PlayerInputAdapter))]
    public sealed class PlayerDodge : MonoBehaviour
    {
        [SerializeField] PlayerLocomotionSettings settings;
        [SerializeField] PlayerCameraRig cameraRig;

        PlayerMotor motor;
        PlayerInputAdapter input;

        float dodgeTimeRemaining;
        float cooldownRemaining;
        Vector3 dodgeVelocity;
        bool isEnabled = true;

        public bool IsDodging => dodgeTimeRemaining > 0f;

        void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            input = GetComponent<PlayerInputAdapter>();

            if (cameraRig == null)
            {
                cameraRig = FindAnyObjectByType<PlayerCameraRig>(FindObjectsInactive.Include);
            }
        }

        void Update()
        {
            if (!isEnabled || settings == null)
            {
                return;
            }

            if (cooldownRemaining > 0f)
            {
                cooldownRemaining -= Time.deltaTime;
            }

            if (dodgeTimeRemaining > 0f)
            {
                TickDodge();
                return;
            }

            if (input.Intent.Dodge && cooldownRemaining <= 0f && !motor.IsLocomotionBusy)
            {
                TryStartDodge(input.Intent.Move);
            }
        }

        /// <summary>Future: DodgeCombatAction calls this instead of reading intent.</summary>
        public bool TryStartDodge(Vector2 moveInput)
        {
            if (!isEnabled || settings == null || dodgeTimeRemaining > 0f || cooldownRemaining > 0f)
            {
                return false;
            }

            bool backstep = moveInput.sqrMagnitude < 0.01f;
            Vector3 direction;

            if (backstep)
            {
                direction = -transform.forward;
                dodgeTimeRemaining = settings.backstepDuration;
                dodgeVelocity = direction * settings.backstepSpeed;
            }
            else
            {
                direction = LocomotionMath.CameraRelativeMove(moveInput, cameraRig != null ? cameraRig.YawDegrees : 0f);
                if (direction.sqrMagnitude < 0.01f)
                {
                    direction = transform.forward;
                }

                dodgeTimeRemaining = settings.dodgeDuration;
                dodgeVelocity = direction.normalized * settings.dodgeSpeed;
            }

            motor.SetLocomotionBusy(true);
            cooldownRemaining = settings.dodgeCooldown;
            return true;
        }

        void TickDodge()
        {
            motor.ApplyDodgeDisplacement(dodgeVelocity);
            dodgeTimeRemaining -= Time.deltaTime;

            if (dodgeTimeRemaining <= 0f)
            {
                motor.SetLocomotionBusy(false);
            }
        }

        public void SetDodgeEnabled(bool enabled)
        {
            isEnabled = enabled;
            if (!enabled)
            {
                dodgeTimeRemaining = 0f;
                motor.SetLocomotionBusy(false);
            }
        }
    }
}
```

---

# Part E — Camera + collision

Sphere cast from **focus pivot** toward the **intended** camera offset direction (Cacildes `LockOnCameraCollision` pattern, without Cinemachine).

### `Assets/_Project/Player/Runtime/PlayerCameraRig.cs`

```csharp
using AF.Core;
using UnityEngine;

namespace AF.Player
{
    public sealed class PlayerCameraRig : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] PlayerInputAdapter input;

        [Header("Orbit")]
        [SerializeField] float distance = 4f;
        [SerializeField] float height = 2f;
        [SerializeField] float focusHeight = 1.5f;
        [SerializeField] float lookSensitivity = 0.15f;
        [SerializeField] float minPitch = -30f;
        [SerializeField] float maxPitch = 60f;

        [Header("Collision")]
        [SerializeField] LayerMask collisionLayers;
        [SerializeField] float sphereRadius = 0.25f;
        [SerializeField] float minDistance = 1f;
        [SerializeField] float pushInSpeed = 12f;
        [SerializeField] float pullOutSpeed = 4f;

        float yaw;
        float pitch = 15f;
        float currentDistance;
        bool isEnabled = true;

        public float YawDegrees => yaw;
        public Transform Target => target;

        void Awake()
        {
            currentDistance = distance;
        }

        void LateUpdate()
        {
            if (!isEnabled || target == null || input == null)
            {
                return;
            }

            Vector2 look = input.Intent.Look;
            if (look.sqrMagnitude >= 0.0001f)
            {
                yaw += look.x * lookSensitivity;
                pitch -= look.y * lookSensitivity;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            ApplyTransform();
        }

        void ApplyTransform()
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 focus = target.position + Vector3.up * focusHeight;

            Vector3 desiredOffset = rotation * new Vector3(0f, height, -distance);
            Vector3 desiredPosition = focus + desiredOffset;
            Vector3 castDirection = (desiredPosition - focus).normalized;
            float castLength = desiredOffset.magnitude;

            float targetDistance = castLength;
            if (Physics.SphereCast(
                    focus,
                    sphereRadius,
                    castDirection,
                    out RaycastHit hit,
                    castLength,
                    collisionLayers,
                    QueryTriggerInteraction.Ignore))
            {
                targetDistance = Mathf.Clamp(hit.distance - sphereRadius * 0.5f, minDistance, castLength);
            }

            currentDistance = LocomotionMath.SmoothCameraDistance(
                currentDistance,
                targetDistance,
                pushInSpeed,
                pullOutSpeed,
                Time.deltaTime);

            Vector3 offset = castDirection * currentDistance;
            transform.position = focus + offset;
            transform.LookAt(focus);
        }

        public void SetCameraEnabled(bool enabled)
        {
            isEnabled = enabled;
        }

        public void Initialize(PlayerInputAdapter playerInputAdapter, Transform followTarget)
        {
            input = playerInputAdapter;
            target = followTarget != null ? followTarget : playerInputAdapter.transform;
        }
    }
}
```

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
        [SerializeField] PlayerDodge dodge;
        [SerializeField] PlayerCameraRig cameraRig;

        RunCoordinator runCoordinator;
        RunState lastState = RunState.Boot;

        void Awake()
        {
            if (dodge == null)
            {
                dodge = GetComponent<PlayerDodge>();
            }

            if (cameraRig != null && input != null)
            {
                cameraRig.Initialize(input, input.transform);
            }
        }

        void Start()
        {
            ApplyState(GetCoordinator()?.State ?? RunState.Boot);
        }

        void Update()
        {
            if (runCoordinator == null)
            {
                return;
            }

            RunState state = runCoordinator.State;
            if (state != lastState)
            {
                ApplyState(state);
            }
        }

        void ApplyState(RunState state)
        {
            lastState = state;

            bool gameplay = state == RunState.FloorActive;
            input.SetInputEnabled(gameplay);
            motor.SetMotorEnabled(gameplay);
            dodge.SetDodgeEnabled(gameplay);
            cameraRig.SetCameraEnabled(gameplay);

            if (gameplay)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
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

# Part F — Tests

### `Assets/_Project/Player/Tests/AF.Player.Tests.asmdef`

```json
{
  "name": "AF.Player.Tests",
  "rootNamespace": "AF.Tests.Player",
  "references": [
    "AF.Core",
    "AF.Player",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
  ],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "versionDefines": [],
  "noEngineReferences": false
}
```

---

### `Assets/_Project/Player/Tests/LocomotionMathTests.cs`

```csharp
using AF.Core;
using NUnit.Framework;
using UnityEngine;

namespace AF.Tests.Player
{
    public sealed class LocomotionMathTests
    {
        [Test]
        public void ComputeJumpVelocity_MatchesPhysicsFormula()
        {
            float gravity = -20f;
            float jumpHeight = 1.2f;
            float expected = Mathf.Sqrt(jumpHeight * -2f * gravity);
            Assert.AreEqual(expected, LocomotionMath.ComputeJumpVelocity(jumpHeight, gravity), 0.001f);
        }

        [Test]
        public void CameraRelativeMove_WithYawZero_MapsStickToWorldXZ()
        {
            Vector3 result = LocomotionMath.CameraRelativeMove(new Vector2(0f, 1f), 0f);
            Assert.AreEqual(Vector3.forward, result);
        }

        [Test]
        public void CameraRelativeMove_WithYaw90_RotatesInput()
        {
            Vector3 result = LocomotionMath.CameraRelativeMove(new Vector2(0f, 1f), 90f);
            Assert.AreEqual(Vector3.right, result, 0.001f);
        }

        [Test]
        public void SmoothCameraDistance_PushIn_UsesPushSpeed()
        {
            float next = LocomotionMath.SmoothCameraDistance(5f, 2f, 12f, 4f, 0.5f);
            Assert.Less(next, 5f);
            Assert.Greater(next, 2f);
        }
    }
}
```

---

# Part G — Unity setup

## Layers

1. **Edit → Project Settings → Tags and Layers**
2. Add layer **`Environment`** (or use **Default** for graybox walls).
3. On `PlayerCameraRig`, set **Collision Layers** to **Environment** only (exclude **Player**).

Assign dungeon room walls / floor colliders to **Environment**.

---

## Player hierarchy

```
Player
├── PlayerInputAdapter
├── PlayerMotor              settings → DefaultLocomotionSettings
├── PlayerDodge                settings → DefaultLocomotionSettings
├── PlayerControlGate          wire input, motor, dodge, cameraRig
├── CharacterController        height ~1.8, radius ~0.3, center y ~0.9
└── (visual mesh child)

PlayerCameraRig                separate GameObject in scene (Main Camera)
├── Camera component
└── PlayerCameraRig            target → Player transform, input → PlayerInputAdapter
```

- Drag **Player Camera Rig** reference onto `PlayerMotor`, `PlayerDodge`, and `PlayerControlGate` (avoid `FindAnyObjectByType` in builds).
- `PlayerControlGate.Awake` calls `cameraRig.Initialize(input, input.transform)`.

---

## Play verification

1. **TitleScreen → New Run** → Graybox floor.
2. **WASD** — move relative to camera; character rotates toward move direction.
3. **Mouse** — orbit camera; pitch clamped.
4. **Space** — jump; land; short jump buffer (cannot spam).
5. **Left Ctrl** (or gamepad East) — dodge in move direction; **no stick** → backstep backward.
6. During dodge — normal WASD move suppressed; gravity still applies.
7. Walk camera into a wall — camera pulls in; step back — camera eases out.
8. **FloorActive** — cursor locked; menu / boot — cursor free.

---

## Edit Mode verification

Test Runner → **AF.Player.Tests** → `LocomotionMathTests` (4 tests green).

---

## Full checklist

- [ ] Part A: `PlayerIntent` + `IPlayerIntentSource` in Core; old Player copies deleted
- [ ] Part B: `Jump` action; Space off Dodge; `PlayerInputAdapter` updated
- [ ] Part C: `DefaultLocomotionSettings.asset` created
- [ ] Part D: `PlayerMotor` + `PlayerDodge` on Player
- [ ] Part E: `PlayerCameraRig` collision + `PlayerControlGate` cursor lock
- [ ] Part F: `AF.Player.Tests` green
- [ ] Part G: Environment layer + scene wired
- [ ] Move + jump + dodge + camera collision work in Graybox

---

## Later (not this slice)

| Item | Where |
|------|--------|
| Stamina cost on dodge/jump | `AF.Stats` + `DodgeCombatAction` |
| I-frames | `CombatController` + hurtbox invuln flag |
| Animator roll / jump | `PlayerView` reads motor/dodge state |
| Lock-on camera mode | `PlayerLockOn` swaps orbit rules |
| Sprint | `PlayerIntent` + settings field |
