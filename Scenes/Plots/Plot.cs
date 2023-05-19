using System;
using Godot;
using MonoCustomResourceRegistry;

[RegisteredType(nameof(Plot))]
public partial class Plot : Resource
{

    [Export] public string ID = "generic_id";
    [Export] public Texture2D Icon = null;
    [Export] public PackedScene WorldScene;

}
