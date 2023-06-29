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

    [Export] private float ShootRateCurveMax = 1.5f;
    [Export] private float ShootRateCurveDuration = 1.5f;
    [Export] private Curve ShootRateCurve;

    [ExportGroup("Suction Settings")]
    [Export] private Area3D.SpaceOverride ActiveSpaceOverride = Area3D.SpaceOverride.CombineReplace;
    [Export] private float SuctionGravitySpeed = 19.6f;
    [Export] private float SuctionGravityTweenDuration = 0.1f;
    [ExportSubgroup("Suction VFX", "SuctionVFX")]
    [Export] private NodePath PathSuctionVFX;
    [Export] private float SuctionVFXActiveDiscardThreshold = 0.7f;
    [Export] private float SuctionVFXTweenDuration = 0.3f;

    [ExportGroup("Movement Stats")]
    [Export] protected float Acceleration = 0.3f;
    [Export] protected float JumpVelocity = 4.5f;
    [Export] protected float mouse_sensitivity = 0.003f;
    [Export] protected float StepHeight = 0.4f;
    [Export] protected float StepStrength = 3.0f;

    [ExportGroup("Node Paths", "Path")]
    [Export] private NodePath PathItemCollector;
    [Export] private NodePath PathSuctionArea;

    [Export] private NodePath PathVCamRoot;
    [Export] private NodePath PathStepCheckTop;
    [Export] private NodePath PathStepCheckBottom;
    [Export] private NodePath PathInteractSensor;
    [Export] private NodePath PathThirdPersonCam;
    [Export] private NodePath PathStats;
    [Export] private NodePath PathInventoryManager;



    // References
    private ItemCollector _ItemCollector;
    private Area3D _SuctionArea;

    private InventoryManager _Inventory;
    protected VirtualCamera ThirdPersonCam;
    protected Node3D VCamRoot;
    protected RayCast3D CanStepCheckTop;
    protected RayCast3D CanStepCheckBottom;
    protected InteractionSensor InterSensor;
    private CharStatManager Stats;
    private MeshInstance3D SuctionVFX;
    private Tween SuctionVFXTween;

    // Values
    protected Vector2 camera_look_vector = new();
    protected float CurrentSpeed = 0.0f;
    protected bool IsOnStairs = false;

    protected float CanStepCheckTop_CastLength = 1.0f;
    private float CanStepCheckBottom_CastLength = 1.0f;
    protected Vector2 InputVector = new();
    private float _ShootTimer = 0.0f;
    private bool _IsSuction = false;
    private bool _IsShooting = false;
    private float _ShootDuration = 0.0f;

    protected float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();


    public override void _Ready()
    {
        this.GetSafe(PathVCamRoot, out VCamRoot);
        this.GetSafe(PathStepCheckTop, out CanStepCheckTop);
        this.GetSafe(PathStepCheckBottom, out CanStepCheckBottom);
        this.GetSafe(PathItemCollector, out _ItemCollector);
        this.GetSafe(PathSuctionArea, out _SuctionArea);
        this.GetSafe(PathInteractSensor, out InterSensor);
        this.GetSafe(PathThirdPersonCam, out ThirdPersonCam);
        this.GetSafe(PathStats, out Stats);
        this.GetSafe(PathSuctionVFX, out SuctionVFX);
        this.GetSafe(PathInventoryManager, out _Inventory);

        var mat = SuctionVFX.GetActiveMaterial(0) as ShaderMaterial;
        mat?.SetShaderParameter("discard_min", 1.0);
        mat?.SetShaderParameter("length_shown", 0.0f);

        CanStepCheckTop.Position += new Vector3(0, StepHeight, 0);
        CanStepCheckBottom_CastLength = CanStepCheckBottom.TargetPosition.Length();
        CanStepCheckTop_CastLength = CanStepCheckTop.TargetPosition.Length();

        Events.Gameplay.RequestPlayerAbleToMove += HandleEventPlayerCanMove;
        _ItemCollector.Enabled = false;
        //_SuctionArea.GravitySpaceOverride = Area3D.SpaceOverride.Disabled;
        Input.MouseMode = Input.MouseModeEnum.Captured;

        var MaxHealth = Stats.GetStat("max_health");
        var MaxEnergy = Stats.GetStat("max_energy");
        Stats.CreateDynamicStat("health", MaxHealth, MaxHealth, 0f, "max_health", "health_regen_factor");
        Stats.CreateDynamicStat("energy", MaxEnergy, MaxEnergy, 0f, "max_energy", "energy_regen_factor");
        Events.Gameplay.TriggerPlayerStatsUpdated(Stats.GetStat("health"), Stats.GetStat("max_health"), Stats.GetStat("energy"), Stats.GetStat("max_energy"));

        _Inventory.SlotUpdate += Events.GUI.TriggerUpdatePlayeInventoryDisplay;
        _Inventory.SlotSelect += Events.GUI.TriggerPlayerInventorySelect;
        int slots = (int)Stats.GetStat("inventory_size");
        _Inventory.ResizeInventory(slots);
        _Inventory.MaxItemsPerSlot = (int)Stats.GetStat("inventory_slot_capacity");
        Events.GUI.TriggerInventoryResized(slots);
        _Inventory.UpdateGUICall(Events.GUI.TriggerUpdatePlayeInventoryDisplay);
        Events.GUI.TriggerPlayerInventorySelect(_Inventory.Selected);

        Events.Gameplay.TriggerPlayerMoneyChange((int)Stats.GetStat("money"));
        Stats.OnStatChange += (stat, val) =>
        {
            if (stat == "money") Events.Gameplay.TriggerPlayerMoneyChange((int)val);
        };

        Events.Data.SerializeAll += SaveToData;
        Events.Data.Reload += LoadFromData;
        LoadFromData();

    }

    public override void _ExitTree()
    {
        Events.Gameplay.RequestPlayerAbleToMove -= HandleEventPlayerCanMove;
        Events.Data.SerializeAll -= SaveToData;
        Events.Data.Reload -= LoadFromData;
    }


    private bool ToggleSuction(InputEvent e)
    {
        if (!e.IsAction("suction")) return false;
        var mat = SuctionVFX.GetActiveMaterial(0) as ShaderMaterial;
        SuctionVFXTween?.Kill();
        SuctionVFXTween = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        if (e.IsPressed())
        {
            // starting stuff
            _SuctionArea.Gravity = 0.0f; // always start from zero
            _IsSuction = _ItemCollector.Enabled = true;// enables pickups and suction
            ManageSuctionSettings(); // set up properties and wake up available opbjects

            // sequence
            SuctionVFXTween.TweenProperty(mat, "shader_parameter/discard_min", SuctionVFXActiveDiscardThreshold, SuctionVFXTweenDuration);
            SuctionVFXTween.Parallel().TweenProperty(mat, "shader_parameter/length_shown", 1.0f, SuctionVFXTweenDuration);
            SuctionVFXTween.Parallel().TweenProperty(_SuctionArea, "gravity", SuctionGravitySpeed, SuctionGravityTweenDuration);
        }
        else
        {
            _SuctionArea.Gravity = 0.0f; // reset to zero instantly
            _ItemCollector.Enabled = _IsSuction = false;
            ManageSuctionSettings();
            SuctionVFXTween.TweenProperty(mat, "shader_parameter/discard_min", 1.1f, SuctionVFXTweenDuration);
            SuctionVFXTween.Parallel().TweenProperty(mat, "shader_parameter/length_shown", 0.0f, SuctionVFXTweenDuration);
        }

        return true;
    }

    private void ManageSuctionSettings()
    {
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
            _ShootTimer = 0.0f;
            _ShootDuration = 0.0f;
        }
        else
        {
            _IsShooting = false;
        }
        return true;
    }

    private void ShootItem(string itemID)
    {
        var itemDef = RegistrationManager.Entities.GetValueOrDefault(itemID);
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
    }

    private void OnPickupItem(Node3D node)
    {
        if (node.GetComponent<WorldItemComponent>() is not WorldItemComponent wic) return;

        if (_Inventory.TryAddItem(wic.ItemID)) node.QueueFree();
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

        if (_IsShooting)
        {
            _ShootTimer -= (float)delta;
            _ShootDuration += (float)delta;
            if (_ShootTimer <= 0)
            {
                var item_id = _Inventory.GetSelectedItem();
                if (_Inventory.RemoveItem()) ShootItem(item_id);
                _ShootTimer = ShootRateCurve.SampleBaked(Mathf.Min(_ShootDuration / ShootRateCurveDuration, 1.0f)) * ShootRateCurveMax;
            }
        }

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
            var target_speed = Stats.GetStat("base_speed");
            if (sprinting) target_speed *= Stats.GetStat("sprint_speed_factor");
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
            handled |= DebugStuff(e);
            handled |= InputSelectItems(e);
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

    private bool DebugStuff(InputEvent e)
    {
#if DEBUG
        if (!e.IsPressed()) return false;
        if (e is not InputEventKey key) return false;
        if (key.Keycode == Key.KpAdd)
        {
            Print.Debug("Adding energy regen buff");
            Stats.AddStatMod("energy_regen_factor", 50.0f, CharStatFloat.Modifier.ADD, 1.0f);
            return true;
        }
        if (key.Keycode == Key.Kp7)
        {
            Stats.ModifyStaticStat("money", 99.0f);
            return true;
        }
        if (key.Keycode == Key.Kp8)
        {
            Stats.ModifyStaticStat("money", -99.0f);
            return true;
        }
        if (key.Keycode == Key.Kp4)
        {
            Stats.ModifyStaticStat("money", 9999.0f);
            return true;
        }
        if (key.Keycode == Key.Kp5)
        {
            Stats.ModifyStaticStat("money", -9999.0f);
            return true;
        }
#endif
        return false;
    }

    private bool InputSelectItems(InputEvent e)
    {
        if (!e.IsPressed()) return false;
        if (e.IsAction("item_select_next"))
        {
            _Inventory.SelectNext();
            return true;
        }
        if (e.IsAction("item_select_previous"))
        {
            _Inventory.SelectPrevious();
            return true;
        }
        int sel = -1;
        if (e is InputEventKey key)
        {
            switch (key.Keycode)
            {
                case Key.Key0:
                    sel = 9;
                    break;
                case Key.Key1:
                    sel = 0;
                    break;
                case Key.Key2:
                    sel = 1;
                    break;
                case Key.Key3:
                    sel = 2;
                    break;
                case Key.Key4:
                    sel = 3;
                    break;
                case Key.Key5:
                    sel = 4;
                    break;
                case Key.Key6:
                    sel = 5;
                    break;
                case Key.Key7:
                    sel = 6;
                    break;
                case Key.Key8:
                    sel = 7;
                    break;
                case Key.Key9:
                    sel = 8;
                    break;
            }
        }
        if (sel >= 0)
        {
            _Inventory.SetSelection(sel);
            return true;
        }
        return false;
    }


    private const string SAVE_FILE = "player.json";

    private void SaveToData()
    {
        var build = new SaveDataBuilder(SAVE_FILE);

        build.PutVector3(nameof(GlobalPosition), GlobalPosition);
        build.PutVector3(nameof(GlobalRotation), GlobalRotation);
        _Inventory.SaveToData(ref build);
        Stats.SaveToData(ref build);

        build.SaveToFile();
    }

    private void LoadFromData()
    {
        var builder = new SaveDataBuilder(SAVE_FILE).LoadFromFile();

        GlobalPosition = builder.GetVector3(nameof(GlobalPosition));
        GlobalRotation = builder.GetVector3(nameof(GlobalRotation));
        _Inventory.LoadFromData(builder);
        Stats.LoadFromData(builder);
    }

}