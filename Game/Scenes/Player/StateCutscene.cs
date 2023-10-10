namespace Squiggles.CheeseFruitGroves.Player;

using Squiggles.Core.Events;
using Squiggles.Core.FSM;

public partial class StateCutscene : State {

  // effectively do nothing until signal is returned

  public override void EnterState() => EventBus.Gameplay.RequestPlayerAbleToMove += HandleExit;

  private void HandleExit(bool canMove) {
    if (canMove) {
      EmitSignal(nameof(OnStateFinished));
    }
  }

  public override void ExitState() => EventBus.Gameplay.RequestPlayerAbleToMove -= HandleExit;


}
