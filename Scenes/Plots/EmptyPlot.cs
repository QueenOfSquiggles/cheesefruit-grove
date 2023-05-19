using System;
using Godot;
using queen.extension;

public partial class EmptyPlot : Node3D
{

    [Export] private Plot PlotType;
    [Export] private NodePath PathPlotRoot;
    [Export] private Script PlotInterfaceScript;

    private Node3D? PlotRoot;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        this.GetSafe(PathPlotRoot, out PlotRoot);
    }

    private void OnInteract()
    {
        if (PlotRoot is null) return;
        if (PlotRoot.GetChildCount() > 0) ModifyExistingPlot();
        else MakeNewPlot();
    }

    private void MakeNewPlot()
    {
        if (PlotRoot is null) return;
        if (PlotType is null) return;
        var world_scene = PlotType?.WorldScene?.Instantiate();
        if (world_scene is null) return; // failed to create world scene
        if (world_scene is PlotInterface plot) plot.SetPlotType(PlotType);
        PlotRoot.AddChild(world_scene);
    }

    private void ModifyExistingPlot()
    {
        if (PlotRoot?.GetChild(0) is not PlotInterface plot) return;
        plot.CreateModifyPrompt();
    }

}
