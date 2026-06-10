# Dungeon overlap / CanPlace investigation

## Summary

`CanPlace` was failing for **two separate reasons** — one in code, one in prefab authoring.

---

## 1. Code bug — footprint center (`RoomPrefabData`)

**Wrong:**
```csharp
root.InverseTransformPoint(box.center); // box.center is LOCAL to the collider child
```

**Right:**
```csharp
root.InverseTransformPoint(colliderTransform.TransformPoint(box.center));
```

Your prefabs use a **"Bounding Box"** child at `(-5, 3~5, -5)` with large scale. The old code treated collider center as world `(0,0,0)`, so the footprint was centered on the room root instead of on the bounding volume. Overlap checks used the wrong AABB.

**Fixed in:** `RoomPrefabData.cs`

---

## 2. Code bug — overlap test was 3D + full door rotation

Two sub-issues:

**Edge-touching:** Unity `Bounds.Intersects` treats face-touching as overlap. Fixed with penetration threshold on each axis.

**Rotated AABB inflation:** Door markers use pitch (e.g. `-90° X`). Alignment applies that full rotation to the room, which **inflates the world-axis AABB** of the footprint even when floor plans only touch on Z. Tall boxes also share Y extent, so a strict 3D test rejects valid chains.

**Fixed in:** `BoundsHelper`
- `ToPlacementBounds` — footprint uses **yaw only** (floor plan), ignores door pitch/roll
- `HasPenetratingOverlap` — **XZ only** (one floor); edge-touching still allowed via `0.01` penetration + `0.1` XZ shrink

---

## 3. Prefab authoring — doors outside footprint

From your prefabs (`Assets/Data/Dungeons/`):

| Prefab | Bounding Box | Exit local | Entrance local |
|--------|----------------|------------|----------------|
| Start Room | pos `(-5,3,-5)` scale `5` | `(-12.5, 2.5, 5)` | — |
| Middle Room | pos `(-5,5,-5)` scale `~18` | `(-7.5, 2.5, 5)` | `(-2.5, 2.5, -15)` |

Floor tiles span roughly **20×20** in local space (`0`, `±10` offsets). Doors at **z = 5** and **z = -15** sit **outside** the bounding box footprint on Z.

The solver aligns **doors**, but `CanPlace` tests **footprints**. When footprints are smaller than the door span, door-snapped rooms can still penetrate each other's footprint volumes (especially with door rotations like `-90° X`).

### What to do in Unity

1. Select **Bounding Box** on each room prefab.
2. Resize/reposition the `BoxCollider` so it **fully contains** all floor tiles **and** every door marker.
3. Prefer **identity rotation** on door transforms for jam (forward = connection axis).
4. Optional: rename `Bounding Box` → `RoomBounds` (code finds both names).

**Rule:** every `DoorEntrance` / `DoorExit` local position must be inside `RoomPrefabData` footprint. The generator now logs a warning if not.

---

## 4. Not the problem

- `DungeonLayoutSolver` alignment math — matches Cacildes
- `TestRooms.Box` tests — bypass prefab footprint building (always passed)
- Empty `occupied` list on start room — start placement should succeed once footprint is valid

---

## Verify

1. **Test Runner → Edit Mode** — run `BoundsHelperTests` + `DungeonLayoutSolverPrefabTests`
2. **Play** — watch Console for `RoomPrefabData: Exit/Entrance ... outside the footprint` warnings
3. Fix prefab bounds until warnings are gone
4. Regenerate dungeon (`DungeonGenerator` rebuilds templates each run)

---

## Quick checklist

- [ ] `RoomPrefabData.cs` uses `TransformPoint(box.center)`
- [ ] `BoundsHelper` uses penetration overlap (not raw `Intersects`)
- [ ] Bounding box wraps floors + doors on all 3 prefabs
- [ ] Door rotations simplified (identity if possible)
- [ ] `roomSize` in Inspector — jam default **5** (you had 15, which is harder to solve)
