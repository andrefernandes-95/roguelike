using System;
using System.Collections.Generic;
using UnityEngine;

namespace AF.Animation
{
    [Serializable]
    public struct PresentationCue
    {
        [Tooltip("Frame in this clip when the cue fires.")]
        public int frame;

        [Tooltip("Event id — use PresentationEventNames or any custom string.")]
        public string eventName;
    }

    [Serializable]
    public struct ClipPresentationEntry
    {
        [Tooltip("One variation clip (e.g. unarmed vs sword light attack 01).")]
        public AnimationClip clip;

        public PresentationCue[] cues;
    }

    /// <summary>
    /// Maps many AnimationClip variations to frame cues for one animator state (e.g. Action_LightAttack_01).
    /// At runtime the scheduler reads the playing clip and uses that entry's cues.
    /// </summary>
    [CreateAssetMenu(fileName = "PresentationMap", menuName = "AF/Animation/Presentation Map")]
    public sealed class AnimationPresentationMap : ScriptableObject
    {
        public ClipPresentationEntry[] entries;

        Dictionary<string, PresentationCue[]> lookupByClipName;

        void OnEnable()
        {
            RebuildLookup();
        }

        void OnValidate()
        {
            RebuildLookup();
        }

        void RebuildLookup()
        {
            if (entries == null)
            {
                lookupByClipName = null;
                return;
            }

            lookupByClipName = new Dictionary<string, PresentationCue[]>(entries.Length);
            for (int i = 0; i < entries.Length; i++)
            {
                AnimationClip clip = entries[i].clip;
                if (clip == null || entries[i].cues == null || entries[i].cues.Length == 0)
                {
                    continue;
                }

                lookupByClipName[clip.name] = entries[i].cues;
            }
        }

        public bool TryGetCues(AnimationClip playingClip, out PresentationCue[] cues)
        {
            cues = null;
            if (playingClip == null || entries == null)
            {
                return false;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].clip == playingClip)
                {
                    cues = entries[i].cues;
                    return cues != null && cues.Length > 0;
                }
            }

            if (lookupByClipName != null && lookupByClipName.TryGetValue(playingClip.name, out cues))
            {
                return cues.Length > 0;
            }

            return false;
        }
    }
}
