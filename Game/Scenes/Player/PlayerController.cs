namespace Squiggles.CheeseFruitGroves.Player;

using System.Linq;
using Godot;
using Squiggles.CheeseFruitGroves.Data;
using Squiggles.Core.Data;
using Squiggles.Core.Error;
using Squiggles.Core.Events;
using Squiggles.Core.FSM;

public partial class PlayerController : CharacterBody3D {

  [Export] private FiniteStateMachine _fsm;
  [Export] private State _stateMoving;
  [Export] private State _stateCutscene;

  private const string SAVE_FILE = "player.json";

  public override void _Ready() {
	_stateCutscene.OnStateFinished += () => _fsm.ChangeState(_stateMoving);
	_stateMoving.OnStateFinished += () => _fsm.ChangeState(_stateCutscene);
	Deserialize();
	EventBus.Data.SerializeAll += Serialize;
	EventBus.Data.Reload += Deserialize;
  }

  public override void _ExitTree() {
	EventBus.Data.SerializeAll -= Serialize;
	EventBus.Data.Reload -= Deserialize;
  }

  private void Serialize() {
	var builder = new SaveDataBuilder(SAVE_FILE);
	builder.PutVector3(nameof(GlobalPosition), GlobalPosition);
	builder.PutVector3(nameof(GlobalRotation), GlobalRotation);
	builder.PutVariant("test var", new int[] { 4, 2, 0, 6, 2, 1 });
	foreach (var child in GetChildren()) {
	  if (child is IHasSaveData data) {
		data.Serialize(builder);
	  }
	}

	builder.SaveToFile();
  }

  private void Deserialize() {
	var builder = new SaveDataBuilder(SAVE_FILE).LoadFromFile();
	GlobalPosition = builder.GetVector3(nameof(GlobalPosition), out var gPos) ? gPos : GlobalPosition;
	GlobalRotation = builder.GetVector3(nameof(GlobalRotation), out var gRot) ? gRot : GlobalRotation;
	foreach (var child in GetChildren()) {
	  if (child is IHasSaveData data) {
		data.Deserialize(builder);
	  }
	}
  }
}
