# Animation presentation schedule (no clip events)

Fire gameplay cues at **frame numbers** from small ScriptableObjects — do not add `AnimationEvent` to commercial clips.

**Three types total:** schedule SO, scheduler MonoBehaviour, hooks MonoBehaviour. No receiver interfaces, no catalog SO, no clip cloning.

**Replaces:** `CombatAnimationEvents` (delete after migration).

---

## Files (all in `AF.Combat` — lives on character prefab next to `CombatController`)

```
Assets/_Project/Combat/
├── AnimationPresentationSchedule.cs
├── PresentationScheduler.cs
├── CharacterPresentationHooks.cs
├── MeleeHitboxAction.cs          ← presentation field + StartSchedule
├── DodgeCombatAction.cs
└── CombatExecution.cs            ← Scheduler ref
```

Delete `CombatAnimationEvents.cs`.

---

### `AnimationPresentationSchedule.cs`

```csharp
using System;
using UnityEngine;

namespace AF.Combat
{
    public enum PresentationCueType
    {
        HitboxOpen,
        HitboxClose,
        DodgeIframesBegin,
        DodgeIframesEnd,
        ActionComplete,
        FootstepLeft,
        FootstepRight,
    }

    [Serializable]
    public struct PresentationCue
    {
        [Tooltip("Frame in the clip when this fires (0 = start of clip).")]
        public int frame;

        public PresentationCueType type;
    }

    [CreateAssetMenu(fileName = "PresentationSchedule", menuName = "AF/Character/Presentation Schedule")]
    public sealed class AnimationPresentationSchedule : ScriptableObject
    {
        public PresentationCue[] cues;
    }
}
```

**Authoring:** scrub the clip in Unity, read the **frame number** from the timeline, type it in.  
Example light attack: frame **10** open, frame **17** close, frame **30** complete (on a 30 fps clip).

---

### `CharacterPresentationHooks.cs`

One component on the character root. Direct methods — no interfaces.

```csharp
using AF.Combat;
using AF.Core;
using UnityEngine;

namespace AF.Combat
{
    public sealed class CharacterPresentationHooks : MonoBehaviour
    {
        [Header("Combat")]
        [SerializeField] Hitbox attackHitbox;

        [Header("Footsteps")]
        [SerializeField] AudioSource footstepSource;
        [SerializeField] AudioClip leftFootstep;
        [SerializeField] AudioClip rightFootstep;

        IActionAnimator actionAnimator;
        IActionPresentationComplete presentationComplete;

        void Awake()
        {
            actionAnimator = GetComponent<IActionAnimator>();
            presentationComplete = GetComponent<IActionPresentationComplete>();
        }

        public void Fire(PresentationCueType type)
        {
            switch (type)
            {
                case PresentationCueType.HitboxOpen:
                    attackHitbox?.BeginSwing();
                    break;
                case PresentationCueType.HitboxClose:
                    attackHitbox?.EndSwing();
                    break;
                case PresentationCueType.DodgeIframesBegin:
                    break;
                case PresentationCueType.DodgeIframesEnd:
                    break;
                case PresentationCueType.ActionComplete:
                    actionAnimator?.OnActionComplete();
                    presentationComplete?.OnActionPresentationComplete();
                    break;
                case PresentationCueType.FootstepLeft:
                    PlayFootstep(leftFootstep);
                    break;
                case PresentationCueType.FootstepRight:
                    PlayFootstep(rightFootstep);
                    break;
            }
        }

        void PlayFootstep(AudioClip clip)
        {
            if (footstepSource != null && clip != null)
            {
                footstepSource.PlayOneShot(clip);
            }
        }
    }
}
```

---

### `PresentationScheduler.cs`

