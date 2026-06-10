# Combat minimum v2 — unified actions + stat-backed HP

## Goal

First combat loop with **one pipeline** for all future verbs (light → heavy → spell):

**player light attack (`CombatAction`) → enemy loses HP → enemy dies → contact damage → `RunCoordinator.NotifyPlayerDied()`**

Replaces [combat-minimum.md](combat-minimum.md) Parts B/C. See [combat-architecture-review.md](combat-architecture-review.md) for why.

**Prerequisite:** Player graybox + dungeon generator (or Graybox with player only).

---

## Architecture (jam)

```
AF.Core          PlayerIntent, IPlayerIntentSource
AF.Stats         StatSheet, ResourcePool, DerivedStats, DamageResolver  (pure C#)
AF.Combat        CombatAction (SO), CombatController, HealthComponent, hitboxes
AF.Player        input + motor only — no combat logic
```

| Cacildes (avoid) | This doc |
|------------------|----------|
| `PlayerCombatController` + `CharacterAbilityManager` | **`CombatController` + `CombatAction`** |
| `HealthPool(int max)` fixed | **`ResourcePool` max from vitality** |
| `StaminaStatManager` MonoBehaviour | **`ResourcePool` stamina later** (same type) |

---

## Roadmap after this

| Doc (later) | Feature |
|-------------|---------|
| `combat-stamina.md` | Endurance → stamina pool + regen on `ResourcePool` |
| `combat-block-dodge.md` | Block + dodge i-frames as `CombatAction` |
| `combat-combos.md` | `CombatAction.next` chain |
| `ai-enemy-chase.md` | Enemy uses same `CombatAction` for attacks |

---

# Part A — Shared intent (move `PlayerIntent` to Core)

Combat reads input through **Core** so `AF.Combat` never references `AF.Player`.

## Files

```
Assets/_Project/Core/Runtime/
├── PlayerIntent.cs          ← MOVE from Player/Runtime (delete old)
└── IPlayerIntentSource.cs   ← NEW

Assets/_Project/Player/Runtime/
├── PlayerInputAdapter.cs    ← UPDATE
└── PlayerIntent.cs          ← DELETE after move
```

---

### `Assets/_Project/Core/Runtime/PlayerIntent.cs`

```csharp
using UnityEngine;

namespace AF.Core
{
    public struct PlayerIntent
    {
        public Vector2 Move;
        public Vector2 Look;
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

### `Assets/_Project/Player/Runtime/PlayerMotor.cs`

Add `using AF.Core;` at top. No logic change.

---

## Input actions (Unity Editor)

Open `Assets/_Project/Player/Input/PlayerInputActions.inputactions`:

| Action | Type | Keyboard | Gamepad |
|--------|------|----------|---------|
| `LightAttack` | Button | `Mouse/leftButton` | `buttonWest` |
| `Block` | Button | `Keyboard/leftShift` | `leftTrigger` |

Add both to **Gameplay** map. **Save Asset** → regenerate `PlayerInputActions.cs`.

---

## Part A checklist

- [ ] `PlayerIntent` in Core; delete `Player/Runtime/PlayerIntent.cs`
- [ ] `IPlayerIntentSource` + adapter implements it
- [ ] Input actions added; project compiles
- [ ] Play: move + dodge still work

---

# Part B — Stats (`AF.Stats`)

Pure C#. One modifier path for future equipment.

## Files

```
Assets/_Project/Stats/
├── AF.Stats.asmdef
├── StatId.cs
├── StatProfile.cs
├── StatModifier.cs
├── StatSheet.cs
├── DerivedStats.cs
├── ResourcePool.cs
├── DamageTypes.cs
└── DamageResolver.cs

