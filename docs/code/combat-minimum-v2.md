# Part C — Combat (`AF.Combat` + player adapter)

One **executor** (`CombatController`) for player and AI. Jam light attack = `MeleeHitboxAction` asset.

> **Animation-driven completion:** Part C snippets below show the original timer shape. **Use Part E** for the current `CombatController` / `MeleeHitboxAction` (no `actionTimer`).

## Files

```
Assets/_Project/Combat/
├── AF.Combat.asmdef
├── CombatAction.cs              ← abstract base
├── MeleeHitboxAction.cs         ← jam light attack
├── CombatExecution.cs
├── CombatController.cs          ← entity-agnostic; no input
├── CombatActor.cs
├── HealthComponent.cs
├── Hurtbox.cs
├── Hitbox.cs
├── PlayerDeathBridge.cs
├── ContactDamage.cs
└── DeathCleanup.cs

Assets/_Project/Player/Runtime/
└── PlayerCombatInput.cs         ← reads intent, calls CombatController

Assets/Data/Combat/
└── LightAttack_Unarmed.asset    ← MeleeHitboxAction, create in Editor
```

---

### `Assets/_Project/Combat/AF.Combat.asmdef`

```json
{
  "name": "AF.Combat",
  "rootNamespace": "AF.Combat",
  "references": ["AF.Core", "AF.Stats"],
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

---

### `Assets/_Project/Combat/CombatAction.cs`

```csharp
using UnityEngine;

namespace AF.Combat
{
    /// <summary>
    /// Data + behavior for one combat verb. Subclass per behavior family (melee, projectile, buff, …).
    /// Cacildes equivalent: abstract Ability — not a single sealed SO with every field.
    /// </summary>
    public abstract class CombatAction : ScriptableObject
    {
        [Header("Costs (jam: leave stamina 0)")]
        public int staminaCost;

        [Header("Combo (later)")]
        public CombatAction next;

        public virtual bool CanExecute(CombatExecution ctx)
        {
            return ctx != null && ctx.Controller != null && !ctx.Controller.IsBusy;
        }

        public abstract void Begin(CombatExecution ctx);
        public abstract void Tick(CombatExecution ctx, float deltaTime);
        public abstract void End(CombatExecution ctx);
    }
}
```

---

### `Assets/_Project/Combat/CombatExecution.cs`

```csharp
namespace AF.Combat
{
    /// <summary>
    /// Per-run context for the active action. Passed into CombatAction lifecycle methods.
    /// </summary>
    public sealed class CombatExecution
    {
        public CombatController Controller { get; }
        public CombatActor Actor { get; }
        public Hitbox Hitbox { get; }

        public CombatExecution(CombatController controller, CombatActor actor, Hitbox hitbox)
        {
            Controller = controller;
            Actor = actor;
            Hitbox = hitbox;
        }
    }
}
```

---

### `Assets/_Project/Combat/MeleeHitboxAction.cs`

```csharp
using UnityEngine;

namespace AF.Combat
{
    [CreateAssetMenu(fileName = "MeleeHitboxAction", menuName = "AF/Combat/Melee Hitbox Action")]
    public sealed class MeleeHitboxAction : CombatAction
    {
        [Header("Melee")]
        public int damage = 15;
        public float duration = 0.25f;

        [Header("Presentation (optional jam)")]
        public string animatorTrigger;

        public override void Begin(CombatExecution ctx)
        {
            if (ctx.Hitbox == null)
            {
                return;
            }

            ctx.Hitbox.ConfigureDamage(damage);
            ctx.Hitbox.BeginSwing();
            ctx.Controller.SetActionTimer(duration);
        }

        public override void Tick(CombatExecution ctx, float deltaTime)
        {
        }

        public override void End(CombatExecution ctx)
        {
            ctx.Hitbox?.EndSwing();
        }
    }
}
```

---

### `Assets/_Project/Combat/CombatActor.cs`

```csharp
using AF.Stats;
using UnityEngine;

namespace AF.Combat
{
    /// <summary>
    /// Owns StatSheet for this entity. Player uses this; enemies can omit and use StatProfile on HealthComponent only.
    /// </summary>
    public sealed class CombatActor : MonoBehaviour
    {
        [SerializeField] StatProfile baseProfile = StatProfile.DefaultPlayer;

        StatSheet sheet;

        public StatSheet Sheet
        {
            get
            {
                if (sheet == null)
                {
                    sheet = new StatSheet(baseProfile);
                }

                return sheet;
            }
        }

