# Animation presentation map (per clip, no clip events)

One **animator state** (e.g. `Action_LightAttack_01`), **many clip variations** (unarmed, sword, … via override).  
Each **clip** has its own frame → event list. Runtime uses **whatever clip is playing**.

This system lives entirely in **`AF.Animation`**. Combat, footsteps, and future VFX subscribe as listeners — they do not own the scheduler.

## Data

`AnimationPresentationMap` (`AF.Animation`):

```
entries[]
  ├── clip: LightAttack01_Unarmed.fbx
  │     cues: frame 10 HitboxOpen, frame 17 HitboxClose, frame 30 ActionComplete
  ├── clip: LightAttack01_Sword.fbx
  │     cues: frame 8 HitboxOpen, frame 14 HitboxClose, frame 28 ActionComplete
  └── … one entry per clip variation
```

Each cue: `{ int frame, string eventName }`. Use `PresentationEventNames` for common ids or any custom string.

Lookup: `TryGetCues(playingClip)` — reference match, then clip **name**.

## Runtime

1. Something calls `IPresentationPlayback.StartMap(map)` (e.g. a combat action on begin).
2. Each frame `PresentationScheduler` reads `GetCurrentAnimatorClipInfo(layer).clip`.
3. Finds that clip's entry, dispatches `eventName` to all `IAnimationPresentationListener` on this object and children.
4. Clip changes → reset fired mask. `ActionComplete` on an active one-shot map → `StopMap()`.

**Locomotion** maps run automatically when the animator is in a bound state (no `StartMap`).

## Files (`AF.Animation`)

- `AnimationPresentationMap.cs` — clip → frame cues
- `PresentationEventNames.cs` — shared event id constants
- `PresentationScheduler.cs` — polls frames, dispatches events
- `IAnimationPresentationListener.cs` — subscribe per domain
- `IPresentationPlayback.cs` — start/stop one-shot maps

## Listeners (examples)

| Assembly | Component | Handles |
|----------|-----------|---------|
| `AF.Combat` | `CombatPresentationListener` | HitboxOpen, HitboxClose, dodge iframes |
| `AF.Character` | `CharacterPresentationListener` | ActionComplete, FootstepLeft/Right |

Add more listeners for VFX, audio, etc. without touching combat or the scheduler.

## Character setup

On the character root (or children):

1. `PresentationScheduler` — assign Animator, locomotion bindings for walk/run clips.
2. `CharacterPresentationListener` — footstep audio, action-complete hooks.
3. `CombatPresentationListener` — hitbox reference (if this character fights).

Combat actions reference `AnimationPresentationMap` assets and call `ctx.Presentation.StartMap(map)` — they do not embed presentation logic.

## Authoring

1. Create **AF → Animation → Presentation Map**.
2. Add one **entry per clip variation**; author **frame numbers** and **event names** on that clip.
3. Assign map on combat action assets and/or locomotion bindings on the scheduler.

Recreate any old `PresentationSchedule` assets as **Presentation Map** (string event names, not enum).
