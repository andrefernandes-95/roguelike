# Dungeon — room prefabs & categories (Unity Editor)

## Goal

Author placeholder room prefabs and category ScriptableObjects so `DungeonGenerator` (slice 4) has something to spawn. **No new C#** — Editor work only.

**Prerequisite:** slice 3 code compiles (`RoomPrefabData`, `RoomCategoryData`, door markers).

---

## Folder layout

```
Assets/_Project/Dungeon/
├── Prefabs/
│   ├── Room_Start.prefab
│   ├── Room_Mid.prefab
│   ├── Room_End.prefab
│   ├── Room_Connector.prefab
│   ├── Room_Side.prefab
│   └── DeadEnd_Wall.prefab
└── Categories/
    ├── Cat_Start.asset
    ├── Cat_Mid.asset
    ├── Cat_End.asset
    ├── Cat_Connector.asset
    └── Cat_Side.asset
```

---

## Room prefab recipe (repeat per variant)

Use **10×2×10** footprint for Start/Mid/End/Side; **8×2×8** for Connector (jam default).

### Hierarchy

```
Room_Start          ← root, add RoomPrefabData
├── RoomBounds      ← BoxCollider, NOT trigger, size 10×2×10, center (0,1,0) optional
├── DoorExit        ← local pos (0, 0, 5), rotation identity, forward = +Z (red gizmo out)
├── DoorEntrance    ← local pos (0, 0, -5), rotation identity, forward = +Z (blue gizmo in)
├── PlayerSpawn     ← START ROOM ONLY, local pos (0, 0, 0)
└── Floor           ← Cube scaled 10×0.2×10 for visuals
```

### Door forward rules

| Marker | Position | `transform.forward` should point |
|--------|----------|----------------------------------|
| `DoorExit` | +Z edge (`z = size/2`) | **Out** of room (+Z) |
| `DoorEntrance` | -Z edge (`z = -size/2`) | **Into** room (+Z) |

Default rotation `identity` on both works if exit is on +Z and entrance on -Z.

### Steps per room

1. Create empty `Room_Start` in Hierarchy
2. Add `RoomPrefabData` on root
3. Child `RoomBounds` → `BoxCollider`, size **10, 2, 10**, **Is Trigger = off**
4. Child `DoorExit` → add `DoorExit` component, position **(0, 0, 5)**
5. Child `DoorEntrance` → add `DoorEntrance` component, position **(0, 0, -5)**
6. Child `PlayerSpawn` (start room only) at origin
7. Child `Floor` — cube for visibility
8. Drag root to `Assets/_Project/Dungeon/Prefabs/` → save prefab, delete hierarchy instance

### Variants

| Prefab | Notes |
|--------|-------|
| `Room_Start` | One `DoorExit` only (no entrance) — or entrance unused; jam: **exit + entrance** like others is fine |
| `Room_Mid` | Exit + entrance (standard box) |
| `Room_End` | Exit + entrance; this category gets `sideRoomChance` on the SO |
| `Room_Connector` | Smaller bounds **8×2×8**, doors at **z = ±4** |
| `Room_Side` | Standard 10×10 box |
| `DeadEnd_Wall` | Cube **4×3×0.5** with collider — no `RoomPrefabData`, no doors |

> **Prefab name = template Id.** Keep names exactly `Room_Start`, `Room_Mid`, etc.

---

## Category ScriptableObjects

**Create → AF/Dungeon/Room Category** for each:

| Asset | `categoryName` | `prefabs` | Extra |
|-------|----------------|-----------|-------|
| `Cat_Start` | Start | `Room_Start` | — |
| `Cat_Mid` | Mid | `Room_Mid` | — |
| `Cat_End` | End | `Room_End` | `sideRoomChance = 0.5`, `sideRoomCategory = Cat_Side` |
| `Cat_Connector` | Connector | `Room_Connector` | — |
| `Cat_Side` | Side | `Room_Side` | — |

---

## Graybox scene prep (before slice 4)

1. Disable or delete static **Ground** (generator replaces layout)
2. Remove duplicate **RunCoordinator** on Graybox if present (TitleScreen DDOL instance is canonical)
3. Keep **Player** in scene — you'll wire it on `DungeonGenerator` in slice 4

---

## Verify prefabs (Scene view)

- Select each prefab → doors show **red** (exit) and **blue** (entrance) gizmo rays
- `RoomPrefabData` on root → Inspector shows `Bounds Collider` auto-filled from `RoomBounds` child (after fix doc)

Optional play-mode smoke: temporary script or debugger calling `RoomPrefabData.BuildTemplateFromPrefab(yourPrefab)` → should return non-null template with 1 exit + 1 entrance.

---

## Checklist

- [ ] 5 room prefabs + 1 dead-end prefab in `Prefabs/`
- [ ] 5 category assets in `Categories/`
- [ ] `Cat_End` references `Cat_Side` for side rooms
- [ ] Door gizmos look correct on all prefabs
- [ ] Ready to type slice 4 from [dungeon-generator.md](dungeon-generator.md)
