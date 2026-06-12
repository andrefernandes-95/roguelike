namespace AF.Animation
{
    /// <summary>
    /// Starts and stops one-shot presentation maps (e.g. combat actions).
    /// Locomotion maps on <see cref="PresentationScheduler"/> run automatically.
    /// </summary>
    public interface IPresentationPlayback
    {
        void StartMap(AnimationPresentationMap map);
        void StopMap();
    }
}
