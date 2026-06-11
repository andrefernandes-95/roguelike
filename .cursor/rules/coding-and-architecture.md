---
alwaysApply: true
---

# AF — Agent & Architecture Rules

> Soulslike combat + roguelike runs — **Cacildes Adventure 2 rebuilt with clean architecture.**
> Reference: `Cacildes Adventure 2` — **gameplay and domain lessons**, not file layout or class design.
> Unity **6000.3**, URP, **New Input System**, **UI Toolkit**, 3rd-person camera + lock-on.

---

## 1. Mission

Build the **full game Cacildes was aiming at**, with **simpler, scalable code** than the original.

- **Ship in vertical slices** (boot → run → fight → die/win → meta) — but each slice must use the **final architecture**, not throwaway jam shortcuts.
- **Defer content**, not structure. Missing a spell type is fine; duplicating combat pipelines because “we’ll fix it later” is not.
- Every system should still be explainable in one sentence. Simple code ≠ minimal types; it means **one clear path** per concern.

### What “better than Cacildes” means

| Copy from Cacildes | Do **not** copy from Cacildes |
|--------------------|-------------------------------|
| Domain (runs, dungeons, soulslike verbs, SO data) | `CharacterBaseManager`, `PlayerManager`, god `SaveManager` |
| Algorithms that worked (`DungeonLayoutSolver`, door snap) | Split combat (lights vs abilities), dual stat modifier stacks |
| Feature set as **north star** | Folder layout, component wiring, event bus |

---

## 1b. Agent behavior — push back on architecture

Agents **must challenge** proposals (user’s or their own) when they repeat Cacildes failure modes or add complexity without scaling benefit.

**Push back when you see:**

- A new `*Manager` with 8+ serialized dependencies or “and also” responsibilities
- Player-only logic on a type that AI will need (`CombatController` reading input)
- A second pipeline for the same verb (melee controller **and** ability manager)
- Stats/attributes/equipment modeled as **two modifier systems**
- “Jam shortcut” / “we’ll refactor later” for **core** systems (combat, stats, saves, input)
- Porting Cacildes class names or structure “because it worked there”

**Instead, propose:**

- Plain C# logic + thin Unity adapter (dungeon solver pattern)
- **One executor, many data types** (abstract `CombatAction` subclasses; one `CombatController`)
- **One modifier path** (`StatSheet` → derived max → `ResourcePool`)
- **Intent in, commands out** — player adapter vs AI adapter, shared executor
- Explicit tradeoffs in writing before delivering `docs/code/*.md`

**Tone:** Direct and constructive. “Cacildes did X; here’s a simpler model that scales to Y.”

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
| Copy **algorithms** that worked in Cacildes (`DungeonLayoutSolver`) | Copy **structure** that rotted (40-field managers, 700-line saves) |
| Design for the **full** combat/stats/save model up front | “Jam version” that forks player vs AI or light vs heavy pipelines |

**The dumb code test:** If you cannot explain what a class does in one sentence without "and also", split it.

**The slice test:** Only **implement** what the current milestone needs — but **design** types and boundaries for the full game. No architectural dead ends.

**The Cacildes test:** Before porting a pattern, name what was wrong in the original and what replaces it here.

---

## 4. Lessons from Cacildes (read before writing code)

### Keep — domain & algorithms

- **Run lifecycle** as first-class (`RunStateMachine`), not `isRoguelike` on a `Game` SO.
- **`DungeonLayoutSolver` pattern** — pure C# logic; `DungeonGenerator` only adapts to Unity. **Template for every feature.**
- **ScriptableObjects for data** — weapons, room categories, `CombatAction` subclasses, loot tables.
- **UI Toolkit** — UXML/USS + presenters; keyboard/gamepad focus.
- **Lock-on + third-person camera** — separate from combat math.
- **Abstract `Ability` with subclasses** — right *idea*; wrong *wiring* (see Reject).

### Reject — and what we do instead