        public void ApplyEquipment(string sourceId, StatModifier modifier)
        {
            Sheet.AddModifiers(sourceId, new[] { modifier });
        }

        public void RemoveEquipment(string sourceId)
        {
            Sheet.RemoveModifiers(sourceId);
        }
    }
}
```

---

### `Assets/_Project/Combat/HealthComponent.cs`

```csharp
using System;
using AF.Stats;
using UnityEngine;

namespace AF.Combat
{
    public sealed class HealthComponent : MonoBehaviour
    {
        [SerializeField] CombatActor combatActor;
        [SerializeField] StatProfile fallbackProfile = StatProfile.DefaultEnemy;

        ResourcePool pool;
        StatSheet sheet;

        public event Action<DamageResult> Damaged;
        public event Action Died;

        public int MaxHealth => pool?.Max ?? 0;
        public int CurrentHealth => pool?.Current ?? 0;
        public bool IsDead => pool?.IsEmpty ?? false;

        void Awake()
        {
            sheet = combatActor != null ? combatActor.Sheet : new StatSheet(fallbackProfile);
            pool = new ResourcePool(DerivedStats.MaxHealth(sheet));
        }

        public void RefreshMaxFromStats()
        {
            pool.RefreshMax(DerivedStats.MaxHealth(sheet));
        }

        public void Fill()
        {
            pool.Fill();
        }

        public void ApplyDamage(int amount)
        {
            if (pool.IsEmpty)
            {
                return;
            }

            DamageResult result = DamageResolver.Resolve(pool, new DamageRequest(amount));
            if (result.DamageDealt <= 0)
            {
                return;
            }

            Damaged?.Invoke(result);
            if (result.Depleted)
            {
                Died?.Invoke();
            }
        }
    }
}
```

---

### `Assets/_Project/Combat/CombatController.cs`

```csharp
using UnityEngine;

namespace AF.Combat
{
    /// <summary>
    /// Shared combat executor for player and AI. Does not read input.
    /// </summary>
    public sealed class CombatController : MonoBehaviour
    {
        [SerializeField] CombatActor actor;
        [SerializeField] Hitbox hitbox;

        CombatExecution execution;
        CombatAction activeAction;
        float actionTimer;

        void Awake()
        {
            execution = new CombatExecution(this, actor, hitbox);
        }

        void Update()
        {
            if (!IsBusy)
            {
                return;
            }

            actionTimer -= Time.deltaTime;
            activeAction.Tick(execution, Time.deltaTime);

            if (actionTimer <= 0f)
            {
                EndActiveAction();
            }
        }

        /// <summary>Called by PlayerCombatInput, AI states, scripts, etc.</summary>
        public bool TryStart(CombatAction action)
        {
            if (action == null || IsBusy)
            {
                return false;
            }

            if (!action.CanExecute(execution))
            {
                return false;
            }

            // Stamina gate later: actor resource pools + action.staminaCost

            activeAction = action;
            activeAction.Begin(execution);
            return true;
        }

        public void SetActionTimer(float duration)
        {
            actionTimer = duration;
        }

        void EndActiveAction()
        {
            activeAction?.End(execution);
            activeAction = null;
            actionTimer = 0f;
        }

        public bool IsBusy => activeAction != null;
    }
}
```

---

### `Assets/_Project/Player/Runtime/PlayerCombatInput.cs`

Add `"AF.Combat"` to `AF.Player.asmdef` references.

```csharp
using AF.Combat;
using AF.Core;
using UnityEngine;

namespace AF.Player
{
    /// <summary>
    /// Player-only: maps PlayerIntent → CombatController.TryStart.
    /// AI uses its own adapter; never put input reads on CombatController.
    /// </summary>
    public sealed class PlayerCombatInput : MonoBehaviour
    {
        [SerializeField] CombatController combat;
        [SerializeField] MeleeHitboxAction lightAttack;

        IPlayerIntentSource intentSource;

        void Awake()
        {
            intentSource = GetComponent<IPlayerIntentSource>();
            if (combat == null)
            {
                combat = GetComponent<CombatController>();
            }
        }

        void Update()
        {
            if (intentSource == null || combat == null || lightAttack == null)
            {
                return;
            }

            if (intentSource.Intent.LightAttack)
            {
                combat.TryStart(lightAttack);
            }
        }
    }
}
```

---

### `Assets/_Project/Combat/Hurtbox.cs`

```csharp
using UnityEngine;

namespace AF.Combat
{
    public sealed class Hurtbox : MonoBehaviour
    {
        [SerializeField] Transform ownerRoot;
        [SerializeField] HealthComponent health;

