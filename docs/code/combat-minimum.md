# Combat minimum — Stats, HP, light attack, one enemy

## Goal

First combat loop: **player light attack → enemy loses HP → enemy dies**. Enemy **contact damage** kills player → `RunCoordinator.NotifyPlayerDied()`.

Split into four parts you type in order. Plain C# in `AF.Stats`; adapters in `AF.Combat`. No lock-on, block, or poise yet.

**Prerequisite:** Player graybox + dungeon generator wired (or Graybox with player only).

---

## Roadmap after this

| Next doc (later) | Feature |
|------------------|---------|
| `combat-block-dodge.md` | Block + i-frames on dodge |
| `ai-enemy-chase.md` | Chase + telegraph attack |
| `lock-on.md` | Single-target lock |

---

# Part A — Shared intent (move `PlayerIntent` to Core)

Combat reads input through a **Core interface** so `AF.Combat` never references `AF.Player`.

## Files

```
Assets/_Project/Core/Runtime/
├── PlayerIntent.cs          ← MOVE from Player/Runtime (delete old file)
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

### `Assets/_Project/Player/Runtime/PlayerMotor.cs` — namespace fix only

Change `using`/namespace references if needed: add `using AF.Core;` at top (struct now lives in `AF.Core`). No logic change.

---

## Input actions (Unity Editor)

Open `Assets/_Project/Player/Input/PlayerInputActions.inputactions`:

| Action | Type | Keyboard | Gamepad |
|--------|------|----------|---------|
| `LightAttack` | Button | `Mouse/leftButton` | `buttonWest` (X on Xbox) |
| `Block` | Button | `Keyboard/leftShift` | `leftTrigger` |

Add both to the **Gameplay** map. **Save Asset** → Unity regenerates `PlayerInputActions.cs`.

---

## Part A checklist

- [ ] `PlayerIntent` moved to Core; old `Player/Runtime/PlayerIntent.cs` deleted
- [ ] `IPlayerIntentSource` added
- [ ] `PlayerInputAdapter` updated + implements interface
- [ ] Input actions added; generated C# compiles
- [ ] Play: move + dodge still work

---

# Part B — Stats (`AF.Stats`)

## Files

```
Assets/_Project/Stats/
├── AF.Stats.asmdef
├── DamageTypes.cs
├── HealthPool.cs
└── DamageResolver.cs

Assets/_Project/Tests/EditMode/
├── HealthPoolTests.cs
└── DamageResolverTests.cs
```

Update `AF.Tests.EditMode.asmdef` references — add `"AF.Stats"`.

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
        public int RemainingHealth { get; }
        public bool Killed { get; }

        public DamageResult(int damageDealt, int remainingHealth, bool killed)
        {
            DamageDealt = damageDealt;
            RemainingHealth = remainingHealth;
            Killed = killed;
        }

        public static DamageResult None => new(0, -1, false);
    }
}
```

---

### `Assets/_Project/Stats/HealthPool.cs`

```csharp
using System;

namespace AF.Stats
{
    public sealed class HealthPool
    {
        public int Max { get; }
        public int Current { get; private set; }
        public bool IsDead => Current <= 0;

        public HealthPool(int max)
        {
            if (max <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(max));
            }

            Max = max;
            Current = max;
        }

        public DamageResult ApplyDamage(int amount)
        {
            if (IsDead || amount <= 0)
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

### `Assets/_Project/Stats/DamageResolver.cs`

```csharp
namespace AF.Stats
{
    public static class DamageResolver
    {
        public static DamageResult Resolve(HealthPool pool, DamageRequest request)
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

### `Assets/_Project/Tests/EditMode/HealthPoolTests.cs`

```csharp
using AF.Stats;
using NUnit.Framework;

namespace AF.Tests
{
    public class HealthPoolTests
    {
        [Test]
        public void ApplyDamage_ReducesCurrent()
        {
            var pool = new HealthPool(100);

            DamageResult result = pool.ApplyDamage(30);

            Assert.AreEqual(30, result.DamageDealt);
            Assert.AreEqual(70, result.RemainingHealth);
            Assert.IsFalse(result.Killed);
            Assert.AreEqual(70, pool.Current);
        }

        [Test]
        public void ApplyDamage_ToZero_SetsKilled()
        {
            var pool = new HealthPool(25);

            DamageResult result = pool.ApplyDamage(25);

            Assert.IsTrue(result.Killed);
            Assert.IsTrue(pool.IsDead);
        }

        [Test]
        public void ApplyDamage_WhenDead_ReturnsNone()
        {
            var pool = new HealthPool(10);
            pool.ApplyDamage(10);

            DamageResult result = pool.ApplyDamage(5);

            Assert.AreEqual(0, result.DamageDealt);
            Assert.AreEqual(0, pool.Current);
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
            var pool = new HealthPool(50);

            DamageResult result = DamageResolver.Resolve(pool, new DamageRequest(15));

            Assert.AreEqual(15, result.DamageDealt);
            Assert.AreEqual(35, pool.Current);
        }
    }
}
```

---

## Part B checklist

- [ ] `AF.Stats` asmdef created
- [ ] `AF.Tests.EditMode` references `AF.Stats`
- [ ] Test Runner → Edit Mode → stats tests pass

---

# Part C — Combat adapters (`AF.Combat`)

## Files

```
Assets/_Project/Combat/
├── AF.Combat.asmdef
├── HealthComponent.cs
├── Hurtbox.cs
├── Hitbox.cs
├── PlayerMeleeAttack.cs
├── PlayerDeathBridge.cs
└── ContactDamage.cs

Assets/_Project/Tests/EditMode/
└── (no new tests — glue is thin; stats tests cover math)
```

Update `AF.Tests.EditMode.asmdef` — add `"AF.Combat"` only if you add combat logic tests later.

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

### `Assets/_Project/Combat/HealthComponent.cs`

```csharp
using System;
using AF.Stats;
using UnityEngine;

namespace AF.Combat
{
    public sealed class HealthComponent : MonoBehaviour
    {
        [SerializeField] int maxHealth = 100;

        HealthPool pool;

        public event Action<DamageResult> Damaged;
        public event Action Died;

        public int MaxHealth => pool?.Max ?? maxHealth;
        public int CurrentHealth => pool?.Current ?? 0;
        public bool IsDead => pool?.IsDead ?? false;

        void Awake()
        {
            pool = new HealthPool(maxHealth);
        }

        public void ApplyDamage(int amount)
        {
            if (pool.IsDead)
            {
                return;
            }

            DamageResult result = DamageResolver.Resolve(pool, new DamageRequest(amount));
            if (result.DamageDealt <= 0)
            {
                return;
            }

            Damaged?.Invoke(result);
            if (result.Killed)
            {
                Died?.Invoke();
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
        [SerializeField] int damage = 15;
        [SerializeField] Transform ownerRoot;

        readonly HashSet<Hurtbox> hitThisSwing = new();

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

### `Assets/_Project/Combat/PlayerMeleeAttack.cs`

```csharp
using AF.Core;
using UnityEngine;

namespace AF.Combat
{
    public sealed class PlayerMeleeAttack : MonoBehaviour
    {
        [SerializeField] Hitbox hitbox;
        [SerializeField] float swingDuration = 0.25f;

