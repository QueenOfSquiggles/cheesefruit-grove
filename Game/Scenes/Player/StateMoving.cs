namespace Squiggles.CheeseFruitGroves.Player;
using Godot;
using Squiggles.Core.CharStats;
using Squiggles.Core.Data;
using Squiggles.Core.Events;
using Squiggles.Core.Extension;
using Squiggles.Core.FSM;
using Squiggles.Core.Scenes.Utility.Camera;

public partial class StateMoving : State {

  [Export] private CharacterBody3D _actor;

  [Export] private CharStatManager _stats;

  [Export] private VirtualCamera _cam;
  [Export] private Node3D _camArm;
  [Export] private float _camAngleMax = 70f;
  [Export] private float _camAngleMin = -70f;
  [Export] private float _gravityPower = 25.0f;

  private Vector3 _camAngleMaxRad;
  private Vector3 _camAngleMinRad;
  private Vector2 _lookVector;

  private float _gravity;
  private int _jumps;

  public override void EnterState() {
    SetPhysicsProcess(true);
    _camAngleMaxRad = new(Mathf.DegToRad(_camAngleMax), 0, 0);
    _camAngleMinRad = new(Mathf.DegToRad(_camAngleMin), 0, 0);
    EventBus.Gameplay.RequestPlayerAbleToMove += HandleDontMove;
    Input.MouseMode = Input.MouseModeEnum.Captured;
  }

  private void HandleDontMove(bool canMove) {
    if (!canMove) {
      EmitSignal(nameof(OnStateFinished));
    }
  }

  public override void ExitState() {
    SetPhysicsProcess(false);
    EventBus.Gameplay.RequestPlayerAbleToMove -= HandleDontMove;
  }

  public override void _PhysicsProcess(double delta) {
    var velocity = _actor.Velocity;
    DoMovement((float)delta, ref velocity);
    DoLook((float)delta);
    DoVertical((float)delta, ref velocity);
    _actor.Velocity = velocity;
    _actor.MoveAndSlide();
  }

  private void DoMovement(float delta, ref Vector3 velocity) {
    var keyDir = Input.GetVector("move_left", "move_right", "move_back", "move_forward");
    var joyDir = Input.GetVector("gamepad_move_left", "gamepad_move_right", "gamepad_move_back", "gamepad_move_forward");
    var inputDir = (keyDir.LengthSquared() > joyDir.LengthSquared()) ? keyDir : joyDir;
    if (inputDir.LengthSquared() > 1.0f) {
      inputDir = inputDir.Normalized();
    }
    var speed = _stats.GetStat("Speed") * (Input.IsActionPressed("sprint") ? _stats.GetStat("SprintSpeedFactor") : 1.0f);
    var accel = _stats.GetStat("Acceleration");

    var moveDir = new Vector3();
    moveDir += _cam.GlobalTransform.Forward() * inputDir.Y;
    moveDir += _cam.GlobalTransform.Right() * inputDir.X;

    moveDir.Y = 0f;
    var len = moveDir.Length() * speed;
    if (moveDir.LengthSquared() > 0.01) {
      moveDir = moveDir.Normalized() * len; // remaps to XZ motion without reductions due to angle
    }
    velocity = velocity.Lerp(moveDir, accel * delta);
  }

  private void DoVertical(float delta, ref Vector3 velocity) {
    var jumpMax = (int)_stats.GetStat("JumpCount");
    if (Input.IsActionJustPressed("jump") && _jumps < jumpMax) {
      _gravity = -_stats.GetStat("JumpStrength");
      _jumps++;
    }

    if (_actor.IsOnFloor() && _gravity > 0) {
      _gravity = 0f;
      _jumps = 0;
    }
    _gravity += _gravityPower * delta * (Input.IsActionPressed("jump") ? _stats.GetStat("JumpFloatFactor") : 1f);

    velocity.Y = -_gravity;
  }

  private void DoLook(float delta) {
    _lookVector += Input.GetVector("gamepad_look_left", "gamepad_look_right", "gamepad_look_down", "gamepad_look_up") * Controls.ControllerLookSensitivity;
    _lookVector *= delta;

    _actor.RotateY(_lookVector.X);
    _camArm.RotateX(_lookVector.Y);
    _camArm.Rotation = _camArm.Rotation.Clamp(_camAngleMinRad, _camAngleMaxRad);

    _lookVector = new();
  }

  public override void _UnhandledInput(InputEvent @event) {
    if (!IsActive) {
      return;
    }
    // handle input events
    if (@event is InputEventMouseMotion iemm) {
      _lookVector += iemm.Relative * Controls.MouseLookSensivity / GetViewport().GetWindow().Size.X * -1f;
      this.HandleInput();
    }
  }

}
