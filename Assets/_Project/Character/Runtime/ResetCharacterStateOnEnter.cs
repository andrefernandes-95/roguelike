using AF.Core;
using UnityEngine;

namespace AF.Character
{
  public sealed class ResetCharacterStateOnEnter : StateMachineBehaviour
  {
    IActionAnimator actionAnimator;
    IActionPresentationComplete presentationComplete;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
      actionAnimator ??= animator.GetComponentInParent<IActionAnimator>();
      presentationComplete ??= animator.GetComponentInParent<IActionPresentationComplete>();

      actionAnimator?.OnActionComplete();
      presentationComplete?.OnActionPresentationComplete();
    }
  }
}
