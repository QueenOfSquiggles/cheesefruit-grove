using System;
using Godot;

public partial class PotionLiquidEffect : MeshInstance3D
{

    [Export] private float Inertia = 100f;

    private Vector2 PotionWobble = new();
    private ShaderMaterial? ShaderMat;
    private Vector2 LastRotation = new();

    public override void _Ready()
    {
        ShaderMat = MaterialOverride as ShaderMaterial;
    }

    public override void _Process(double delta)
    {
        if (ShaderMat is null) return;
        LastRotation.X = GlobalRotation.X;
        LastRotation.Y = GlobalRotation.Z;
        float factor = Inertia * (float)delta;
        PotionWobble.X = Mathf.LerpAngle(PotionWobble.X, LastRotation.X, factor);
        PotionWobble.Y = Mathf.LerpAngle(PotionWobble.Y, LastRotation.Y, factor);
        SetInstanceShaderParameter("potion_wobble", PotionWobble);
    }
}
