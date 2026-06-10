# Dungeon — floor tile collision

## Rules (strict)

- Every walkable slab: **`RoomFloorTile` + `MeshFilter`** on the same GameObject.
- **All** floor children need the component (Middle Room has 10 slabs — tag every one).
- Missing component or mesh → `BuildTemplate` returns `null` + warning.
- Solver uses **yaw-only** tile rotation (door pitch does not tilt the floor plan).
- Plane meshes bake as **0.5m thick** horizontal slabs for overlap.

## Prefab layout

```
Room (root)
├── RoomPrefabData
├── DoorEntrance
├── DoorExit
├── Floor          ← RoomFloorTile + MeshFilter (Plane/Cube)
├── Floor (1)
└── Floor (2)
```

1. Delete old `Bounding Box` children (unused).
2. Add **`RoomFloorTile`** to each floor child that has a mesh.

## Code

| File | Role |
|------|------|
| `RoomFloorTile.cs` | Required marker |
| `RoomPrefabData.cs` | Bakes `RoomFloorTile` mesh bounds + doors |
| `BoundsHelper.cs` | Per-tile overlap (Cacildes shrink + `Intersects`) |
| `DungeonTypes.cs` | `List<Bounds> FloorTiles` |

## Verify

**Test Runner → Edit Mode:** `BoundsHelperTests`, `DungeonLayoutSolverTests`, `DungeonLayoutSolverRealPrefabTests`

**Play:** Graybox, `roomSize = 3`. If layout fails, Console will name the prefab missing `RoomFloorTile` or `MeshFilter`.
