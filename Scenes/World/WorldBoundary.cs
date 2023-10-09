using System;
using Godot;

public partial class WorldBoundary : Area3D
{
    [Export] private string PlayerGroupName = "player";
    [Export] private NodePath RefRespawnPoint;

    private void OnBodyEnter(Node3D node)
    {
        // resets all fallen
        var pos = GetNode<Node3D>(RefRespawnPoint);
        node.GlobalPosition = pos.GlobalPosition;
    }
}
