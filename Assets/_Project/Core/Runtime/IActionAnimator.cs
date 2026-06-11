namespace AF.Core
{
    public interface IActionAnimator
    {
        bool IsBusy { get; }
        bool IsRootMotionActive { get; }

        bool TryPlayState(int stateHash, bool useRootMotion);
        void OnActionComplete();
    }
}
