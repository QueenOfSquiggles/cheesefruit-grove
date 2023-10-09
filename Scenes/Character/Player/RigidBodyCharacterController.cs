using System;
using Godot;
using queen.error;

public partial class RigidBodyCharacterController : RigidBody3D
{
    [Export] private float TargetSpeed = 5.0f;

    private PID3D _PID = new(1.0f, 0.1f, 1.0f);
    private const float CORRECTION_IMPULSE_FACTOR = 0.01f;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    public override void _PhysicsProcess(double delta)
    {
        var input_dir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        var direction = new Vector3(input_dir.X, 0f, input_dir.Y);
        var target_velocity = direction * TargetSpeed;
        var velocity_error = target_velocity - LinearVelocity;
        var correction_impulse = _PID.Update(velocity_error, (float)delta) * CORRECTION_IMPULSE_FACTOR;
        if (correction_impulse.LengthSquared() > 1.0f) Print.Debug("Applying correctional impulse");
        ApplyCentralImpulse(correction_impulse);
    }

}