Assets/_Project/Tests/EditMode/
├── StatSheetTests.cs
├── ResourcePoolTests.cs
├── DerivedStatsTests.cs
└── DamageResolverTests.cs
```

Update `AF.Tests.EditMode.asmdef` — add `"AF.Stats"` and `"AF.Core"` to references.

---

### `Assets/_Project/Stats/AF.Stats.asmdef`

```json
{
    "name": "AF.Stats",
    "rootNamespace": "AF.Stats",
    "references": [
        "AF.Core"
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

---

### `Assets/_Project/Stats/StatId.cs`

```csharp
namespace AF.Stats
{
    public enum StatId
    {
        Vitality,
        Endurance
    }
}
```

---

### `Assets/_Project/Stats/StatProfile.cs`

```csharp
using System;

namespace AF.Stats
{
    [Serializable]
    public struct StatProfile
    {
        public int Vitality;
        public int Endurance;

        public static StatProfile DefaultPlayer => new() { Vitality = 10, Endurance = 10 };
        public static StatProfile DefaultEnemy => new() { Vitality = 3, Endurance = 0 };
    }
}
```

---

### `Assets/_Project/Stats/StatModifier.cs`

```csharp
namespace AF.Stats
{
    public readonly struct StatModifier
    {
        public StatId Stat { get; }
        public int FlatDelta { get; }

        public StatModifier(StatId stat, int flatDelta)
        {
            Stat = stat;
            FlatDelta = flatDelta;
        }
    }
}
```

---

### `Assets/_Project/Stats/StatSheet.cs`

```csharp
using System;
using System.Collections.Generic;

namespace AF.Stats
{
    public sealed class StatSheet
    {
        readonly Dictionary<StatId, int> baseLevels = new();
        readonly Dictionary<string, List<StatModifier>> modifiersBySource = new();

        public StatSheet(StatProfile profile)
        {
            baseLevels[StatId.Vitality] = profile.Vitality;
            baseLevels[StatId.Endurance] = profile.Endurance;
        }

        public int GetTotal(StatId stat)
        {
            int total = baseLevels.TryGetValue(stat, out int baseLevel) ? baseLevel : 0;

            foreach (List<StatModifier> list in modifiersBySource.Values)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Stat == stat)
                    {
                        total += list[i].FlatDelta;
                    }
                }
            }

            return Math.Max(1, total);
        }

        public void SetBase(StatId stat, int level)
        {
            baseLevels[stat] = level;
        }

        public void AddModifiers(string sourceId, IReadOnlyList<StatModifier> modifiers)
        {
            if (string.IsNullOrEmpty(sourceId) || modifiers == null || modifiers.Count == 0)
            {
                return;
            }

            if (!modifiersBySource.TryGetValue(sourceId, out List<StatModifier> list))
            {
                list = new List<StatModifier>();
                modifiersBySource[sourceId] = list;
            }

            for (int i = 0; i < modifiers.Count; i++)
            {
                list.Add(modifiers[i]);
            }
        }

        public void RemoveModifiers(string sourceId)
        {
            modifiersBySource.Remove(sourceId);
        }
    }
}
```

---

### `Assets/_Project/Stats/DerivedStats.cs`

```csharp
namespace AF.Stats
{
    /// <summary>
    /// Jam formulas. Replace with data later without changing ResourcePool/HealthComponent.
    /// </summary>
    public static class DerivedStats
    {
        public const int HealthPerVitality = 10;
        public const int StaminaPerEndurance = 5;

        public static int MaxHealth(StatSheet sheet)
        {
            return sheet.GetTotal(StatId.Vitality) * HealthPerVitality;
        }

        public static int MaxStamina(StatSheet sheet)
        {
            return sheet.GetTotal(StatId.Endurance) * StaminaPerEndurance;
        }
    }
}
```

---

### `Assets/_Project/Stats/ResourcePool.cs`

```csharp
using System;

namespace AF.Stats
{
    public sealed class ResourcePool
    {
        public int Max { get; private set; }
        public int Current { get; private set; }
        public bool IsEmpty => Current <= 0;

        public ResourcePool(int max)
        {
            if (max <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(max));
            }

            Max = max;
            Current = max;
        }

        public void RefreshMax(int newMax)
        {
            if (newMax <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newMax));
            }

            Max = newMax;
            Current = Math.Min(Current, Max);
        }

        public void Fill()
        {
            Current = Max;
        }

        public bool TrySpend(int amount)
        {
            if (IsEmpty || amount <= 0 || Current < amount)
            {
                return false;
            }

            Current -= amount;
            return true;
        }

        public DamageResult ApplyDamage(int amount)
        {
            if (IsEmpty || amount <= 0)
            {
                return DamageResult.None;
            }

            int dealt = Math.Min(amount, Current);
            Current -= dealt;
            return new DamageResult(dealt, Current, Current <= 0);
        }
    }
}
```

---

### `Assets/_Project/Stats/DamageTypes.cs`

```csharp
namespace AF.Stats
{
    public readonly struct DamageRequest
    {
        public int Amount { get; }

        public DamageRequest(int amount)
        {
            Amount = amount;
        }
    }

    public readonly struct DamageResult
    {
        public int DamageDealt { get; }
        public int Remaining { get; }
        public bool Depleted { get; }

        public DamageResult(int damageDealt, int remaining, bool depleted)
        {
            DamageDealt = damageDealt;
            Remaining = remaining;
            Depleted = depleted;
        }

        public static DamageResult None => new(0, -1, false);
    }
}
```

---

### `Assets/_Project/Stats/DamageResolver.cs`

```csharp
namespace AF.Stats
{
    public static class DamageResolver
    {
        public static DamageResult Resolve(ResourcePool pool, DamageRequest request)
        {
            if (pool == null)
            {
                return DamageResult.None;
            }

            return pool.ApplyDamage(request.Amount);
        }
    }
}
```

---

### `Assets/_Project/Tests/EditMode/StatSheetTests.cs`

```csharp
using AF.Stats;
using NUnit.Framework;

namespace AF.Tests
{
    public class StatSheetTests
    {
        [Test]
        public void GetTotal_UsesBaseLevel()
        {
            var sheet = new StatSheet(new StatProfile { Vitality = 10, Endurance = 8 });

            Assert.AreEqual(10, sheet.GetTotal(StatId.Vitality));
            Assert.AreEqual(8, sheet.GetTotal(StatId.Endurance));
        }

        [Test]
        public void AddModifiers_IncreasesTotal()
        {
            var sheet = new StatSheet(StatProfile.DefaultPlayer);

            sheet.AddModifiers("ring_01", new[] { new StatModifier(StatId.Vitality, 3) });

            Assert.AreEqual(13, sheet.GetTotal(StatId.Vitality));
        }

        [Test]
        public void RemoveModifiers_RestoresTotal()
        {
            var sheet = new StatSheet(StatProfile.DefaultPlayer);
            sheet.AddModifiers("ring_01", new[] { new StatModifier(StatId.Vitality, 5) });

            sheet.RemoveModifiers("ring_01");

            Assert.AreEqual(10, sheet.GetTotal(StatId.Vitality));
        }
    }
}
```

---

### `Assets/_Project/Tests/EditMode/DerivedStatsTests.cs`

```csharp
using AF.Stats;
using NUnit.Framework;

namespace AF.Tests
{
    public class DerivedStatsTests
    {
        [Test]
        public void MaxHealth_ScalesWithVitality()
        {
            var sheet = new StatSheet(new StatProfile { Vitality = 10, Endurance = 0 });

            Assert.AreEqual(100, DerivedStats.MaxHealth(sheet));
        }

        [Test]
        public void MaxHealth_IncludesEquipmentModifier()
        {
            var sheet = new StatSheet(StatProfile.DefaultPlayer);
            sheet.AddModifiers("gear", new[] { new StatModifier(StatId.Vitality, 2) });

            Assert.AreEqual(120, DerivedStats.MaxHealth(sheet));
        }
    }
}
```

---

### `Assets/_Project/Tests/EditMode/ResourcePoolTests.cs`

```csharp
using AF.Stats;
using NUnit.Framework;

namespace AF.Tests
{
    public class ResourcePoolTests
    {
        [Test]
        public void ApplyDamage_ReducesCurrent()
        {
            var pool = new ResourcePool(100);

            DamageResult result = pool.ApplyDamage(30);

            Assert.AreEqual(30, result.DamageDealt);
            Assert.AreEqual(70, result.Remaining);
            Assert.IsFalse(result.Depleted);
        }

        [Test]
        public void ApplyDamage_ToZero_SetsDepleted()
        {
            var pool = new ResourcePool(25);

            DamageResult result = pool.ApplyDamage(25);

            Assert.IsTrue(result.Depleted);
            Assert.IsTrue(pool.IsEmpty);
        }

        [Test]
        public void RefreshMax_ClampsCurrent()
        {
            var pool = new ResourcePool(100);
            pool.ApplyDamage(40);

            pool.RefreshMax(50);

            Assert.AreEqual(50, pool.Max);
            Assert.AreEqual(50, pool.Current);
        }

        [Test]
        public void TrySpend_FailsWhenInsufficient()
        {
            var pool = new ResourcePool(10);

            Assert.IsFalse(pool.TrySpend(15));
            Assert.AreEqual(10, pool.Current);
        }
    }
}
```

---

### `Assets/_Project/Tests/EditMode/DamageResolverTests.cs`

```csharp
using AF.Stats;
using NUnit.Framework;

namespace AF.Tests
{
    public class DamageResolverTests
    {
        [Test]
        public void Resolve_DelegatesToPool()
        {
            var pool = new ResourcePool(50);

            DamageResult result = DamageResolver.Resolve(pool, new DamageRequest(15));

            Assert.AreEqual(15, result.DamageDealt);
            Assert.AreEqual(35, pool.Current);
        }
    }
}
```

---

## Part B checklist

- [ ] `AF.Stats` asmdef + all scripts
- [ ] `AF.Tests.EditMode` references `AF.Stats`
- [ ] Test Runner → Edit Mode → 4 stat test fixtures green

---

# Part C — Combat (`AF.Combat`)

One controller. Light attack is a `CombatAction` asset — same type heavies/spells use later.

## Files

```
Assets/_Project/Combat/
├── AF.Combat.asmdef
├── CombatAction.cs
├── CombatController.cs
├── CombatActor.cs
├── HealthComponent.cs
├── Hurtbox.cs
├── Hitbox.cs
├── PlayerDeathBridge.cs
├── ContactDamage.cs
└── DeathCleanup.cs

Assets/Data/Combat/
└── LightAttack_Unarmed.asset    ← create in Editor after scripts compile
```

---

### `Assets/_Project/Combat/AF.Combat.asmdef`

```json
{
    "name": "AF.Combat",
    "rootNamespace": "AF.Combat",
    "references": [
        "AF.Core",
        "AF.Stats"
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

---

### `Assets/_Project/Combat/CombatAction.cs`

```csharp
using UnityEngine;

namespace AF.Combat
{
    [CreateAssetMenu(fileName = "CombatAction", menuName = "AF/Combat/Combat Action")]
    public sealed class CombatAction : ScriptableObject
    {
        [Header("Costs (jam: leave stamina 0)")]
        public int staminaCost;

        [Header("Effect")]
        public int damage = 15;
        public float duration = 0.25f;

        [Header("Combo (later)")]
        public CombatAction next;

        [Header("Presentation (optional jam)")]
        public string animatorTrigger;
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
using AF.Core;
using UnityEngine;

namespace AF.Combat
{
    /// <summary>
    /// Single combat executor. Every player verb is a CombatAction asset.
    /// </summary>
    public sealed class CombatController : MonoBehaviour
    {
        [SerializeField] CombatAction lightAttack;
        [SerializeField] Hitbox hitbox;

        IPlayerIntentSource intentSource;
        CombatAction activeAction;
        float actionTimer;
        bool isExecuting;

        void Awake()
        {
            intentSource = GetComponent<IPlayerIntentSource>();
        }

        void Update()
        {
            if (intentSource == null || hitbox == null)
            {
                return;
            }

            if (isExecuting)
            {
                TickActiveAction();
                return;
            }

            if (intentSource.Intent.LightAttack && lightAttack != null)
            {
                TryStartAction(lightAttack);
            }
        }

        void TickActiveAction()
        {
            actionTimer -= Time.deltaTime;
            if (actionTimer <= 0f)
            {
                hitbox.EndSwing();
                isExecuting = false;
                activeAction = null;
            }
        }

        void TryStartAction(CombatAction action)
        {
            if (action == null || isExecuting)
            {
                return;
            }

            // Stamina gate later: ResourcePool.TrySpend(action.staminaCost)

            activeAction = action;
            isExecuting = true;
            actionTimer = action.duration;
            hitbox.ConfigureForAction(action);
            hitbox.BeginSwing();
        }

        public bool IsExecuting => isExecuting;
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

        public void ConfigureForAction(CombatAction action)
        {
            damage = action != null ? action.damage : 0;
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
2. Right-click → **Create → AF → Combat → Combat Action**
3. Name: `LightAttack_Unarmed`
4. Set **Damage** = `15`, **Duration** = `0.25`, **Stamina Cost** = `0`

---

## Player hierarchy

```
Player
├── ... existing motor / input / camera / control gate ...
├── CombatActor              baseProfile: Vitality 10, Endurance 10
├── HealthComponent          combatActor → CombatActor (no fallback needed)
├── Hurtbox                  ownerRoot = Player, health → HealthComponent
├── PlayerDeathBridge        health → HealthComponent
├── CombatController         lightAttack → LightAttack_Unarmed, hitbox → AttackHitbox
└── AttackHitbox             (child, inactive by default)
    ├── BoxCollider          Is Trigger, ~1×1×1.5 in front of player
    └── Hitbox               ownerRoot = Player
```

- `PlayerInputAdapter` already on Player → `CombatController` resolves `IPlayerIntentSource` via `GetComponent`.
- **Do not** add `AF.Player` reference to `AF.Combat` asmdef.

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
5. Console: no null refs on `IPlayerIntentSource` / `CombatController`

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
- [ ] Light attack kills enemy via `CombatController` (not a separate melee script)
- [ ] Player max HP = vitality × 10
- [ ] Contact damage → player death path

---

## Asmdef summary

| Assembly | References |
|----------|------------|
| `AF.Core` | — |
| `AF.Player` | `AF.Core` |
| `AF.Stats` | `AF.Core` |
| `AF.Combat` | `AF.Core`, `AF.Stats` |
| `AF.Tests.EditMode` | `AF.Core`, `AF.Stats`, `AF.Dungeon`, test runners |

**Never** `AF.Combat` → `AF.Player` or `AF.Player` → `AF.Combat`.

---

## Equipment preview (when you add loot)

```csharp
// Equip ring +3 Vitality
combatActor.ApplyEquipment(ringInstanceId, new StatModifier(StatId.Vitality, 3));
healthComponent.RefreshMaxFromStats();
// Max HP 100 → 130, current clamped up to new max
```

Same `StatSheet` + `ResourcePool` pattern will host stamina when you type `combat-stamina.md`.
