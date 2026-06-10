---
alwaysApply: true
---

# AF — Agent & Architecture Rules

> Soulslike combat + roguelike runs. **Indie game jam scope.**
> Reference project: `Cacildes Adventure 2` (inspiration only — do not copy structure wholesale).
> Unity **6000.3**, URP, **New Input System**, **UI Toolkit**, 3rd-person camera + lock-on.

---

## 1. Mission

Build a **playable jam game**, not a platform for every future feature.

Every file, class, and system must earn its place in a **1–2 week** scope. When in doubt, cut scope and keep the code dumb.

---

## 2. Code delivery (mandatory)

**The user writes all game code.** Agents do not create or edit `.cs`, `.uxml`, `.uss`, `.asmdef`, or other source assets unless the user explicitly asks otherwise.

### What agents deliver

Every implementation task is delivered as a **markdown file** in `docs/code/`:

```
docs/code/
├── core-bootstrap.md
├── player-motor.md
└── ...
```

Each delivery file must include:

1. **Goal** — one paragraph, what this adds and why
2. **Files to create** — full paths under `Assets/_Project/`
3. **Full file contents** — copy-paste ready code blocks, one block per file, labeled with the target path
4. **Unit tests** — Edit Mode tests for every plain C# logic class (solver, bounds, state machine, damage math, etc.). Include full test file contents in the same delivery.
5. **Unity setup steps** — Inspector wiring, scene objects, play-mode verification
6. **Checklist** — compile, Test Runner → Edit Mode → pass, play, keyboard/gamepad (if UI)

### Agent rules

| Do | Don't |
|----|-------|
| Write complete `docs/code/<feature>.md` handoffs | Create or edit `.cs` / `.asmdef` in `Assets/_Project/` (unless user explicitly asks) |
| Include full copy-paste file contents in the doc | Partial snippets with `// ... rest` |
| Include Edit Mode tests in every logic delivery | Skip tests without a one-line justification |
| State package/asmdef impact in the doc | Silently add assembly references in the repo |
| One feature slice per delivery | Touch unrelated files “while here” |

---

## 3. Code philosophy

Write for your **dumber future self** at 2 AM before a build.

| Do                                                            | Don't                                                                 |
| ------------------------------------------------------------- | --------------------------------------------------------------------- |
| Short files, one job                                          | God classes (`PlayerManager`, `SaveManager`, `GameManager`)           |
| Composition (components)                                      | Deep inheritance trees (`CharacterBaseManager` → `PlayerManager` → …) |
| Plain C# for rules & state                                    | Business logic in `Update()`                                          |
| Explicit data flow                                            | Global string events (`EventManager` + `ON_*` constants)              |
| Serialize references in the Inspector                         | `FindObjectOfType` / `FindAnyObjectByType` at runtime                 |
| Copy patterns that worked in Cacildes (`DungeonLayoutSolver`) | Copy patterns that rotted (40-field managers, 700-line saves)         |

**The dumb code test:** If you cannot explain what a class does in one sentence without "and also", split it.

**The jam test:** If a system is not needed for **boot → new run → fight → die or win → meta reward → repeat**, it does not get built yet.

---

## 4. Lessons from Cacildes (read before writing code)

### Keep (proven value)

- **`DungeonLayoutSolver`** — pure C# layout logic; Unity adapter (`DungeonGenerator`) only instantiates results. **This is the template for every feature.**
- **UI Toolkit** — `UIDocument` + UXML for menus; keyboard/gamepad via focusable elements.
- **Lock-on + third-person camera** — separate concerns; lock-on swaps camera/strafe mode, does not own combat math.
- **`PlayerComponentManager` idea** — enable/disable control groups — but implement as a thin **control gate**, not a second god object.
- **ScriptableObjects for config** — weapons, room categories, loot tables, game tuning.

### Reject (why it became unmaintainable)

- **`PlayerManager` / `CharacterBaseManager`** — dozens of public `[SerializeField]` cross-refs; `ResetStates()` calls 20+ subsystems.
- **`SaveManager`** — knows every database, player, quest, bonfire, fade, notification; 700+ lines.
- **`Game` ScriptableObject** — player cosmetics, world flags, roguelike toggle, equipment defaults in one asset.
- **TigerForge `EventManager`** — stringly-typed broadcast; impossible to trace who listens.
- **Roguelike bolted onto soulslike** — `isRoguelike` flag on `Game` instead of a first-class run lifecycle.
- **Monolithic Cacildes `Scripts/` folder** — hundreds of files, no asmdefs. **This project uses `AF.*` namespaces with one asmdef per feature** — not a return to the old monolith.

