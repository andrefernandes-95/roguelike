using AF.Core;
using UnityEngine;

namespace AF.Combat
{
  /// <summary>
  /// Shared combat executor for player and AI. Does not read input.
  /// Actions end via animation clip events or locomotion hub SMB — not timers.
  /// </summary>
  public sealed class CombatController : MonoBehaviour, IActionPresentationComplete
  {
    [SerializeField] CombatActor actor;
    [SerializeField] Hitbox hitbox;

    CombatExecution execution;
    CombatAction activeAction;

    void Awake()
    {
      IActionAnimator actionAnimator = GetComponent<IActionAnimator>();
      ILocomotionReadout locomotionReadout = GetComponent<ILocomotionReadout>();
      execution = new CombatExecution(this, actor, hitbox, actionAnimator, locomotionReadout);
    }

    void Update()
    {
      if (!IsBusy)
      {
        return;
      }

      activeAction.Tick(execution, Time.deltaTime);
    }

    public bool TryStart(CombatAction action)
    {
      if (action == null || IsBusy)
      {
        return false;
      }

      if (!action.CanExecute(execution))
      {
        return false;
      }

      activeAction = action;
      activeAction.Begin(execution);
      return IsBusy;
    }

    /// <summary>
    /// Called when Begin fails (e.g. animator could not play state).
    /// </summary>
    public void CancelActiveAction()
    {
      if (!IsBusy)
      {
        return;
      }

      EndActiveAction();
    }

    /// <summary>
    /// Called by CombatAnimationEvents clip events.
    /// </summary>
    public void NotifyActionAnimationComplete()
    {
      OnActionPresentationComplete();
    }

    public void OnActionPresentationComplete()
    {
      if (!IsBusy)
      {
        return;
      }

      EndActiveAction();
    }

    void EndActiveAction()
    {
      activeAction?.End(execution);
      activeAction = null;
    }

    public bool IsBusy => activeAction != null;
  }
}
