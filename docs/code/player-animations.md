# Moved

This doc was **player-centric** and has been superseded by the character-agnostic plan:

**[character-animations.md](character-animations.md)**

Key change: locomotion + animation live in **`AF.Character`**; the player prefab adds thin **`AF.Player`** adapters only (`PlayerLocomotionInput`, `PlayerCombatInput`, camera, control gate). Dodge is a **`DodgeCombatAction`**, not `PlayerDodge`.
