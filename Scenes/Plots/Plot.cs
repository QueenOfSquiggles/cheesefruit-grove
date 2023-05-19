using System;
using Godot;
using MonoCustomResourceRegistry;

[RegisteredType(nameof(Plot))]
public partial class Plot : Resource
{

    [Export] public string ID = "generic_id";
    [Export] public PackedScene WorldScene;
}
