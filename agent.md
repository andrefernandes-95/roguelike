# AF — Agent & Architecture Rules

> **Canonical (Cursor auto-load):** [.cursor/rules/coding-and-architecture.md](.cursor/rules/coding-and-architecture.md)  
> This file is a mirror for quick access. Edit the `.cursor` copy first.

> Soulslike combat + roguelike runs. **Indie game jam scope.**  
> Reference: `Cacildes Adventure 2` (inspiration only).  
> Unity **6000.3**, URP, Input System, UI Toolkit, 3rd-person + lock-on.

**Namespaces:** `AF.Core`, `AF.Player`, `AF.Dungeon`, … — **not** `Rogue.*`.

---

## Code delivery (§2)

**Agents implement directly** in `Assets/_Project/` — `.cs`, `.asmdef`, `.uxml`, `.uss`.

- **Unit tests** (Edit Mode) required with every plain C# logic change
- Optional `docs/code/*.md` for large slices only
- Summarize changes + Inspector/scene wiring for the user

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

**Next:** `DungeonGenerator` adapter (slice 4).

---

_Full rules: see [.cursor/rules/coding-and-architecture.md](.cursor/rules/coding-and-architecture.md)_