        public Transform OwnerRoot => ownerRoot != null ? ownerRoot : transform.root;

        public void ReceiveHit(int damage)
        {
            if (health == null)
            {
                return;
            }

            health.ApplyDamage(damage);
        }
    }
}
```

---

### `Assets/_Project/Combat/Hitbox.cs`

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace AF.Combat
{
    public sealed class Hitbox : MonoBehaviour
    {
        [SerializeField] Transform ownerRoot;

        int damage = 15;
        readonly HashSet<Hurtbox> hitThisSwing = new();

        public void ConfigureDamage(int amount)
        {
            damage = amount;
        }

        public void BeginSwing()
        {
            hitThisSwing.Clear();
            gameObject.SetActive(true);
        }

        public void EndSwing()
        {
            gameObject.SetActive(false);
            hitThisSwing.Clear();
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out Hurtbox hurtbox))
            {
                return;
            }

            Transform owner = ownerRoot != null ? ownerRoot : transform.root;
            if (hurtbox.OwnerRoot == owner)
            {
                return;
            }

            if (hitThisSwing.Contains(hurtbox))
            {
                return;
            }

            hitThisSwing.Add(hurtbox);
            hurtbox.ReceiveHit(damage);
        }
    }
}
```

---

### `Assets/_Project/Combat/PlayerDeathBridge.cs`

```csharp
using AF.Core;
using UnityEngine;

namespace AF.Combat
{
    public sealed class PlayerDeathBridge : MonoBehaviour
    {
        [SerializeField] HealthComponent health;
        [SerializeField] RunCoordinator runCoordinator;

        void OnEnable()
        {
            if (health != null)
            {
                health.Died += OnDied;
            }
        }

        void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnDied;
            }
        }

        void OnDied()
        {
            RunCoordinator coordinator = runCoordinator != null
                ? runCoordinator
                : RunCoordinator.Instance;

            coordinator?.NotifyPlayerDied();
        }
    }
}
```

---

### `Assets/_Project/Combat/ContactDamage.cs`

```csharp
using UnityEngine;

namespace AF.Combat
{
    public sealed class ContactDamage : MonoBehaviour
    {
        [SerializeField] int damagePerTick = 10;
        [SerializeField] float tickInterval = 0.5f;
        [SerializeField] Transform ownerRoot;

        float nextTickTime;

        void OnTriggerStay(Collider other)
        {
            if (Time.time < nextTickTime)
            {
                return;
            }

            if (!other.TryGetComponent(out Hurtbox hurtbox))
            {
                return;
            }

            Transform owner = ownerRoot != null ? ownerRoot : transform.root;
            if (hurtbox.OwnerRoot == owner)
            {
                return;
            }

            nextTickTime = Time.time + tickInterval;
            hurtbox.ReceiveHit(damagePerTick);
        }
    }
}
```

---

### `Assets/_Project/Combat/DeathCleanup.cs`

```csharp
using UnityEngine;

namespace AF.Combat
{
    public sealed class DeathCleanup : MonoBehaviour
    {
        [SerializeField] HealthComponent health;

        void OnEnable()
        {
            if (health != null)
            {
                health.Died += OnDied;
            }
        }

        void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnDied;
            }
        }

        void OnDied()
        {
            gameObject.SetActive(false);
        }
    }
}
```

---

# Part E — Animation-driven action completion

Root-motion attacks and dodges stay **busy** until the presentation ends — not a fixed `duration` timer.  
See also [character-animations.md](character-animations.md) for animator setup; this part is **combat code only**.

## How completion works

```
MeleeHitboxAction.Begin / DodgeCombatAction.Begin
  → IActionAnimator.TryPlayState(...)
  → (no timer — stays busy until presentation ends)

End of clip OR Locomotion Hub SMB
  → IActionAnimator.OnActionComplete()     (motor/blend tree free)
  → IActionPresentationComplete.OnActionPresentationComplete()   (End active CombatAction)

Begin fails (anim did not start)
  → CombatController.CancelActiveAction()
```

| Caller | Method | Package |
|--------|--------|---------|
| Clip event `OnActionComplete` on attack/dodge | `CombatAnimationEvents` | `AF.Combat` |
| Locomotion Hub SMB (safety net) | `ResetCharacterStateOnEnter` → both Core interfaces | `AF.Character` + `AF.Combat` |
| `TryPlayState` fails in `Begin` | `CancelActiveAction()` | `AF.Combat` |

