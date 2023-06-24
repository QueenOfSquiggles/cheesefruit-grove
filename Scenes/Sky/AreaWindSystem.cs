using System;
using Godot;
using queen.extension;

[Tool]
public partial class AreaWindSystem : Area3D
{


    private Marker3D WindMarker;
    public override void _Ready()
    {
        UpdateWindShaderSettings();
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint()) UpdateWindShaderSettings();
    }

    private void UpdateWindShaderSettings()
    {
        if (WindSourcePath == "") return;
        WindMarker = GetNode<Marker3D>(WindSourcePath);
        if (WindMarker is null) return;

        WindMarker.GlobalTransform = GlobalTransform;
        float c = Mathf.Cos(WindMarker.GlobalRotation.Y);
        float s = Mathf.Sin(WindMarker.GlobalRotation.Y);
        RenderingServer.GlobalShaderParameterSet("weather_wind_velocity", new Vector3(s, 0.0f, c));
    }




}