---

## 5. Project layout

All game code lives under `Assets/_Project/`.

```
Assets/_Project/
├── Core/                 # Session, run lifecycle, scene flow, bootstrap, shared interfaces
├── Player/               # Player entity, motor, camera, lock-on, input routing (not combat math)
├── Combat/               # Hitboxes, damage pipeline, poise, abilities execution
├── Stats/                # Stat definitions, modifiers, resource pools (HP, stamina)
├── AI/                   # Enemy behaviour, perception, state machines
├── Dungeon/              # Procedural layout solver + room spawning adapter
├── Loot/                 # Drops, chests, run-scoped inventory
├── Meta/                 # Persistent unlocks between runs (jam-minimal)
├── UI/                   # UXML, USS, UIDocument presenters
└── Editor/               # Inspectors, debug tools (Editor asmdef only)
```

Each feature folder has an asmdef at the **feature root** (covers `Runtime/`, `Input/`, etc.):

```
Feature/
├── AF.Feature.asmdef     # e.g. AF.Core, AF.Player, AF.Dungeon
├── Runtime/
├── Editor/               # (optional) Editor-only code + AF.Feature.Editor.asmdef
└── Data/                 # (optional) ScriptableObject assets
```

### Namespaces & asmdefs (strict)

| Asmdef | Namespace | References |
|--------|-----------|------------|
| `AF.Core` | `AF.Core` | Unity only |
| `AF.Stats` | `AF.Stats` | `AF.Core` |
| `AF.Combat` | `AF.Combat` | `AF.Stats`, `AF.Core` |
| `AF.AI` | `AF.AI` | `AF.Combat`, `AF.Stats`, `AF.Core` |
| `AF.Player` | `AF.Player` | `AF.Core`, `Unity.InputSystem` |
| `AF.Dungeon` | `AF.Dungeon` | `AF.Core` |
| `AF.Loot` | `AF.Loot` | `AF.Stats`, `AF.Core` |
| `AF.Meta` | `AF.Meta` | `AF.Core` |
| `AF.UI` | `AF.UI` | `AF.Core` |
| `AF.Tests.EditMode` | `AF.Tests` | test targets (Editor only) |

Dependency direction:

```
Core          →  (Unity, Input System only)
Stats         →  Core
Combat        →  Stats, Core
AI            →  Combat, Stats, Core
Player        →  Core, Input System
Dungeon       →  Core
Loot          →  Stats, Core
Meta          →  Core
UI            →  Core
Editor        →  everything (editor only)
```

**Never:** `Core` → `Combat` / `AI` / `UI`.
**Never:** circular asmdefs.

If `Core` needs to react to combat, define an **interface or struct event** in `Core`, implemented or raised from `Combat`.

---

## 6. Layer model

Every feature follows three layers:

```
┌─────────────────────────────────────────┐
│  Adapter (MonoBehaviour)                │  Unity life cycle, Inspector, physics, animation
│  — reads input, writes transforms       │
├─────────────────────────────────────────┤
│  Controller / Coordinator               │  Glues adapters; still no Unity APIs if avoidable
├─────────────────────────────────────────┤
│  Logic (plain C#)                       │  State machines, solvers, damage math — unit-testable
├─────────────────────────────────────────┤
│  Data (SO / structs)                    │  Tunable, designer-facing
└─────────────────────────────────────────┘
```

### MonoBehaviour rules

MonoBehaviours are **adapters only**:

- Read input / collision / animation events
- Pass data into plain C# logic
- Apply results back to `Transform`, `Animator`, `CharacterController`
- **No** damage formulas, AI decisions, or run state transitions inside `Update()` unless trivial glue (1–3 lines)

Max **~150 lines** per MonoBehaviour. Split or extract logic if larger.

### Plain C# rules

- No `MonoBehaviour`, `GameObject`, `Transform` in logic classes
- Constructor-inject dependencies; use `struct` for snapshots (`RunState`, `DamageResult`, `StatSheet`)
- State machines are `enum` + `switch` or small classes — **no** animator-style hierarchy for game state

---

## 7. Core package (build first)

`Core` owns the **run lifecycle** — the roguelike spine.

### Run state machine (canonical)

```
Boot → MainMenu → RunStarting → FloorActive ↔ Encounter → (FloorCleared | PlayerDead) → RunEnded → MetaApply → MainMenu
```

Implement as plain C# `RunStateMachine`. One MonoBehaviour adapter (`RunCoordinator`) ticks it and loads scenes.

### Core types (initial)

