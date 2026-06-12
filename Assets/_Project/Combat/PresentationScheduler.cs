using AF.Animation;
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
