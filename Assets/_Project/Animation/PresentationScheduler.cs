using UnityEngine;

namespace AF.Animation
{
    [System.Serializable]
    public struct LocomotionPresentationBinding
    {
        public string animatorStateName;
        public AnimationPresentationMap map;
    }

    /// <summary>
    /// Polls the playing clip each frame and fires presentation events at authored frame numbers.
    /// Listeners implement <see cref="IAnimationPresentationListener"/> on this object or its children.
    /// </summary>
    public sealed class PresentationScheduler : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] int layer;
        [SerializeField] LocomotionPresentationBinding[] locomotionMaps;

        IAnimationPresentationListener[] listeners;
        AnimationPresentationMap activeMap;
        int firedMask;
        int lastFrame = -1;
        string lastClipName;

        void Awake()
        {
            listeners = GetComponentsInChildren<IAnimationPresentationListener>();
        }

        public void StartMap(AnimationPresentationMap map)
        {
            activeMap = map;
            firedMask = 0;
            lastFrame = -1;
            lastClipName = null;
        }

        public void StopMap()
        {
            activeMap = null;
            firedMask = 0;
            lastFrame = -1;
            lastClipName = null;
        }

        void Update()
        {
            if (animator == null)
            {
                return;
            }

            AnimationPresentationMap map = activeMap;
            if (map == null)
            {
                map = ResolveLocomotionMap();
                if (map == null)
                {
                    return;
                }
            }

            if (!TryGetCurrentClipFrame(out AnimationClip clip, out int currentFrame, out bool looped))
            {
                return;
            }

            if (clip.name != lastClipName)
            {
                firedMask = 0;
                lastClipName = clip.name;
            }

            if (looped)
            {
                firedMask = 0;
            }

            lastFrame = currentFrame;

            if (!map.TryGetCues(clip, out PresentationCue[] cues))
            {
                return;
            }

            FireDueCues(cues, currentFrame);
        }

        AnimationPresentationMap ResolveLocomotionMap()
        {
            if (locomotionMaps == null)
            {
                return null;
            }

            int hash = animator.GetCurrentAnimatorStateInfo(layer).shortNameHash;
            for (int i = 0; i < locomotionMaps.Length; i++)
            {
                if (Animator.StringToHash(locomotionMaps[i].animatorStateName) == hash)
                {
                    return locomotionMaps[i].map;
                }
            }

            return null;
        }

        bool TryGetCurrentClipFrame(out AnimationClip clip, out int frame, out bool looped)
        {
            clip = null;
            frame = 0;
            looped = false;

            AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(layer);
            if (clips.Length == 0)
            {
                return false;
            }

            clip = clips[0].clip;
            if (clip == null)
            {
                return false;
            }

            float normalizedTime = animator.GetCurrentAnimatorStateInfo(layer).normalizedTime;
            float timeInClip = (normalizedTime - Mathf.Floor(normalizedTime)) * clip.length;
            frame = Mathf.FloorToInt(timeInClip * clip.frameRate);
            looped = activeMap == null && lastFrame >= 0 && frame < lastFrame;
            return true;
        }

        void FireDueCues(PresentationCue[] cues, int currentFrame)
        {
            for (int i = 0; i < cues.Length; i++)
            {
                if (i >= 31)
                {
                    break;
                }

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
                Dispatch(cues[i].eventName);

                if (activeMap != null
                    && cues[i].eventName == PresentationEventNames.ActionComplete)
                {
                    StopMap();
                }
            }
        }

        void Dispatch(string eventName)
        {
            if (string.IsNullOrEmpty(eventName) || listeners == null)
            {
                return;
            }

            for (int i = 0; i < listeners.Length; i++)
            {
                listeners[i].OnAnimationPresentationEvent(eventName);
            }
        }
    }
}
