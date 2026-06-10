# AF — Agent & Architecture Rules

> **Canonical (Cursor auto-load):** [.cursor/rules/coding-and-architecture.md](.cursor/rules/coding-and-architecture.md)  
> This file is a mirror for quick access. Edit the `.cursor` copy first.

> Soulslike combat + roguelike runs. **Indie game jam scope.**  
> Reference: `Cacildes Adventure 2` (inspiration only).  
> Unity **6000.3**, URP, Input System, UI Toolkit, 3rd-person + lock-on.

**Namespaces:** `AF.Core`, `AF.Player`, `AF.Dungeon`, … — **not** `Rogue.*`.

---

## Code delivery (§2)

**The user writes all game code.** Agents deliver **`docs/code/<feature>.md`** with full copy-paste file contents.

- **Unit tests** (Edit Mode) required in every logic delivery
- Unity setup steps + verify checklist in each doc
- Agents do not edit `.cs` / `.asmdef` in `Assets/_Project/` unless explicitly asked

---

## Dungeon — locked decisions (§10)

| Decision | Value |
|----------|-------|
| Asmdef | `AF.Dungeon` |
| `roomSize` (jam) | **5** |
| Side rooms | Keep |
| Connectors | Keep |
| Collision | One `BoxCollider` footprint per room |
| Seed | `RunCoordinator.Instance.Session.Seed` |

## Combat — next up

**Type from:** [docs/code/combat-minimum-v2.md](docs/code/combat-minimum-v2.md) — Parts A → B → C → D. (Supersedes `combat-minimum.md`.)

| Part | Package | What |
|------|---------|------|
| A | Core + Player | Move `PlayerIntent` to Core; add Attack/Block input |
| B | `AF.Stats` | `StatSheet`, `ResourcePool`, vitality → max HP + tests |
| C | `AF.Combat` | `CombatAction` + `CombatController`, hitbox/hurtbox, death bridge |
| D | Unity | Wire player + one `Enemy_Graybox` |

---

_Full rules: see [.cursor/rules/coding-and-architecture.md](.cursor/rules/coding-and-architecture.md)_