```csharp
using UnityEngine;

namespace AF.Combat
{
    [System.Serializable]
    public struct LocomotionScheduleBinding
    {
        public string animatorStateName;
        public AnimationPresentationSchedule schedule;
    }

    public sealed class PresentationScheduler : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] int layer;
        [SerializeField] CharacterPresentationHooks hooks;
        [SerializeField] LocomotionScheduleBinding[] locomotionSchedules;

        AnimationPresentationSchedule activeSchedule;
        int firedMask;
        int lastFrame = -1;

        public void StartSchedule(AnimationPresentationSchedule schedule)
        {
            activeSchedule = schedule;
            firedMask = 0;
            lastFrame = -1;
        }

        public void StopSchedule()
        {
            activeSchedule = null;
            firedMask = 0;
            lastFrame = -1;
        }

        void Update()
        {
            if (animator == null || hooks == null)
            {
                return;
            }

            AnimationPresentationSchedule schedule = activeSchedule;
            if (schedule == null)
            {
                schedule = ResolveLocomotionSchedule();
                if (schedule == null)
                {
                    return;
                }
            }

            if (!TryGetCurrentFrame(out int currentFrame, out bool looped))
            {
                return;
            }

            if (looped)
            {
                firedMask = 0;
            }

            lastFrame = currentFrame;
            FireDueCues(schedule, currentFrame);

        }

        AnimationPresentationSchedule ResolveLocomotionSchedule()
        {
            int hash = animator.GetCurrentAnimatorStateInfo(layer).shortNameHash;
            for (int i = 0; i < locomotionSchedules.Length; i++)
            {
                if (Animator.StringToHash(locomotionSchedules[i].animatorStateName) == hash)
                {
                    return locomotionSchedules[i].schedule;
                }
            }

            return null;
        }

        bool TryGetCurrentFrame(out int frame, out bool looped)
        {
            frame = 0;
            looped = false;

            AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(layer);
            if (clips.Length == 0)
            {
                return false;
            }

            AnimationClip clip = clips[0].clip;
            if (clip == null)
            {
                return false;
            }

            float normalizedTime = animator.GetCurrentAnimatorStateInfo(layer).normalizedTime;
            float timeInClip = (normalizedTime - Mathf.Floor(normalizedTime)) * clip.length;
            frame = Mathf.FloorToInt(timeInClip * clip.frameRate);
            looped = activeSchedule == null && lastFrame >= 0 && frame < lastFrame;
            return true;
        }

        void FireDueCues(AnimationPresentationSchedule schedule, int currentFrame)
        {
            PresentationCue[] cues = schedule.cues;
            for (int i = 0; i < cues.Length; i++)
            {
                int bit = 1 << i;
                if ((firedMask & bit) != 0)
                {
                    continue;
                }

                if (currentFrame < cues[i].frame)
                {
                    continue;
                }

                firedMask |= bit;
                hooks.Fire(cues[i].type);

                if (cues[i].type == PresentationCueType.ActionComplete && activeSchedule == schedule)
                {
                    StopSchedule();
                }
            }
        }
    }
}
```

**Frame source:** current playing clip from `GetCurrentAnimatorClipInfo` × state normalized time. Author frames against that clip (or matching override with same length/fps).

---

### `MeleeHitboxAction.cs` (add to `Begin`)

```csharp
public AnimationPresentationSchedule presentation;

// after TryPlayState succeeds:
ctx.Scheduler?.StartSchedule(presentation);
```

### `CombatExecution` + `CombatController.Awake`

```csharp
public PresentationScheduler Scheduler { get; }

// Awake:
Scheduler = GetComponent<PresentationScheduler>();
execution = new CombatExecution(..., Scheduler);
```

### `End` on action

```csharp
ctx.Scheduler?.StopSchedule();
ctx.Hitbox?.EndSwing();
```

---

## Assets

```
Assets/Data/Character/Presentation/
├── LightAttack_Unarmed.asset     frames: 10 Open, 17 Close, 30 Complete
└── Walk_Footsteps.asset          frames: 5 Left, 15 Right
```

Assign `presentation` on `LightAttack_Unarmed` combat SO.  
Assign walk schedule on `PresentationScheduler` locomotion array (`stateName` = your walk state).

---

## Hierarchy

```
CharacterRoot
├── CombatController
├── CharacterAnimationDriver
├── PresentationScheduler
├── CharacterPresentationHooks
└── Model → Animator
```

Delete `CombatAnimationEvents`. Remove clip events from FBX.

---

## Checklist

- [ ] Schedule SOs authored with **frame** numbers
- [ ] Scheduler + hooks on character
- [ ] Combat actions call `StartSchedule`
- [ ] Walk footsteps via locomotion array on scheduler
- [ ] `CombatAnimationEvents` deleted

---

## What we are not building

- `IPresentationEventReceiver` / multiple receiver components
- Separate catalog ScriptableObject
- Normalized-time authoring
- Runtime clip event injection
