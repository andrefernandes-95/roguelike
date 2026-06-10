# Combat architecture review — vs Cacildes & unified goals

Review of [combat-minimum.md](combat-minimum.md) against **Cacildes Adventure 2** and your targets:

- One system for **combat controller + abilities** (redesigned, not ported wholesale)
- **Health / stamina / mana** as resource modules tied to stats (vitality, endurance, …)
- Equipment modifiers without Cacildes-level complexity

---

## Verdict

| Area | `combat-minimum.md` today | Right direction? |
|------|---------------------------|------------------|
| Part A — `PlayerIntent` in Core + `IPlayerIntentSource` | Matches `coding-and-architecture.md` §8 | **Yes** — keep |
| Part B — `HealthPool(int max)` standalone | Fixed max at construct; no stats | **Partial** — needs one revision before you type it |
| Part C — `PlayerMeleeAttack` + `HealthComponent` | Parallel path to future abilities | **No** — repeats Cacildes split |
| Hitbox / Hurtbox / `DamageResolver` | Thin glue, testable math | **Yes** — keep |
| Asmdef boundaries (no Combat ↔ Player) | Clean | **Yes** — keep |

**Summary:** The doc is a good **graybox slice**, but Parts B and C **diverge** from both your unified-combat goal and §9 in `coding-and-architecture.md` (`StatSheet`, `StatModifier`, resource pools). Type Part A as written; **revise B/C** using the target shape below so you do not throw away code when abilities and gear land.

---

## What Cacildes actually does (relevant parts)

### Combat is split in two — this is the main problem

| Path | Entry | Execution |
|------|--------|-----------|
| **Light / combo melee** | `PlayerCombatController` (~540 lines) | Animator state names, combo index, `StaminaStatManager`, weapon speed — **not** `Ability` SOs |
| **Heavy / spells / AI skills** | Same controller *or* AI → `CharacterAbilityManager` | `Ability` ScriptableObject → `RuntimeAbility` → `Prepare` / `Use` / `Finished` |

Light attacks never go through the ability pipeline. Heavy attacks call `characterAbilityManager.QueueAbility(combatAbility)`. That duplication is where combo bugs, stamina edge cases, and “can I attack?” logic drift apart.

### Stats are three layers deep

```
StatsController          RuntimeStat (vitality, endurance, …) + equipment modifiers by UUID
        ↓
AttributeController      30+ CharacterAttribute SOs with interval formulas
        ↓
RuntimeAttribute         current/max for health, stamina, mana, attacks, defenses, resistances…
        +
StaminaStatManager       separate regen, costs, PlayerStatsDatabase persistence
ManaManager              same pattern again
CharacterBaseHealth      TakeDamage / RestoreHealth on top of attributes
```

Equipment touches **both** `RuntimeStat` (level bumps) **and** `RuntimeAttribute` (flat/percent modifiers). Two modifier systems for one ring swap.

### God wiring

`CharacterBaseManager` holds 20+ component references. `PlayerComponentManager` enables/disables `playerCombatController`, dodge, block, locomotion together. `PlayerCombatController` reaches into stamina, weapons, menus, climb, swim, UI, abilities.

Your Rogue rules already reject this (`PlayerEntity` composition, no megaclass).

---

## What `combat-minimum.md` gets right

1. **Intent boundary** — Combat reads `IPlayerIntentSource`, not `AF.Player` asmdef. Same idea as Cacildes input listeners, but cleaner.
2. **Pure damage math** — `DamageRequest` → `DamageResolver` → `DamageResult` in `AF.Stats` with Edit Mode tests.
3. **Hitbox / Hurtbox** — Same collision model as Cacildes `CharacterWeaponHitbox` / damage receiver, without scaling formulas on day one.
4. **Phased delivery** — Graybox enemy + death bridge to `RunCoordinator` before block/dodge/AI docs.

---

## Where the plan drifts from your goals

### 1. Health is not a “stat module” yet

```csharp
// combat-minimum.md today
HealthPool pool = new HealthPool(maxHealth);  // serialized on HealthComponent
```

Cacildes derives max HP from **vitality** via `AttributeController.health` → `RuntimeAttribute.GetTotalValue(StatsController)`.

Your doc hard-codes `maxHealth = 100`. When vitality or a +HP ring appears, you rewrite `HealthComponent` instead of recomputing max from `StatSheet`.

### 2. `PlayerMeleeAttack` is a third combat stack

You want **one** controller executing **actions/abilities**. The doc adds a dedicated melee MonoBehaviour that will fight the future ability executor for:

- “Am I busy?”
- Cooldown / stamina gate
- Hitbox open/close timing

That recreates Cacildes: `PlayerCombatController` for lights, something else for heavies.

### 3. §9 in your own architecture doc is ahead of `combat-minimum.md`

`coding-and-architecture.md` already says:

> **`Stats`**: `StatSheet`, `StatModifier`, resource pools. Pure math.  
> Weapons/abilities are **data** (ScriptableObject) + **executor** (plain C#).

`combat-minimum.md` skips `StatSheet` / `StatModifier` entirely.

---

## Target architecture (simpler than Cacildes, ready for gear)

One pipeline for **every** player combat verb (light, heavy, spell, dodge attack later):

```
Input → PlayerIntent → CombatController (single MonoBehaviour on player)
                              │
                              ├─ CanExecute?(CombatAction, CombatActor)
                              ├─ PayCosts (stamina/mana from ResourcePools)
                              └─ ActionRunner runs active CombatAction
                                      ├─ timing (duration, hitbox windows)
                                      ├─ animator trigger (via ICombatView)
                                      └─ Hitbox.BeginSwing / EndSwing
```

### `AF.Stats` (pure C# — jam subset)

```text
StatId              enum: Vitality, Endurance, … (add as needed)
StatSheet           base level per stat + modifiers keyed by sourceId (equipment UUID)
StatModifier        { StatId, FlatDelta }   // one system only
DerivedStats        static formulas: MaxHealth(Vitality), MaxStamina(Endurance), MaxMana(Intelligence)
ResourcePool        Current, Max, ApplyDelta, IsEmpty; Max from DerivedStats + StatSheet
```

- **Health** = `ResourcePool` bound to vitality-derived max (same type stamina/mana use later).
- **No** `AttributeController` with 30 SOs at jam start.
- **No** separate `StaminaStatManager` MonoBehaviour — regen ticks live on a thin `StaminaComponent` that owns a `ResourcePool` and reads max from endurance.

Equipment (later):

```csharp
statSheet.AddModifier(gearInstanceId, new StatModifier(StatId.Vitality, +3));
resourcePool.RefreshMaxFromStats();  // HP max 100 → 115, clamp current
```

One modifier path. Cacildes needs two.

### `AF.Combat`

| Type | Role |
|------|------|
| `CombatAction` (SO) | Data: damage, stamina cost, swing duration, hitbox profile, animator trigger |
| `CombatController` | Reads intent, picks action, gates, runs runner |
| `CombatActor` | MonoBehaviour: `StatSheet`, health/stamina pools, hurtbox ref |
| `HealthComponent` | Wraps health `ResourcePool`, events, `ApplyDamage` |
| Hitbox / Hurtbox | Unchanged from combat-minimum |

**Jam light attack** = one `CombatAction` asset (`LightAttack_Unarmed`) referenced by `CombatController`. Not a separate `PlayerMeleeAttack` class.

### What we deliberately drop from Cacildes (jam)

- Combo chains via animator name tables in code (later: combo as linked `CombatAction` `next` field — same as Cacildes `Ability.next` but **one** type)
- `CharacterBaseAttackManager` scaling grades on day one (flat damage on `CombatAction` first)
- Poise, posture, 12 damage types, status resistances
- `PlayerManager` / `PlayerComponentManager` enable-disable webs

---

## Revised slice order (still shippable)

| Slice | Deliver | Playable result |
|-------|---------|-----------------|
| **A** | Intent in Core (as doc) | Move + attack input |
| **B′** | `StatSheet` + `ResourcePool` + `DerivedStats.MaxHealth` + tests | HP max from vitality level |
| **C′** | Hitbox/Hurtbox + `CombatAction` SO + `CombatController` + `HealthComponent` | Light attack kills graybox enemy |
| **D** | Scene wiring + `PlayerDeathBridge` | Same as doc Part D |
| Later | `ResourcePool` stamina + endurance; dodge i-frames doc | Block/dodge |
| Later | Second `CombatAction` for heavy spell | Proves unified pipeline |

Part B′ can start with **vitality = 10 → 100 HP** hardcoded formula in `DerivedStats` (no ScriptableObject formulas). Replace with data later without changing `HealthComponent`.

---

## Mapping Cacildes → Rogue (what to port in spirit)

| Cacildes | Rogue replacement |
|----------|-------------------|
| `PlayerCombatController` + `CharacterAbilityManager` | **`CombatController` + `CombatAction`** |
| `Ability` / `RuntimeAbility` | **`CombatAction` + runtime `ActiveAction` struct** (plain C#, no SO at runtime) |
| `StatsController` + `RuntimeStat` | **`StatSheet`** |
| `AttributeController` + 30 attributes | **`DerivedStats` static formulas** (2–3 numbers for jam) |
| `StaminaStatManager` | **`ResourcePool` stamina** + small regen component |
| `CharacterBaseHealth` | **`HealthComponent`** on `ResourcePool` |

---

## Action items

1. **Type Part A** from `combat-minimum.md` unchanged.
2. **Replace Part B** with `StatSheet` + `ResourcePool` + vitality → max HP before typing combat adapters.
3. **Replace `PlayerMeleeAttack`** with `CombatController` + one `CombatAction` ScriptableObject.
4. **Keep** hitbox/hurtbox/death bridge/contact damage from Part C/D.
5. When you want the full typed delivery, ask for **`combat-minimum-v2.md`** (or we rename the existing doc) with revised Parts B′/C′.

You are heading in the **right direction** on boundaries and testability. The correction is narrow: **do not fork melee vs abilities in the first slice**; **do not freeze max HP outside the stat module** — both are cheap to get right now and expensive to fix after graybox.