**`NotifyActionAnimationComplete` lives on `CombatController`** (`AF.Combat`).  
`CombatAnimationEvents` (model child) and the SMB call it via **`IActionPresentationComplete`** in Core so `AF.Character` never references `AF.Combat`.

## Files to add / update

```
Assets/_Project/Core/Runtime/
└── IActionPresentationComplete.cs     ← NEW

Assets/_Project/Combat/
├── CombatController.cs                ← UPDATE (implements interface + Notify…)
├── CombatExecution.cs                 ← UPDATE (Animator + Locomotion — if not done)
├── MeleeHitboxAction.cs               ← UPDATE (anim state, hitbox via clip events)
├── DodgeCombatAction.cs               ← NEW (optional M3)
└── CombatAnimationEvents.cs           ← NEW (on humanoid model child)
```

---

### `Assets/_Project/Core/Runtime/IActionPresentationComplete.cs`

```csharp
namespace AF.Core
{
    /// <summary>
    /// Combat presentation finished (clip event or locomotion hub SMB).
    /// Implemented by CombatController; invoked without AF.Character → AF.Combat reference.
    /// </summary>
    public interface IActionPresentationComplete
    {
        void OnActionPresentationComplete();
    }
}
```

---

### `Assets/_Project/Combat/CombatController.cs` (replace Part C version)

```csharp
using AF.Core;
using UnityEngine;

namespace AF.Combat
{
    /// <summary>
    /// Shared combat executor for player and AI. Does not read input.
    /// Actions end via animation events / SMB only — no action timer.
    /// </summary>
    public sealed class CombatController : MonoBehaviour, IActionPresentationComplete
    {
        [SerializeField] CombatActor actor;
        [SerializeField] Hitbox hitbox;

        CombatExecution execution;
        CombatAction activeAction;

        void Awake()
        {
            IActionAnimator actionAnimator = GetComponent<IActionAnimator>();
            ILocomotionReadout locomotionReadout = GetComponent<ILocomotionReadout>();
            execution = new CombatExecution(this, actor, hitbox, actionAnimator, locomotionReadout);
        }

        void Update()
        {
            if (!IsBusy)
            {
                return;
            }

            activeAction.Tick(execution, Time.deltaTime);
        }

        public bool TryStart(CombatAction action)
        {
            if (action == null || IsBusy)
            {
                return false;
            }

            if (!action.CanExecute(execution))
            {
                return false;
            }

            activeAction = action;
            activeAction.Begin(execution);
            return IsBusy;
        }

        public void CancelActiveAction()
        {
            if (!IsBusy)
            {
                return;
            }

            EndActiveAction();
        }

        public void NotifyActionAnimationComplete()
        {
            OnActionPresentationComplete();
        }

        public void OnActionPresentationComplete()
        {
            if (!IsBusy)
            {
                return;
            }

            EndActiveAction();
        }

        void EndActiveAction()
        {
            activeAction?.End(execution);
            activeAction = null;
        }

        public bool IsBusy => activeAction != null;
    }
}
```

---

### `Assets/_Project/Combat/CombatExecution.cs` (replace Part C version)

```csharp
using AF.Core;

namespace AF.Combat
{
    public sealed class CombatExecution
    {
        public CombatController Controller { get; }
        public CombatActor Actor { get; }
        public Hitbox Hitbox { get; }
        public IActionAnimator Animator { get; }
        public ILocomotionReadout Locomotion { get; }

        public CombatExecution(
            CombatController controller,
            CombatActor actor,
            Hitbox hitbox,
            IActionAnimator actionAnimator,
            ILocomotionReadout locomotionReadout)
        {
            Controller = controller;
            Actor = actor;
            Hitbox = hitbox;
            Animator = actionAnimator;
            Locomotion = locomotionReadout;
        }
    }
}
```

Wire on the **character root**: `CombatController` + `CharacterAnimationDriver` (`IActionAnimator`) + `PlayerLocomotionInput` or `CharacterMotor` (`ILocomotionReadout`).

---

### `Assets/_Project/Combat/MeleeHitboxAction.cs` (animation-driven)

