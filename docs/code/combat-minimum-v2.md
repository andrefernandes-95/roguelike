# Part C — Combat (`AF.Combat` + player adapter)

One **executor** (`CombatController`) for player and AI. Jam light attack = `MeleeHitboxAction` asset.

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

# Part D — Unity setup

## Create `LightAttack_Unarmed` asset

1. Create folder `Assets/Data/Combat/`
2. Right-click → **Create → AF → Combat → Melee Hitbox Action**
3. Name: `LightAttack_Unarmed`
4. Set **Damage** = `15`, **Duration** = `0.25`, **Stamina Cost** = `0`

---

## Player hierarchy

```
Player
├── ... existing motor / input / camera / control gate ...
├── CombatActor              baseProfile: Vitality 10, Endurance 10
├── HealthComponent          combatActor → CombatActor
├── Hurtbox                  ownerRoot = Player, health → HealthComponent
├── PlayerDeathBridge        health → HealthComponent
├── CombatController         actor → CombatActor, hitbox → AttackHitbox
├── PlayerCombatInput        combat → CombatController, lightAttack → LightAttack_Unarmed
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
