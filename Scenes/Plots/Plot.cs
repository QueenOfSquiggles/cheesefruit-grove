using System;
using Godot;

[GlobalClass]
public partial class Plot : Resource
{

    [Export] public string ID = "generic_id";
    [Export] public Texture2D Icon = null;
    [Export] public PackedScene WorldScene;

}
