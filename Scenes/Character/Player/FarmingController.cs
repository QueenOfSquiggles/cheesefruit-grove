using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using interaction;
using queen.data;
using queen.error;
using queen.events;
using queen.extension;

public partial class FarmingController : RigidBody3D
{

    [ExportCategory("Farm Character")]

    [ExportGroup("Shoot Settings")]
    [Export] private float ShootImpulseForce = 9.8f;

    [ExportGroup("Suction Settings")]
    [Export] private Area3D.SpaceOverride ActiveSpaceOverride = Area3D.SpaceOverride.CombineReplace;

    [ExportGroup("Movement Stats")]
    [Export] protected float Speed = 2.0f;
    [Export] protected float SprintSpeed = 5.0f;
    [Export] protected float Acceleration = 0.3f;
    [Export] protected float JumpVelocity = 4.5f;
    [Export] protected float mouse_sensitivity;
    [Export] protected float StepHeight = 0.4f;
    [Export] protected float StepStrength = 3.0f;

    [ExportGroup("Node Paths")]
    [Export] private NodePath PathItemCollector;
    [Export] private NodePath PathSuctionArea;

    [Export] private NodePath PathVCam;
    [Export] private NodePath PathVCamRoot;
    [Export] private NodePath PathStepCheckTop;
    [Export] private NodePath PathStepCheckBottom;
    [Export] private NodePath PathInteractRay;

    private bool _IsSuction = false;
    private bool _IsShooting = false;
    private ItemCollector _ItemCollector;
    private Area3D _SuctionArea;

    private Dictionary<string, int> _Items = new();



    // References
    protected VirtualCamera vcam;
    protected Node3D VCamRoot;
    protected RayCast3D CanStepCheckTop;
    protected RayCast3D CanStepCheckBottom;
    protected RayCast3D InteractionRay;

    // Values
    protected Vector2 camera_look_vector = new();
    protected float CurrentSpeed = 0.0f;
    protected bool IsOnStairs = false;

    protected float CanStepCheckTop_CastLength = 1.0f;
    private float CanStepCheckBottom_CastLength = 1.0f;
    protected Vector2 InputVector = new();
    protected bool LastWasInteractable = false;

    private PID3D _PID = new(1.0f, 0.1f, 1.0f);
    private const float CORRECTION_IMPULSE_FACTOR = 0.01f;



    public override void _Ready()
    {
        this.GetSafe(PathVCam, out vcam);
        this.GetSafe(PathVCamRoot, out VCamRoot);
        this.GetSafe(PathStepCheckTop, out CanStepCheckTop);
        this.GetSafe(PathStepCheckBottom, out CanStepCheckBottom);
        this.GetSafe(PathInteractRay, out InteractionRay);
        this.GetSafe(PathItemCollector, out _ItemCollector);
        this.GetSafe(PathSuctionArea, out _SuctionArea);

        CanStepCheckTop.Position += new Vector3(0, StepHeight, 0);
        CanStepCheckBottom_CastLength = CanStepCheckBottom.TargetPosition.Length();
        CanStepCheckTop_CastLength = CanStepCheckTop.TargetPosition.Length();

        Events.Gameplay.RequestPlayerAbleToMove += HandleEventPlayerCanMove;
        _ItemCollector.Enabled = false;
        _SuctionArea.GravitySpaceOverride = Area3D.SpaceOverride.Disabled;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }


    private bool ToggleSuction(InputEvent e)
    {
        if (!e.IsAction("suction")) return false;
        if (e.IsPressed())
        {
            _ItemCollector.Enabled = _IsSuction = true;
        }
        else
        {
            _ItemCollector.Enabled = _IsSuction = false;
        }
        _SuctionArea.GravitySpaceOverride = _IsSuction ? ActiveSpaceOverride : Area3D.SpaceOverride.Disabled;
        if (_IsSuction)
        {
            // wake up sleeping RigidBodies
            var bodies = _SuctionArea.GetOverlappingBodies();
            foreach (var b in bodies)
            {
                if (b is not RigidBody3D rb) continue;
                rb.Sleeping = false;
            }
        }
        Print.Debug($"Is Suction = {_IsSuction}");
        return true;
    }

    private void OnBodyEnterSuctionZone(Node3D node)
    {
        if (!_IsSuction) return;
        if (node is RigidBody3D rb) rb.Sleeping = false;
    }

    private bool ToggleFiring(InputEvent e)
    {
        if (!e.IsAction("shoot")) return false;
        if (e.IsPressed())
        {
            _IsShooting = true;
            if (_Items.Count > 0) ShootItem(_Items.Keys.First());
        }
        else
        {
            _IsShooting = false;
        }
        return true;
    }

    private void ShootItem(string itemID)
    {
        Print.Debug($"Shooting item '{itemID}'");
        var itemDef = RegistrationManager.Instance.Entities.GetValueOrDefault(itemID);
        if (itemDef is null) Print.Debug($"Registry for '{itemID}' was null");
        if (itemDef?.WorldScene is null) Print.Debug($"WorldEntity:WorldScene for '{itemID}' was null");
        if (itemDef?.WorldScene?.Instantiate() is not Node3D scene)
        {
            Print.Debug("Failed to construct valid item scene");
            return;
        }

        // add valid item scene to world
        scene.GlobalTransform = _SuctionArea.GlobalTransform;
        GetParent().AddChild(scene);

        // If valid item scene is Rigid Body, apply force
        if (scene is RigidBody3D rb) rb.ApplyCentralImpulse(-vcam.GlobalTransform.Basis.Z * ShootImpulseForce);

        // Remove item from inventory
        // TODO split responsibility of inventory management to different node
        if (_Items.ContainsKey(itemID))
        {
            _Items[itemID] -= 1;
            if (_Items[itemID] <= 0) _Items.Remove(itemID); // clear out zeroed items;
        }

    }