| Cacildes problem | Better AF shape |
|------------------|-----------------|
| `CharacterBaseManager` / `PlayerManager` — 20+ refs, `ResetStates()` cascade | **Entity composition** — small components; `PlayerControlGate` enables/disables groups |
| `PlayerCombatController` + `CharacterAbilityManager` — two pipelines | **`CombatController.TryStart(CombatAction)`** — one executor; `PlayerCombatInput` vs AI driver |
| `StatsController` + `AttributeController` + per-resource managers | **`StatSheet` → `DerivedStats` → `ResourcePool`** — one modifier path |
| `StaminaStatManager` / `ManaManager` separate from attributes | Same **`ResourcePool`** type; regen policy per resource, max from stats |
| `SaveManager` knows everything | **Per-domain save ports** + orchestrator; no 700-line god file |
| TigerForge `EventManager` | **Typed events** on owning types, or Core interfaces — traceable |
| Light attacks bypass `Ability` SOs | **Every combat verb is a `CombatAction` subclass** — including basic melee |
| Monolithic `Scripts/` | **`AF.*` asmdefs** per feature, tests colocated in `Feature/Tests/` |

**Default stance:** Cacildes is a **requirements doc** and **anti-pattern catalog**, not a codebase to mirror.

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
├── Meta/                 # Persistent unlocks between runs
├── UI/                   # UXML, USS, UIDocument presenters
└── Editor/               # Inspectors, debug tools (Editor asmdef only)
```

Each feature folder has an asmdef at the **feature root** (covers `Runtime/`, `Input/`, etc.):

```
Feature/
├── AF.Feature.asmdef     # e.g. AF.Core, AF.Player, AF.Dungeon
├── Runtime/
├── Tests/                # Edit Mode tests — AF.Feature.Tests.asmdef
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
| `AF.<Feature>.Tests` | `AF.Tests.<Feature>` | that feature asmdef + Test Runner (Editor only) |

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
| `MetaProfile` (plain C# + save adapter)   | Persistent unlocks between runs |
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
}
```

`PlayerIntent` lives in **`AF.Core`**. `IPlayerIntentSource` implemented by `PlayerInputAdapter` in `AF.Player`.

**Do not** recreate `PlayerManager` / `PlayerCombatController`. Player = motor + camera + input + **thin adapters** that call into `Combat` / `Stats`.

---

## 9. Combat & Stats — target architecture (not a stub)

Design for **full Cacildes combat breadth** with **fewer moving parts**.

### Stats (`AF.Stats`) — plain C#

```
StatId (enum) → StatSheet (base + modifiers by sourceId)
             → DerivedStats (formulas: vitality→HP, endurance→stamina, …)
             → ResourcePool (current/max, damage, spend, regen hooks)
             → DamageResolver (int damage in/out — extend for types/resists later)
```

- **Health, stamina, mana** are all `ResourcePool` instances fed by the same stat sheet.
- Equipment: `StatSheet.AddModifier(sourceId, …)` then `ResourcePool.RefreshMax()`. **One system.**

### Combat (`AF.Combat`)

```
CombatAction (abstract SO)     — CanExecute, Begin, Tick, End
    ├── MeleeHitboxAction
    ├── ProjectileAction       (later)
    ├── DodgeAction            (later)
    └── …

CombatController               — TryStart(action); entity-agnostic; NO input
CombatExecution                — context: controller, actor, hitboxes, target (later)
CombatActor                    — StatSheet + resource pool refs for this entity