```csharp
using UnityEngine;

namespace AF.Combat
{
    [CreateAssetMenu(fileName = "MeleeHitboxAction", menuName = "AF/Combat/Melee Hitbox Action")]
    public sealed class MeleeHitboxAction : CombatAction
    {
        [Header("Melee")]
        public int damage = 15;

        [Header("Animation")]
        [Tooltip("Animator state name, e.g. Action_LightAttack_01")]
        public string animationStateName = "Action_LightAttack_01";

        public override void Begin(CombatExecution ctx)
        {
            if (ctx.Hitbox != null)
            {
                ctx.Hitbox.ConfigureDamage(damage);
            }

            if (ctx.Animator == null
                || !ctx.Animator.TryPlayState(Animator.StringToHash(animationStateName), useRootMotion: true))
            {
                ctx.Controller.CancelActiveAction();
                return;
            }

            // Hitbox open/close via clip events on CombatAnimationEvents.
        }

        public override void Tick(CombatExecution ctx, float deltaTime) { }

        public override void End(CombatExecution ctx)
        {
            ctx.Hitbox?.EndSwing();
        }
    }
}
```

---

### `Assets/_Project/Combat/CombatAnimationEvents.cs`

Put on the **humanoid model child** (same GameObject as `Animator`). Clip event function names must match exactly.

```csharp
using AF.Core;
using UnityEngine;

namespace AF.Combat
{
    public sealed class CombatAnimationEvents : MonoBehaviour
    {
        [SerializeField] Hitbox attackHitbox;
        [SerializeField] CombatController combat;

        IActionAnimator actionAnimator;
        IActionPresentationComplete presentationComplete;

        void Awake()
        {
            if (combat == null)
            {
                combat = GetComponentInParent<CombatController>();
            }

            actionAnimator = GetComponentInParent<IActionAnimator>();
            presentationComplete = GetComponentInParent<IActionPresentationComplete>();
        }

        public void OnHitboxOpen()
        {
            attackHitbox?.BeginSwing();
        }

        public void OnHitboxClose()
        {
            attackHitbox?.EndSwing();
        }

        public void OnDodgeIframesBegin() { }

        public void OnDodgeIframesEnd() { }

        /// <summary>Clip event at end of attack/dodge — or last frame before locomotion hub.</summary>
        public void OnActionComplete()
        {
            actionAnimator?.OnActionComplete();
            presentationComplete?.OnActionPresentationComplete();
        }
    }
}
```

`OnActionComplete` calls **both** sides: animator driver (locomotion) and combat executor (`NotifyActionAnimationComplete` through the interface). Safe to call when already idle — both guard with `IsBusy` / null checks.

---

### `ResetCharacterStateOnEnter.cs` (SMB — update in `AF.Character`)

On **Locomotion Hub** state enter, same completion as clip event (in case clip event is missing):

```csharp
using AF.Core;
using UnityEngine;

namespace AF.Character
{
    public sealed class ResetCharacterStateOnEnter : StateMachineBehaviour
    {
        IActionAnimator actionAnimator;
        IActionPresentationComplete presentationComplete;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (actionAnimator == null)
            {
                actionAnimator = animator.GetComponentInParent<IActionAnimator>();
            }

            if (presentationComplete == null)
            {
                presentationComplete = animator.GetComponentInParent<IActionPresentationComplete>();
            }

            actionAnimator?.OnActionComplete();
            presentationComplete?.OnActionPresentationComplete();
        }
    }
}
```

---

## Part E — Unity animator wiring

1. On attack / dodge clips, add animation events:
   - `OnHitboxOpen` / `OnHitboxClose` at active frames (light attack)
   - `OnActionComplete` on the last frame (or first frame of transition to Locomotion Hub)
2. Event receiver object = model child with `CombatAnimationEvents`.
3. Assign **Attack Hitbox** on `CombatAnimationEvents` in Inspector.
4. `LightAttack_Unarmed` asset: set **Animation State Name** = `Action_LightAttack_01` (match controller state).

## Part E — Play verification

| # | Test |
|---|------|
| 1 | Light attack plays anim; hitbox only during `OnHitboxOpen`–`OnHitboxClose` |
| 2 | After clip, `IsBusy` false; can attack again |
| 3 | During attack, dodge input ignored (`TryStart` fails) |
| 4 | If anim missing, `TryStart` returns false (not stuck busy) |
| 5 | No null ref on `CombatAnimationEvents.OnActionComplete` |

## Part E checklist

- [ ] `IActionPresentationComplete` in Core
- [ ] `CombatController` implements interface + `NotifyActionAnimationComplete`
- [ ] `CombatExecution` has `Animator` + `Locomotion`
- [ ] No `SetActionTimer` / `actionTimer` in combat (animation-only completion)
- [ ] `CombatAnimationEvents` on model child + clip events
- [ ] Locomotion Hub SMB calls both interfaces
- [ ] `CombatController` on same root as `CharacterAnimationDriver`

