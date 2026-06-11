# AF — Agent & Architecture Rules

> **Canonical (Cursor auto-load):** [.cursor/rules/coding-and-architecture.md](.cursor/rules/coding-and-architecture.md)  
> This file is a mirror for quick access. Edit the `.cursor` copy first.

> **Cacildes Adventure 2 — rebuilt with clean architecture.**  
> Cacildes = gameplay/domain reference + **anti-pattern catalog**. Do not mirror its class structure.  
> Unity **6000.3**, URP, Input System, UI Toolkit, 3rd-person + lock-on.

**Namespaces:** `AF.Core`, `AF.Player`, `AF.Dungeon`, `AF.Stats`, `AF.Combat`, … — **not** `Rogue.*`.

---

## Mission (summary)

- Build the **full game** Cacildes was aiming at, with **simpler, scalable code**.
- **Ship vertical slices** — each slice uses **final architecture**, not throwaway shortcuts.
- **Defer content, not structure.** Missing spells is fine; forking combat pipelines is not.

## Agent must push back when

- New god `*Manager`, dual combat pipelines, `CombatController` reading player input
- Two stat modifier systems (Cacildes `StatsController` + `AttributeController`)
- “Jam shortcut / refactor later” on **core** systems
- Porting Cacildes **structure** “because it worked”

**Say:** what Cacildes did, what was wrong, what AF does instead.

---

## Code delivery (§2)

**The user writes all game code.** Agents deliver **`docs/code/<feature>.md`** with full copy-paste file contents + Edit Mode tests in `Feature/Tests/`.

---

## Architecture anchors

| System | AF shape (not Cacildes) |
|--------|-------------------------|
| **Dungeon** | Pure `DungeonLayoutSolver` + thin `DungeonGenerator` |
| **Stats** | `StatSheet` → `DerivedStats` → `ResourcePool` (one modifier path) |
| **Combat** | `abstract CombatAction` + **`CombatController.TryStart`** — player **and** AI |
| **Player** | Composition + `PlayerCombatInput` adapter — **no** `PlayerManager` |
| **Tests** | `Feature/Tests/AF.Feature.Tests.asmdef` — not monolithic `AF.Tests.EditMode` |

Full detail: §9 Combat & Stats, §4 Cacildes lessons — in canonical rules file.

---

## Current milestone

**M1 locomotion:** [docs/code/player-locomotion-camera.md](docs/code/player-locomotion-camera.md) — move, jump, dodge, camera sphere-cast collision.

**M2 combat (parallel):** [docs/code/combat-minimum-v2.md](docs/code/combat-minimum-v2.md) — Stats + unified combat.

| Slice | Package | What |
|-------|---------|------|
| Locomotion A–G | Core + Player | Intent in Core + Jump; motor, dodge, camera collision, tests |
| Combat A | Core + Player | `PlayerIntent` in Core; attack/block input (overlap with locomotion A) |
| Combat B | `AF.Stats` | `StatSheet`, `ResourcePool`, vitality → HP + tests |
| Combat C | `AF.Combat` | `CombatAction` hierarchy, `CombatController`, adapters |
| Combat D | Unity | Wire player + enemy graybox |

---

## Dungeon — locked tech decisions (§10)

| Decision | Value |
|----------|-------|
| Asmdef | `AF.Dungeon` |
| Default `roomSize` | 5 (tunable) |
| Collision | Floor tiles — `RoomFloorTile` + `MeshFilter` |
| Seed | `RunCoordinator.Instance.Session.Seed` |

---

_Full rules: [.cursor/rules/coding-and-architecture.md](.cursor/rules/coding-and-architecture.md)_