PlayerCombatInput (AF.Player)  — intent → TryStart
AI states (AF.AI)              — decision → TryStart
```

- **Never** a parallel melee-only controller.
- Hitbox/Hurtbox + `HealthComponent` wrap `ResourcePool` + events.

### Delivery order

Still slice by slice for **implementation**, but each doc must match this shape. See `docs/code/combat-minimum-v2.md` for first slice.

---

## 10. Dungeon (`AF.Dungeon`) — locked decisions

Port from Cacildes with **KISS** rules. Delivery in four slices: types → solver → prefab authoring → generator.

### Locked (do not change without user approval)

| Decision | Value |
|----------|-------|
| Namespace / asmdef | `AF.Dungeon` |
| Default critical path `roomSize` | **5** (start + 3 mid + boss) — tunable, not a code fork |
| Side rooms | **Keep** |
| Connector rooms | **Keep** |
| Collision | **Floor tile bounds** — `RoomFloorTile` + `MeshFilter` per slab, baked at prefab load |
| Seed source | `RunCoordinator.Instance.Session.Seed` |
| Visibility culling | **Deferred** — solver types stay pure; adapter adds culling later |
| Enemy/loot spawn in generator | **`AI` / `Loot` modules** — not in generator |

### Architecture

1. **`DungeonLayoutSolver`** (plain C#) — placement only; no `GameObject` in solver.
2. **`RoomTemplate` / `PlacedRoom` / `DoorSocket`** — logic types; `RoomTemplate` has `List<Bounds> FloorTiles`.
3. **`BoundsHelper`** — per-tile overlap (Cacildes shrink + `Intersects`).
4. **`RoomPrefabData`** (MonoBehaviour on prefab) — bakes floor tiles + doors from prefab hierarchy.
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
├── DoorEntrance               ← empty marker, forward = into room
├── DoorExit
└── Floor*                     ← RoomFloorTile + MeshFilter per slab (required)
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
- **One test asmdef per feature** — colocated under that feature’s `Tests/` folder (e.g. `Dungeon/Tests/AF.Dungeon.Tests.asmdef`). Do **not** grow a monolithic `AF.Tests.EditMode` that references every package.
- Test asmdef references **only** the feature under test (+ Unity Test Runner). Cross-feature integration tests are rare; put them in the higher-level feature’s test assembly or skip for jam.
- Test **logic**, not instantiated prefabs or Play Mode scenes (prefab bake tests that need `AssetDatabase` are OK in that feature’s `Tests/` with Editor platform)
- Shared builders for a feature live in that feature’s `Tests/` (e.g. `Dungeon/Tests/TestRooms.cs`)
- Namespace: `AF.Tests.Dungeon`, `AF.Tests.Stats`, … — not a flat `AF.Tests` grab bag
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

- Edit Mode test files in `Assets/_Project/<Feature>/Tests/`
- `AF.<Feature>.Tests.asmdef` for that feature (create if missing)
- Tests run or user told to run **Test Runner → Edit Mode**

### Test asmdef template

```json
{
    "name": "AF.Dungeon.Tests",
    "rootNamespace": "AF.Tests.Dungeon",
    "references": [
        "AF.Dungeon",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

Replace `AF.Dungeon` / namespace with the feature under test (`AF.Stats`, etc.).

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

1. **Read this file** — package ownership, asmdef direction, **target architecture** (§9)
2. **Challenge the request** if it copies a Cacildes anti-pattern (§1b, §4) — propose the AF shape first
3. **Deliver `docs/code/<feature>.md`** — full file contents for the user to type
4. **Include unit tests** — Edit Mode tests for all new plain C# logic (§16), in the feature’s `Tests/` folder
5. **Prefer plain C# classes** over new MonoBehaviours where possible
6. **Port from Cacildes** only after: (a) what we keep, (b) what was wrong, (c) what replaces it
7. **Do not** design managers with more than **5** serialized dependencies
8. **UI screens** — UXML/USS + keyboard/gamepad focus; full contents in the delivery doc
9. **One slice per task** — focused delivery, not a monolith; slice **implements** less, **designs** for full game

### Definition of done (per task)

- [ ] `docs/code/<feature>.md` written with full copy-paste file contents
- [ ] **Unit tests included** in the doc for all new plain C# logic (or explicit skip reason)
- [ ] Unity setup steps and verify checklist included
- [ ] Asmdef dependency direction documented if references change
- [ ] No `Find*` in runtime code (in delivered snippets)
- [ ] Logic classes have no `UnityEngine` dependency (or exception is explained)
- [ ] User confirms **Test Runner → Edit Mode → pass** after typing

---

## 19. Content milestones (implementation order — not architecture limits)

**Architecture** targets the full soulslike + roguelike (Cacildes feature set). **Content** ships in milestones:

| Milestone | Playable loop | Architecture must already support |
|-----------|---------------|-----------------------------------|
| M1 | Menu → run → graybox dungeon → move | Core, Dungeon, Player |
| M2 | + light attack, enemy HP, player death | Stats, Combat (`CombatAction` hierarchy) |
| M3 | + block, dodge, lock-on | More `CombatAction` types, same executor |
| M4 | + AI chase/attack, loot, meta currency | AI driver → `TryStart`, Loot, Meta saves |
| M5+ | Spells, equipment modifiers, more biomes | Extend `StatSheet`, `CombatAction` — no rewrites |

**Explicitly deferred content** (not deferred architecture): quests, bonfires, swimming/climbing, full customization UI, localization — add when milestone allows; **do not** hack them in via god managers.

Expand milestones only with user approval; **never** expand by bolting Cacildes-style monoliths.

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

_Last updated: Cacildes rebuild — clean architecture, vertical slices, agent pushback (§1b). User writes code from `docs/code/` (§2). Tests in `Feature/Tests/` (§16). Mirror: `agent.md`._