    private void OnPickupItem(Node3D node)
    {
        if (node.GetComponent<WorldItemComponent>() is not WorldItemComponent wic) return;
        if (_Items.ContainsKey(wic.ItemID))
        {
            _Items[wic.ItemID] += 1;
        }
        else
        {
            _Items.Add(wic.ItemID, 1);
        }
        node.QueueFree();
        Print.Debug($"Player picked up: '{wic.ItemID}', now holding {_Items[wic.ItemID]}");
    }




    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = LinearVelocity;
        if (Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            CamLookLogic(delta);
            CamMoveLogic(ref velocity, delta);
            JumpLogic(ref velocity, delta);
            StepLogic(ref velocity, delta);
        }

    }

    private void JumpLogic(ref Vector3 velocity, double delta)
    {
        // Add the gravity.

        // Handle Jump.
        // FIXME
        // if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
        //     velocity.Y = JumpVelocity;
    }

    private void CamMoveLogic(ref Vector3 velocity, double delta)
    {

        InputVector = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        if (InputVector.LengthSquared() < 0.1f)
            InputVector = Input.GetVector("gamepad_move_left", "gamepad_move_right", "gamepad_move_forward", "gamepad_move_back");

        Vector3 direction = VCamRoot.GlobalTransform.Basis.Z * InputVector.Y;
        direction += VCamRoot.GlobalTransform.Basis.X * InputVector.X;
        if (direction != Vector3.Zero)
        {
            // Sprint or No Sprint
            var target_speed = (IsOnFloor() && Input.IsActionPressed("sprint")) ? SprintSpeed : Speed;
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, target_speed, Acceleration);
        }
        else
        {
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, 0, Acceleration);
        }

        var target_velocity = direction * CurrentSpeed;
        var velocity_error = target_velocity - LinearVelocity;
        var correction_impulse = _PID.Update(velocity_error, (float)delta) * CORRECTION_IMPULSE_FACTOR;
        // TODO movement feels wrong. How to fix?
        ApplyCentralImpulse(correction_impulse);
    }

    private void StepLogic(ref Vector3 velocity, double _delta)
    {
        if (InputVector.LengthSquared() < 0.8f) return;

        var dir = new Vector3(InputVector.X, 0, InputVector.Y);
        CanStepCheckBottom.TargetPosition = dir * CanStepCheckBottom_CastLength;
        CanStepCheckTop.TargetPosition = dir * CanStepCheckTop_CastLength;

        if (!IsOnWall())
        {
            IsOnStairs = false;
            return;
        }

        IsOnStairs = CanStepCheckBottom.IsColliding() && !CanStepCheckTop.IsColliding();

        if (IsOnStairs)
        {
            velocity.Y = StepHeight * StepStrength;
        }
    }

    private void CamLookLogic(double delta)
    {
        var look = (camera_look_vector.LengthSquared() > 0.1f) ? camera_look_vector : GetGamepadLookVector();
        look *= (float)delta;

        VCamRoot.RotateY(look.X * mouse_sensitivity);

        var rot = vcam.Rotation;
        rot.X += look.Y * mouse_sensitivity;
        var cl = Mathf.DegToRad(89.0f);
        rot.X = Mathf.Clamp(rot.X, -cl, cl);
        vcam.Rotation = rot;

        camera_look_vector = Vector2.Zero;
    }

    private Vector2 GAMEPAD_VEC_FLIP = new(-1, 1);
    private Vector2 GetGamepadLookVector()
    {
        return Input.GetVector("gamepad_look_left", "gamepad_look_right", "gamepad_look_down", "gamepad_look_up")
        * Controls.Instance.ControllerLookSensitivity
        * GAMEPAD_VEC_FLIP;
    }


    private void HandleEventPlayerCanMove(bool can_move)
    {
        SetPhysicsProcess(can_move);
        // prevents random motion after returning
        AxisLockLinearX = !can_move;
        AxisLockLinearY = !can_move;
        AxisLockLinearX = !can_move;
        camera_look_vector = Vector2.Zero;
    }
    public override void _UnhandledInput(InputEvent e)
    {
        bool handled = false;
        if (Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            handled |= InputMouseLook(e);
            handled |= InputInteract(e);
            handled |= ToggleSuction(e);
            handled |= ToggleFiring(e);
        }
        if (handled) GetViewport().SetInputAsHandled();
    }
    private bool InputMouseLook(InputEvent e)
    {
        if (e is not InputEventMouseMotion mm) return false;
        camera_look_vector += mm.Relative * Controls.Instance.MouseLookSensivity * -1f;
        return true;
    }

    private bool InputInteract(InputEvent e)
    {

        InteractionRay.ForceRaycastUpdate();

        if (InteractionRay.GetCollider() is Node collider && collider is IInteractable inter && inter.IsActive())
        {
            if (!LastWasInteractable)
            {
                LastWasInteractable = true;
                Events.GUI.TriggerAbleToInteract(inter.GetActiveName());
            }

            if (!e.IsActionPressed("interact")) return false;
            else if (inter.Interact())
            {
                // TODO: do we want anything to happen on this end? Realistically the Interact object should handle SFX, VFX, etc...
            }
        }
        else if (LastWasInteractable)
        {
            LastWasInteractable = false;
            Events.GUI.TriggerUnableToInteract();
        }

        return true;
    }

    private bool IsOnFloor()
    {
        // TODO figure out replacement
        return false;
    }

    private bool IsOnWall()
    {
        // TODO figure out replacement
        return false;
    }

}