        IPlayerIntentSource intentSource;
        float swingTimer;
        bool swinging;

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

            if (swinging)
            {
                swingTimer -= Time.deltaTime;
                if (swingTimer <= 0f)
                {
                    hitbox.EndSwing();
                    swinging = false;
                }

                return;
            }

            if (intentSource.Intent.LightAttack)
            {
                swinging = true;
                swingTimer = swingDuration;
                hitbox.BeginSwing();
            }
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
    /// <summary>
    /// Enemy body contact — damages player hurtbox on stay (rate-limited).
    /// </summary>
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

# Part D — Unity setup (Player + enemy)

## Player hierarchy (add to existing Player)

```
Player
├── ... existing motor / input / camera ...
├── HealthComponent          maxHealth = 100
├── Hurtbox                  ownerRoot = Player root, health → HealthComponent
├── PlayerDeathBridge        health → HealthComponent
├── PlayerMeleeAttack        hitbox → child AttackHitbox
└── AttackHitbox             (child, disabled by default)
    ├── BoxCollider          Is Trigger = on, size ~1×1×1.5 in front of player
    └── Hitbox               damage = 15, ownerRoot = Player root
```

- `AttackHitbox` starts **inactive** — `Hitbox.BeginSwing()` enables it.
- Add **Combat** components on Player; `AF.Combat` does not need `AF.Player` asmdef reference (`GetComponent<IPlayerIntentSource>()` resolves at runtime).

## Enemy graybox (cube or capsule in Graybox scene)

```
Enemy_Graybox
├── HealthComponent          maxHealth = 30
├── Hurtbox                  ownerRoot = Enemy root, health → HealthComponent
├── CapsuleCollider          solid (not trigger) — blocks movement
└── ContactTrigger           (child, trigger wraps body)
    ├── SphereCollider       Is Trigger = on
    └── ContactDamage        ownerRoot = Enemy root, damagePerTick = 10
```

When enemy `HealthComponent` reaches 0, disable root or destroy (optional tiny script):

### Optional `Assets/_Project/Combat/DeathCleanup.cs`

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

Add `DeathCleanup` on enemy only (not player — player uses `PlayerDeathBridge`).

---

## Layers (optional but recommended)

| Layer | Used by |
|-------|---------|
| `Player` | Player root |
| `Enemy` | Enemy root |

No layer matrix required if owner-root checks are wired — layers are optional for jam.

---

## Play verification

1. **TitleScreen → New Run** → Graybox with enemy placed near player
2. **Left click / X** — enemy HP drops; enemy disables at 0 HP
3. Walk into enemy — player HP drops; at 0 `NotifyPlayerDied` fires (log / future death UI)
4. Console: no null refs on `IPlayerIntentSource` / `Hitbox`

---

## Full checklist

- [ ] Part A: intent in Core + input actions
- [ ] Part B: `AF.Stats` + tests green
- [ ] Part C: `AF.Combat` scripts compile
- [ ] Part D: Player + enemy wired in scene
- [ ] Light attack kills enemy
- [ ] Contact damage triggers player death path

---

## Asmdef summary

| Assembly | New references |
|----------|----------------|
| `AF.Core` | unchanged |
| `AF.Player` | unchanged (`AF.Core` already) |
| `AF.Stats` | `AF.Core` |
| `AF.Combat` | `AF.Core`, `AF.Stats` |
| `AF.Tests.EditMode` | add `AF.Stats` |

**Never** add `AF.Player` → `AF.Combat` or `AF.Combat` → `AF.Player`.
