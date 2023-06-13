using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using interaction;
using queen.data;
using queen.error;
using queen.events;
using queen.extension;

public partial class FarmingController : CharacterBody3D
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
    [Export] protected float mouse_sensitivity = 0.003f;
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
    [Export] private NodePath PathInteractSensor;
    [Export] private NodePath PathThirdPersonCam;
    [Export] private NodePath PathStats;

    private bool _IsSuction = false;
    private bool _IsShooting = false;
    private ItemCollector _ItemCollector;
    private Area3D _SuctionArea;

    private Dictionary<string, int> _Items = new();



    // References
    protected VirtualCamera vcam;
    protected VirtualCamera ThirdPersonCam;
    protected Node3D VCamRoot;
    protected RayCast3D CanStepCheckTop;
    protected RayCast3D CanStepCheckBottom;
    protected RayCast3D InteractionRay;
    protected InteractionSensor InterSensor;
    private CharStatManager Stats;

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

    protected float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();


    public override void _Ready()
    {
        this.GetSafe(PathVCam, out vcam);
        this.GetSafe(PathVCamRoot, out VCamRoot);
        this.GetSafe(PathStepCheckTop, out CanStepCheckTop);
        this.GetSafe(PathStepCheckBottom, out CanStepCheckBottom);
        this.GetSafe(PathInteractRay, out InteractionRay);
        this.GetSafe(PathItemCollector, out _ItemCollector);
        this.GetSafe(PathSuctionArea, out _SuctionArea);
        this.GetSafe(PathInteractSensor, out InterSensor);
        this.GetSafe(PathThirdPersonCam, out ThirdPersonCam);
        this.GetSafe(PathStats, out Stats);

        CanStepCheckTop.Position += new Vector3(0, StepHeight, 0);
        CanStepCheckBottom_CastLength = CanStepCheckBottom.TargetPosition.Length();
        CanStepCheckTop_CastLength = CanStepCheckTop.TargetPosition.Length();

        Events.Gameplay.RequestPlayerAbleToMove += HandleEventPlayerCanMove;
        _ItemCollector.Enabled = false;
        _SuctionArea.GravitySpaceOverride = Area3D.SpaceOverride.Disabled;
        Input.MouseMode = Input.MouseModeEnum.Captured;

        var MaxHealth = Stats.GetStat("max_health");
        var MaxEnergy = Stats.GetStat("max_energy");
        Stats.CreateDynamicStat("health", MaxHealth, MaxHealth, 0f, "max_health", "health_regen_factor");
        Stats.CreateDynamicStat("energy", MaxEnergy, MaxEnergy, 0f, "max_energy", "energy_regen_factor");
        Events.Gameplay.TriggerPlayerStatsUpdated(Stats.GetStat("health"), Stats.GetStat("max_health"), Stats.GetStat("energy"), Stats.GetStat("max_energy"));
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
        if (scene is RigidBody3D rb) rb.ApplyCentralImpulse(-VCamRoot.GlobalTransform.Basis.Z * ShootImpulseForce);

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
    }


    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;
        if (!IsOnFloor()) velocity.Y -= gravity * (float)delta;

        if (Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            CamLookLogic(delta);
            CamMoveLogic(ref velocity, delta);
            JumpLogic(ref velocity, delta);
            StepLogic(ref velocity, delta);
        }
        Velocity = velocity;
        MoveAndSlide();

        Events.Gameplay.TriggerPlayerStatsUpdated(Stats.GetStat("health"), Stats.GetStat("max_health"), Stats.GetStat("energy"), Stats.GetStat("max_energy"));
    }

    private void JumpLogic(ref Vector3 velocity, double delta)
    {
        // Add the gravity.

        // Handle Jump.
        if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
            velocity.Y = JumpVelocity;
    }

    private void CamMoveLogic(ref Vector3 velocity, double delta)
    {

        InputVector = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        if (InputVector.LengthSquared() < 0.1f)
            InputVector = Input.GetVector("gamepad_move_left", "gamepad_move_right", "gamepad_move_forward", "gamepad_move_back");

        Vector3 direction = (Transform.Basis * new Vector3(InputVector.X, 0, InputVector.Y)).Normalized();
        if (direction != Vector3.Zero)
        {
            // TODO Holy fuck this is some messy logic. This should really get cleaned up somehow. Maybe by having a series of contributing factors? IDK
            // Sprint or No Sprint
            // Sprint if button pressed, on floor, and energy is available to consume
            var sprinting = IsOnFloor() && Input.IsActionPressed("sprint") && Stats.ConsumeDynStat("energy", Stats.GetStat("sprint_energy_use") * (float)delta);
            var target_speed = sprinting ? SprintSpeed : Speed;
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, target_speed, Acceleration);
            velocity.X = direction.X * CurrentSpeed;
            velocity.Z = direction.Z * CurrentSpeed;
        }
        else
        {
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, 0, Acceleration);
            velocity.X = Mathf.MoveToward(Velocity.X, 0, CurrentSpeed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, CurrentSpeed);
        }
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

        RotateY(look.X * mouse_sensitivity);

        var rot = VCamRoot.Rotation;
        rot.X += look.Y * mouse_sensitivity;
        var cl = Mathf.DegToRad(89.0f);
        rot.X = Mathf.Clamp(rot.X, -cl, cl);
        VCamRoot.Rotation = rot;

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
            handled |= InputToggleThirdPerson(e);
            handled |= AddTestBuff(e);
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
        // return value relates to handling input, not whether task was successful
        if (!e.IsActionPressed("interact")) return false;
        if (InterSensor.CurrentInteraction is not IInteractable inter) return true;
        inter.Interact();
        return true;
    }

    private bool InputToggleThirdPerson(InputEvent e)
    {
        if (!e.IsActionPressed("toggle_third_person")) return false;

        if (ThirdPersonCam.IsOnStack) ThirdPersonCam.PopVCam();
        else ThirdPersonCam.PushVCam();

        return true;
    }

    private bool AddTestBuff(InputEvent e)
    {
        if (e is not InputEventKey key) return false;
        if (key.Keycode != Key.KpAdd) return false;
        Print.Debug("Adding energy regen buff");
        Stats.AddStatMod("energy_regen_factor", 50.0f, CharStatFloat.Modifier.ADD, 1.0f);
        return true;
    }
}
