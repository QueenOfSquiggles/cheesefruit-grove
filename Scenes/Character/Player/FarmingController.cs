using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using queen.error;
using queen.extension;

public partial class FarmingController : PsuedoAAACharController
{

    [ExportCategory("Farm Character")]

    [ExportGroup("Shoot Settings")]
    [Export] private float ShootImpulseForce = 9.8f;

    [ExportGroup("Suction Settings")]
    [Export] private Area3D.SpaceOverride ActiveSpaceOverride = Area3D.SpaceOverride.CombineReplace;

    [ExportGroup("Node Paths")]
    [Export] private NodePath PathItemCollector;
    [Export] private NodePath PathSuctionArea;


    private bool _IsSuction = false;
    private bool _IsShooting = false;
    private ItemCollector _ItemCollector;
    private Area3D _SuctionArea;

    private Dictionary<string, int> _Items = new();

    public override void _Ready()
    {
        base._Ready();
        this.GetSafe(PathItemCollector, out _ItemCollector);
        this.GetSafe(PathSuctionArea, out _SuctionArea);
        _ItemCollector.Enabled = false;
        _SuctionArea.GravitySpaceOverride = Area3D.SpaceOverride.Disabled;
    }

    protected override bool ExtraInputEventHandling(InputEvent e)
    {
        if (ToggleSuction(e)) return true;
        if (ToggleFiring(e)) return true;
        return false;
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


    // Redirects to make Godot Happy


}