---

# Part D — Unity setup

## Create `LightAttack_Unarmed` asset

1. Create folder `Assets/Data/Combat/`
2. Right-click → **Create → AF → Combat → Melee Hitbox Action**
3. Name: `LightAttack_Unarmed`
4. Set **Damage** = `15`, **Animation State Name** = your attack state, **Stamina Cost** = `0`

---

## Player hierarchy

```
Player
├── ... existing motor / input / camera / control gate ...
├── CombatActor              baseProfile: Vitality 10, Endurance 10
├── HealthComponent          combatActor → CombatActor
├── Hurtbox                  ownerRoot = Player, health → HealthComponent
├── PlayerDeathBridge        health → HealthComponent
├── CharacterAnimationDriver   IActionAnimator
├── PlayerLocomotionInput      ILocomotionReadout
├── CombatController           actor → CombatActor, hitbox → AttackHitbox
├── PlayerCombatInput          combat → CombatController, lightAttack → LightAttack_Unarmed
├── Model (child)
│   ├── Animator
│   ├── CharacterRootMotionApplier
│   └── CombatAnimationEvents  attackHitbox → AttackHitbox, combat → CombatController
└── AttackHitbox             (child, inactive by default)
    ├── BoxCollider          Is Trigger, ~1×1×1.5 in front of player
    └── Hitbox               ownerRoot = Player
```

- `PlayerInputAdapter` + `PlayerCombatInput` on Player — combat assembly stays input-agnostic.
- Add **`AF.Combat`** to `AF.Player.asmdef` (Player → Combat is OK; Combat must not reference Player).

---

## Enemy graybox

```
Enemy_Graybox
├── HealthComponent          fallbackProfile: Vitality 3 (→ 30 HP), no CombatActor
├── DeathCleanup             health → HealthComponent
├── Hurtbox                  ownerRoot = Enemy, health → HealthComponent
├── CapsuleCollider          solid
└── ContactTrigger           (child)
    ├── SphereCollider       Is Trigger
    └── ContactDamage        ownerRoot = Enemy
```

---

## Play verification

1. **TitleScreen → New Run** → Graybox with enemy near player
2. **Left click** — enemy takes damage; disables at 0 HP
3. Walk into enemy — player HP drops (100 max from vitality 10)
4. Player dies at 0 → `NotifyPlayerDied` fires
5. Console: no null refs on `PlayerCombatInput` / `CombatController`

---

## Edit Mode verification

Test Runner → run:

- `StatSheetTests`
- `DerivedStatsTests`
- `ResourcePoolTests`
- `DamageResolverTests`

---

## Full checklist

- [ ] Part A: intent in Core + attack/block input
- [ ] Part B: `AF.Stats` + 4 test fixtures green
- [ ] Part C: `AF.Combat` + `LightAttack_Unarmed` asset
- [ ] Part E: animation completion (`NotifyActionAnimationComplete`, `CombatAnimationEvents`)
- [ ] Part D: player + enemy wired
- [ ] Light attack kills enemy via `PlayerCombatInput` → `CombatController.TryStart`
- [ ] Player max HP = vitality × 10
- [ ] Contact damage → player death path

---

## Asmdef summary

| Assembly           | References                           |
| ------------------ | ------------------------------------ |
| `AF.Core`          | —                                    |
| `AF.Player`        | `AF.Core`, `AF.Combat`, Input System |
| `AF.Stats`         | `AF.Core`                            |
| `AF.Combat`        | `AF.Core`, `AF.Stats`                |
| `AF.Stats.Tests`   | `AF.Stats`, test runners (Editor)    |
| `AF.Dungeon.Tests` | `AF.Dungeon`, test runners (Editor)  |

**Never** `AF.Combat` → `AF.Player`. Player → Combat is allowed for thin adapters (`PlayerCombatInput`).

### Dungeon tests (done)

Dungeon Edit Mode tests live in `Assets/_Project/Dungeon/Tests/` (`AF.Dungeon.Tests`, namespace `AF.Tests.Dungeon`). Shared builders: `TestRooms.cs`.

---

## Equipment preview (when you add loot)

```csharp
// Equip ring +3 Vitality
combatActor.ApplyEquipment(ringInstanceId, new StatModifier(StatId.Vitality, 3));
healthComponent.RefreshMaxFromStats();
// Max HP 100 → 130, current clamped up to new max
```

Same `StatSheet` + `ResourcePool` pattern will host stamina when you type `combat-stamina.md`.
