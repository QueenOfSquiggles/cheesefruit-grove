using System;
using Godot;
using queen.error;
using queen.extension;

public partial class ItemCollector : Area3D
{

    [Signal] public delegate void OnItemPickupEventHandler(Node3D nodeRef);
    [Signal] public delegate void OnItemRejectedEventHandler(Node3D nodeRef);

    [Export] public string ItemGroupName = "item";
    [Export] public string[] Filters = Array.Empty<string>();
    [Export] public bool Enabled = true;

    private void OnBodyEnter(Node3D node)
    {
        if (!Enabled) return;
        if (!node.IsInGroup(ItemGroupName)) return;
        if (node.GetComponent<WorldItemComponent>() is not WorldItemComponent wic) return;
        foreach (var f in Filters)
        {
            if (!node.IsInGroup(f))
            {
                EmitSignal(nameof(OnItemRejected), node);
                return;
            }
        }
        // in item group and passing all filters (or no filters are applied)
        EmitSignal(nameof(OnItemPickup), node);
        return;
    }
}
