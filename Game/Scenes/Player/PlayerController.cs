namespace Squiggles.CheeseFruitGroves.Player;

using Godot;
using Squiggles.Core.FSM;
using System;

public partial class PlayerController : CharacterBody3D {

  [Export] private FiniteStateMachine _fsm;
  [Export] private State _stateMoving;
  [Export] private State _stateCutscene;

  public override void _Ready() {
    _stateCutscene.OnStateFinished += () => _fsm.ChangeState(_stateMoving);
    _stateMoving.OnStateFinished += () => _fsm.ChangeState(_stateCutscene);
  }
}
