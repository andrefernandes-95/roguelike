# AF — Agent & Architecture Rules

> **Canonical (Cursor auto-load):** [.cursor/rules/coding-and-architecture.md](.cursor/rules/coding-and-architecture.md)  
> This file is a mirror for quick access. Edit the `.cursor` copy first.

> Soulslike combat + roguelike runs. **Indie game jam scope.**  
> Reference: `Cacildes Adventure 2` (inspiration only).  
> Unity **6000.3**, URP, Input System, UI Toolkit, 3rd-person + lock-on.

**Namespaces:** `AF.Core`, `AF.Player`, `AF.Dungeon`, … — **not** `Rogue.*`.

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

Delivery order: `dungeon-types-and-bounds.md` → `dungeon-solver.md` → `dungeon-prefab-authoring.md` → `dungeon-generator.md`

---

## Code delivery (§2)

User writes all `.cs` / `.asmdef`. Agents deliver **`docs/code/*.md`** with:

- Full production file contents
- **Unit tests** (Edit Mode) for every plain C# logic slice — mandatory
- Setup steps + verify checklist (must include **tests pass**)

---

_Full rules: see [.cursor/rules/coding-and-architecture.md](.cursor/rules/coding-and-architecture.md)_
