namespace AF.Animation
{
    /// <summary>
    /// Receives frame-based presentation events from <see cref="PresentationScheduler"/>.
    /// Add one listener component per domain (combat, footsteps, VFX, …).
    /// </summary>
    public interface IAnimationPresentationListener
    {
        void OnAnimationPresentationEvent(string eventName);
    }
}
