# AF — Agent & Architecture Rules

> **Canonical (Cursor auto-load):** [.cursor/rules/coding-and-architecture.md](.cursor/rules/coding-and-architecture.md)  
> This file is a mirror for quick access. Edit the `.cursor` copy first.

> **Cacildes Adventure 2 — rebuilt with clean architecture.**  
> Cacildes = gameplay/domain reference + **anti-pattern catalog**. Do not mirror its class structure.  
> Unity **6000.3**, URP, Input System, UI Toolkit, 3rd-person + lock-on.

**Namespaces:** `AF.Core`, `AF.Character`, `AF.Player`, `AF.Combat`, `AF.Dungeon`, … — **not** `Rogue.*`.

---

## Mission (summary)

- Build the **full game** Cacildes was aiming at, with **simpler, scalable code**.
- **Ship vertical slices** — each slice uses **final architecture**, not throwaway shortcuts.
- **Defer content, not structure.** Missing spells is fine; forking combat pipelines is not.

## Player is a character (§8 — mandatory)

The **player prefab = humanoid character + player adapters**. Body systems are **entity-agnostic**.

| Package | Owns |
|---------|------|
| **`AF.Character`** | `CharacterMotor`, `CharacterAnimationDriver`, `CharacterLocomotionView`, root motion, animation set |
| **`AF.Combat`** | `CombatController`, `CombatAction` (incl. `DodgeCombatAction`), `CombatAnimationEvents` |
| **`AF.Core`** | `IActionAnimator`, `ILocomotionReadout`, `PlayerIntent`, `IPlayerIntentSource` |
| **`AF.Player`** | **Only** `PlayerInputAdapter`, `PlayerLocomotionInput`, `PlayerCombatInput`, camera, control gate |

**Reject:** `PlayerMotor`, `PlayerDodge`, `PlayerView`, `PlayerAnimationDriver` — enemies would need duplicates.

**Dodge** = `DodgeCombatAction` + `IActionAnimator`, not a `PlayerDodge` component.

**Asmdef:** `Character` → `Core`; `Player` → `Core`, `Character`, `Combat`; `Combat` must **not** → `Player` or `Character`.

**Docs:** [character-animations.md](docs/code/character-animations.md)

---

## Agent must push back when

- New god `*Manager`, dual combat pipelines, `CombatController` reading player input
- **`Player*` on shared body systems** (`PlayerMotor`, `PlayerDodge`, …) — use `Character*` + adapters
- Two stat modifier systems (Cacildes `StatsController` + `AttributeController`)
- Runtime `AnimatorOverrideController` creation (Cacildes `AnimatorOverrideHandler`)
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
| **Character** | `CharacterMotor` + `CharacterAnimationDriver` — player **and** AI humanoids |
| **Dungeon** | Pure `DungeonLayoutSolver` + thin `DungeonGenerator` |
| **Stats** | `StatSheet` → `DerivedStats` → `ResourcePool` (one modifier path) |
| **Combat** | `abstract CombatAction` + **`CombatController.TryStart`** — player **and** AI |
| **Player** | Thin adapters only — **no** motor, dodge, or animator on `AF.Player` |
| **Tests** | `Feature/Tests/AF.Feature.Tests.asmdef` — not monolithic `AF.Tests.EditMode` |

Full detail: §8 Character & Player, §9 Combat & Stats — in canonical rules file.

---

## Current milestone

**M1 locomotion:** [docs/code/player-locomotion-camera.md](docs/code/player-locomotion-camera.md) — prefer `CharacterMotor` + `PlayerLocomotionInput`.

**M1 animations:** [docs/code/character-animations.md](docs/code/character-animations.md) — `AF.Character` root-motion; dodge via `DodgeCombatAction`.

**M2 combat:** [docs/code/combat-minimum-v2.md](docs/code/combat-minimum-v2.md) — Stats + unified combat.

| Slice | Package | What |
|-------|---------|------|
| Character body | `AF.Character` | Motor, anim driver, view, root motion |
| Player adapters | `AF.Player` | Locomotion input, combat input, camera, gate |
| Combat | `AF.Combat` | Actions, controller, animation clip events |

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
