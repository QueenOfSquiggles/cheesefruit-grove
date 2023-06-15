using System;
using Godot;
using Godot.Collections;
using Godot.NativeInterop;
using queen.error;
using queen.extension;

[Tool]
public partial class SkyboxSystem : Node3D
{
    [ExportGroup("Time")]
    [Export(PropertyHint.Range, "0,2400,10,or_greater,or_less")] private float CurrentTime = 1200.0f;
    [Export] private float CycleDuration = 2400.0f;

    [ExportGroup("Sky Settings")]
    [ExportSubgroup("General")]
    [Export] private float AxisOffSet = 0.1f;
    [Export] private Curve SkyFactor;

    [ExportSubgroup("Sun")]
    [Export(PropertyHint.Range, "0,1")] private float DaylightIntensity = 1.0f;
    [Export] private Color SunColour = Colors.White;
    [Export] private Curve SunIntensityCurve;

    [ExportSubgroup("Moon")]
    [Export(PropertyHint.Range, "0,1")] private float MoonlightIntensity = 0.5f;
    [Export] private Color MoonColour = Colors.AliceBlue;
    [Export] private Curve MoonIntensityCurve;

    [ExportSubgroup("Cycle")]
    [Export] private bool AutoProgressCycle = false;
    [Export] private float SecondsPerCycle = 600.0f; // 15 minutes per cycle (7.5 day, 7.5 night)

    [ExportGroup("Node Paths")]
    [Export] private NodePath PathWorldEnvironment;
    [Export] private NodePath PathLightsChunk;
    [Export] private NodePath PathSun;
    [Export] private NodePath PathMoon;
    [ExportGroup("Debugging")]
    [Export] private bool UpdatePropsEveryFrame = false;
    [Export] private float DEBUG_LightsAngle = 0.0f;
    [Export] private float DEBUG_TOD_Percent = 0.0f;





    private WorldEnvironment Env;
    private Node3D Lights;
    private DirectionalLight3D Sun;
    private DirectionalLight3D Moon;
    private bool HasInit = false;

    public override void _Ready()
    {
        LoadRefs();
    }

    private void LoadRefs()
    {
        if (HasInit) return;
        this.GetSafe(PathWorldEnvironment, out Env, false);
        this.GetSafe(PathLightsChunk, out Lights, false);
        this.GetSafe(PathSun, out Sun, false);
        this.GetSafe(PathMoon, out Moon, false);

        // Sun
        Sun.LightEnergy = DaylightIntensity;
        Sun.LightColor = SunColour;

        // Moon
        Moon.LightEnergy = MoonlightIntensity;
        Moon.LightColor = MoonColour;
        HasInit = true;

        // Dynamic Properties
        UpdateProps();
    }

    private void UpdateProps()
    {
        if (!HasInit) return;
        // Angle
        CurrentTime = Mathf.PosMod(CurrentTime, CycleDuration);
        var todPercent = CurrentTime / CycleDuration;
        var curve = GetDayCycleCurve(todPercent);
        var angle = todPercent * 2.0f * Mathf.Pi;
        Lights.Rotation = Lights.Rotation.SetRotateX(angle);
        Lights.Rotation = Lights.Rotation.SetRotateY(curve * AxisOffSet);

        // Energy
        Sun.LightEnergy = DaylightIntensity * SunIntensityCurve.SampleBaked(todPercent);
        Moon.LightEnergy = MoonlightIntensity * MoonIntensityCurve.SampleBaked(todPercent);

        RenderingServer.GlobalShaderParameterSet("time_of_day", SkyFactor.SampleBaked(todPercent));
        RenderingServer.GlobalShaderParameterSet("weather_sunmoon_angle", Lights.Rotation);

#if DEBUG
        DEBUG_LightsAngle = angle;
        DEBUG_TOD_Percent = todPercent;
#endif
    }

    private float GetDayCycleCurve(float todPercent)
    {
        return -4.0f * Mathf.Pow(todPercent - 0.5f, 2.0f) + 1.0f;
    }

    private void DoAutoProgression(float delta)
    {
        var rate = CycleDuration / SecondsPerCycle;
        CurrentTime += rate * delta;
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint())
        {
            if (!HasInit) LoadRefs();
            if (UpdatePropsEveryFrame) UpdateProps();
        }
        else
        {
            UpdateProps();
        }

        if (AutoProgressCycle) DoAutoProgression((float)delta);
    }

    public bool IsDay() => CurrentTime >= 0 && CurrentTime <= (CycleDuration / 2.0);
    public bool IsNight() => CurrentTime >= (CycleDuration / 2.0) && CurrentTime < CycleDuration;

}
