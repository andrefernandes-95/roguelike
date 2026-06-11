namespace AF.Core
{
  /// <summary>
  /// Combat presentation finished (clip event or locomotion hub SMB).
  /// Implemented by CombatController; invoked without AF.Character → AF.Combat reference.
  /// </summary>
  public interface IActionPresentationComplete
  {
    void OnActionPresentationComplete();
  }
}