| Type                                      | Responsibility                                       |
| ----------------------------------------- | ---------------------------------------------------- |
| `RunStateMachine`                         | States, transitions, run seed, floor index           |
| `RunConfig` / `RunSession` (SO or struct) | Per-run data: seed, difficulty, elapsed time         |
| `MetaProfile` (plain C# + save adapter)   | Persistent unlocks (jam: 1 currency, 3 upgrades max) |
| `ISceneFlow`                              | Load/unload dungeon, menu, bootstrap                 |
| `IPlayerSpawn`                            | Interface only — implementation in `Player`          |
| `GameBootstrap`                           | Entry point; wires refs from Inspector               |

### Core does NOT own

- Damage, poise, hitboxes → `Combat`
- Stat formulas → `Stats`
- Enemy decisions → `AI`
- Room geometry / spawning → `Dungeon`
- HUD layout → `UI`

---

## 8. Player package

Player is an **entity composition**, not a manager megaclass.

```
PlayerEntity (root GameObject)
├── PlayerInputAdapter        # Input System → intent struct
├── PlayerMotor               # CharacterController movement
├── PlayerCameraRig           # 3rd person follow
├── PlayerLockOn              # target selection, camera mode switch
├── PlayerControlGate         # enable/disable control groups (menu, cutscene, death)
└── PlayerView                # Animator parameter driver
```

**`PlayerIntent` struct** (plain data):

```csharp
public struct PlayerIntent
{
    public Vector2 Move;
    public Vector2 Look;
    public bool Dodge;
    public bool LightAttack;
    public bool LockOn;
    public bool LockOnSwitch;
    // jam scope: keep this list short
}
```

Combat reads `PlayerIntent` from an interface (`IPlayerIntentSource`) defined in `Core` or `Combat`, implemented in `Player`.

**Do not** reference `PlayerCombatController`, `PlayerDodgeController`, etc. from a single `PlayerManager` — each is its own component; coordination goes through intent + events.

---

## 9. Combat & Stats (stub until Core + Player move)

- **`Stats`**: `StatSheet`, `StatModifier`, resource pools. Pure math.
- **`Combat`**: `DamageRequest` → `DamageResolver` → `DamageResult`. Adapters apply results to health components.
- Weapons/abilities are **data** (ScriptableObject) + **executor** (plain C#), not 800-line controllers.

Jam scope combat verbs: **move, dodge, light attack, block** — add heavy/spell only if time remains.

---

## 10. Dungeon (`AF.Dungeon`) — locked decisions

Port from Cacildes with **KISS** rules. Delivery in four slices: types → solver → prefab authoring → generator.

### Locked (do not change without user approval)

| Decision | Value |
|----------|-------|
| Namespace / asmdef | `AF.Dungeon` |
| Critical path `roomSize` (jam) | **5** (start + 3 mid + boss) |
| Side rooms | **Keep** |
| Connector rooms | **Keep** |
| Collision | **One `BoxCollider` footprint per room** — no floor-tile matrix |
| Seed source | `RunCoordinator.Instance.Session.Seed` |
| Visibility culling | **Out** (jam v1) |
| Enemy/loot spawn in generator | **Out** — `AI` / `Loot` modules later |

### Architecture

1. **`DungeonLayoutSolver`** (plain C#) — placement only; no `GameObject` in solver.
2. **`RoomTemplate` / `PlacedRoom` / `DoorSocket`** — logic types; `RoomTemplate` has one `Bounds Footprint`, not a tile list.
3. **`BoundsHelper`** — one overlap function (~30 lines).
4. **`RoomPrefabData`** (MonoBehaviour on prefab) — authored footprint + doors; `OnValidate` from `RoomBounds` collider.
5. **`RoomCategoryData`** (ScriptableObject) — prefab pool, side room chance, connector pool.
6. **`DungeonGenerator`** (thin adapter) — build configs, call solver, instantiate, dead-ends, player spawn. **~80–100 lines target.**

### Placement helpers (dedupe Cacildes)

- `TryAttachRoom(...)` — direct snap exit → entrance
- `TryAttachWithConnector(...)` — optional connector between two rooms

Copy `AlignRooms` and `GetCategoryForIndex` math from Cacildes **verbatim** on first port.

### Room prefab authoring

```
Room (root)
├── RoomPrefabData
├── BoxCollider "RoomBounds"   ← footprint, not trigger
├── DoorEntrance               ← empty marker, forward = into room
├── DoorExit
└── Floor (visual only)
```

**Room constraints for camera + lock-on:**

- Boxy rooms for jam (L-shapes need a tight hand-placed box)
- Doorways face connectable axes
- Boss = last category in `layoutSequence`

---

## 11. Input System

- One `InputActionAsset` per project: `PlayerInputActions.inputactions`
- Generate C# class; wrap in `PlayerInputAdapter`
- **Action maps:** `Gameplay`, `UI`, `Menu`
- Switch maps when `RunStateMachine` enters menu / gameplay / death screen
- No legacy `UnityEngine.Input` — ever
- Rebind UI is **out of jam scope** unless literally free

---

## 12. UI Toolkit

### Structure

```
UI/
├── UXML/           # Layout only
├── USS/            # Styles only
└── Presenters/     # MonoBehaviour or plain C# that binds data → VisualElement
```

### Accessibility (mandatory)

Every interactive screen must work with **keyboard + gamepad** without mouse:

- Use `Button`, `Toggle`, `TextField` — not `Clickable` div hacks
- Set `focusable="true"` and verify `NavigationMoveEvent` works
- Define explicit `tabIndex` order on menus (title → new run → quit)
- **`PanelSettings`**: assign default navigation event handler
- On show: `root.Q<Button>("StartButton")?.Focus()` in presenter
- Never require hover-only interactions

### Combat HUD

- UITK for bars (HP, stamina), menus, run summary
- Lock-on reticle: world-space or `VisualElement` overlay — pick one, document in `PlayerLockOn`
- Do not split HUD across 5 UIDocuments like Cacildes HUD v2 + alert + wheel — **one gameplay HUD document** for jam

### Presenter pattern

```csharp
// Presenter reads from plain C# view-model or struct — not PlayerManager
public sealed class RunSummaryPresenter : MonoBehaviour
{
    public void Show(RunSummary summary) { /* bind labels */ }
}
```

---

## 13. Messaging (replace EventManager)

**No global string events.**

Allowed:

1. **C# event** on a narrow owner: `health.OnDied += ...`
2. **Interface callback** injected at bootstrap: `IRunEvents.OnPlayerDied()`
3. **`ScriptableObject` channel** (jam-friendly): `GameEventChannel` with `Raise()` / `Register` — typed, one concern per asset

```csharp
// Core/Runtime/Events/GameEventChannel.cs
public sealed class GameEventChannel : ScriptableObject
{
    public event Action Raised;
    public void Raise() => Raised?.Invoke();
}
```

Forbidden:

- `EventManager.StartListening("ON_CHARACTER_KILLED", ...)`
- Static singletons for cross-feature chatter

---

## 14. Data & persistence

### ScriptableObjects

- **Config** (designer tuning): `WeaponData`, `RoomCategory`, `EnemyData`
- **Channels**: `GameEventChannel`
- **Not** mutable runtime state — runtime state lives in plain C# objects

### Save (jam scope)

- `MetaProfile` only between runs (unlocks + best run stats)
- **No** mid-run save — roguelike contract
- One `MetaSaveAdapter` MonoBehaviour — not a second `SaveManager` god file
- Use `Application.persistentDataPath` + JSON (`JsonUtility` or simple custom) — no third-party save asset for jam unless already in project

---

## 15. Naming & style

| Item                   | Convention                                                     |
| ---------------------- | -------------------------------------------------------------- |
| Namespace              | `AF.Core`, `AF.Combat`, `AF.Dungeon`, …                        |
| Asmdef file            | `AF.Core.asmdef`, `AF.Dungeon.asmdef`, … at feature root       |
| MonoBehaviour adapters | `*Adapter`, `*Presenter`, `*View`, `*Gate`, `*Rig`             |
| Plain C# logic         | `*Resolver`, `*Solver`, `*Machine`, `*Sheet`                   |
| ScriptableObjects      | `*Data`, `*Config`, `*Definition`                              |
| Interfaces             | `I*` prefix, live in lowest assembly that needs them           |
| Fields                 | `_camelCase` private, `PascalCase` public properties           |
| SerializeField         | `[SerializeField] Type _name` — no public fields for Inspector |

- **Files:** one primary type per file; file name = type name
- **Usings:** remove unused; no `using Input = UnityEngine.Input` hacks
- **Comments:** only non-obvious invariants ("jam: hardcoded to 3 floors")
- **Async:** coroutines for jam; no `async/await` unless file already uses it

---

## 16. Testing (mandatory in every delivery)

**Every implementation task includes unit tests** when plain C# logic is added or changed. Pure UI/scene wiring with no testable logic may skip — state why in the summary.

### Rules

- **Edit Mode tests** (NUnit) for plain C#: solvers, bounds, state machines, damage math, stat sheets
- Tests live in `Assets/_Project/Tests/EditMode/` with `AF.Tests.EditMode.asmdef`
- Test **logic**, not instantiated prefabs or Play Mode scenes
- Reuse shared builders (e.g. `TestRooms.Box`) in `AF.Tests` namespace
- Name tests clearly: `MethodName_Scenario_ExpectedResult`
- No Play Mode tests for jam unless verifying a critical integration

### What to test per layer

| Layer | Test? |
|-------|-------|
| Plain C# logic | **Yes** — required |
| Static helpers (`BoundsHelper`) | **Yes** |
| MonoBehaviour adapter (thin glue) | Optional — prefer testing the logic it calls |
| UXML / USS / scene wiring | Manual checklist only |

### Agent must include with logic changes

- Edit Mode test files in `Assets/_Project/Tests/EditMode/`
- `AF.Tests.EditMode.asmdef` updated if new assembly reference needed
- Tests run or user told to run **Test Runner → Edit Mode**

---

## 17. Third-party & Unity packages

**Approved for jam:**

- Unity Input System (required)
- UI Toolkit (required)
- Cinemachine or Unity 6 camera rig for 3rd person
- AI Navigation (enemies)
- URP (already in project)

**Ask before adding:**

- Any new package from Package Manager
- DOTween, Odin, TigerForge, QuickSave, etc.

**Default:** solve with standard library + small custom code.

---

## 18. Agent workflow (Cursor / AI)

When implementing a task:

1. **Read this file** — package ownership, asmdef direction, jam scope
2. **Deliver `docs/code/<feature>.md`** — full file contents for the user to type
3. **Include unit tests** — Edit Mode tests for all new plain C# logic (§16), in the same doc
4. **Prefer plain C# classes** over new MonoBehaviours where possible
5. **Port from Cacildes** only after stating what you are simplifying
6. **Do not** design managers with more than **5** serialized dependencies
7. **Do not** add features outside jam loop (quests, companions, day/night, reputation, crafting)
8. **UI screens** — UXML/USS + keyboard/gamepad focus; full contents in the delivery doc
9. **One slice per task** — focused delivery, not a monolith

### Definition of done (per task)

- [ ] `docs/code/<feature>.md` written with full copy-paste file contents
- [ ] **Unit tests included** in the doc for all new plain C# logic (or explicit skip reason)
- [ ] Unity setup steps and verify checklist included
- [ ] Asmdef dependency direction documented if references change
- [ ] No `Find*` in runtime code (in delivered snippets)
- [ ] Logic classes have no `UnityEngine` dependency (or exception is explained)
- [ ] User confirms **Test Runner → Edit Mode → pass** after typing

---

## 19. Jam scope lock (do not expand without explicit user approval)

**In scope:**

- Title menu → start run
- Procedural floor (1 biome, 1 boss at end)
- 3rd person move + dodge + light attack + block
- Lock-on (single target, switch left/right)
- 2–3 enemy types
- Run loot (weapon + consumable)
- Death → meta currency → 2–3 permanent upgrades
- Run summary screen

**Out of scope (v1):**

- Character customization
- Quests, NPCs, dialogue
- Bonfires / mid-run checkpoints
- Rebindable controls
- Full inventory UI (use simple pickup slots)
- Swimming, climbing, stealth, bows, spells, executions, companions
- Save anywhere
- Localization

---

## 20. Quick reference — file templates

### Plain C# state machine

```csharp
namespace AF.Core
{
    public enum RunState { Boot, MainMenu, RunStarting, FloorActive, Encounter, FloorCleared, PlayerDead, RunEnded }

    public sealed class RunStateMachine
    {
        public RunState State { get; private set; }
        public void GoTo(RunState next) { /* set + event */ }
    }
}
```

### MonoBehaviour adapter (thin)

```csharp
namespace AF.Core
{
    public sealed class RunCoordinator : MonoBehaviour
    {
        RunStateMachine _machine = new();
        // scene loads on state entered — no god logic
    }
}
```

---

## 21. Glossary

| Term             | Meaning                                                         |
| ---------------- | --------------------------------------------------------------- |
| **Run**          | One attempt from `RunStarting` to `RunEnded` (death or victory) |
| **Floor**        | One procedural dungeon layer within a run                       |
| **Meta**         | Persistent progress between runs                                |
| **Adapter**      | MonoBehaviour that connects Unity to plain C#                   |
| **Intent**       | Frame input snapshot consumed by gameplay systems               |
| **Control gate** | Enables/disables player adapters (menu, stun, death)            |

---

_Last updated: user writes code from `docs/code/` (§2). Unit tests mandatory (§16). Canonical: `.cursor/rules/coding-and-architecture.md`. Mirror: `agent.md`._
