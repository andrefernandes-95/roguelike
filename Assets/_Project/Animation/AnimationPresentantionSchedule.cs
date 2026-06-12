using System;
using UnityEngine;

namespace AF.Animation
{
    public enum PresentationCueType
    {
        HitboxOpen,
        HitboxClose,
        DodgeIframesBegin,
        DodgeIframesEnd,
        ActionComplete,
        FootstepLeft,
        FootstepRight
    }

    [Serializable]
    public struct PresentationCue
    {
        [Tooltip("Frame in the clip when this fires")]
        public int frame;

        public PresentationCueType type;
    }

    [CreateAssetMenu(fileName = "PresentationSchedule", menuName = "AF/Character/Presentation Schedule")]
    public sealed class AnimationPresentationSchedule : ScriptableObject
    {
        public PresentationCue[] cues;
    }
}
