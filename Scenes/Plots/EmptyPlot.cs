using System;
using System.Linq;
using Godot;
using queen.events;
using queen.extension;

public partial class EmptyPlot : Node3D
{

    [Export] private NodePath PathPromptNewPlot;
    [Export] private NodePath PathPlotRoot;
    [Export] private NodePath PathInteractionTrigger;
    [Export] private NodePath PathPlotLabel;

    private Node3D? PlotRoot;
    private Label3D? PlotLabel;
    private PopupMenu MenuPromptNewPlot;
    private InteractiveTrigger InteractTrigger;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        this.GetSafe(PathPlotRoot, out PlotRoot);
        this.GetSafe(PathPlotLabel, out PlotLabel);
        this.GetSafe(PathPromptNewPlot, out MenuPromptNewPlot);
        this.GetSafe(PathInteractionTrigger, out InteractTrigger);
        var plot_types = RegistrationManager.Plots.Values.ToList();
        foreach (var plot in plot_types)
        {
            MenuPromptNewPlot.AddIconItem(plot.Icon, plot.ID);
        }
        MenuPromptNewPlot.Hide();
        // MenuPromptNewPlot.IndexPressed += OnMenuIndexPressed;
    }

    private void OnInteract()
    {
        if (PlotRoot is null) return;
        if (PlotRoot.GetChildCount() > 0) ModifyExistingPlot();
        else PromptMakeNewPlot();
    }

    private void PromptMakeNewPlot()
    {
        Events.Gameplay.TriggerRequestPlayerAbleToMove(false);
        Input.MouseMode = Input.MouseModeEnum.Visible;
        MenuPromptNewPlot.PopupCentered();
    }

    private void OnMenuIndexPressed(int idx)
    {
        var plot_id = MenuPromptNewPlot.GetItemText(idx);
        var plot_types = RegistrationManager.Plots.Values.ToList();
        foreach (var plot in plot_types)
        {
            if (plot.ID == plot_id) MakeNewPlot(plot);
        }
    }

    private void OnPopupHide()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        Events.Gameplay.TriggerRequestPlayerAbleToMove(true);
    }

    private void MakeNewPlot(Plot PlotType)
    {
        if (PlotRoot is null) return;
        if (PlotType is null) return;

        var world_scene = PlotType.WorldScene?.Instantiate();
        if (world_scene is null) return; // failed to create world scene
        if (world_scene is PlotInterface plot) plot.SetPlotType(PlotType);
        PlotRoot.AddChild(world_scene);
        InteractTrigger.custom_name = PlotType.ID;
        if (PlotLabel is not null) PlotLabel.Text = PlotType.ID;
    }

    private void ModifyExistingPlot()
    {
        if (PlotRoot?.GetChild(0) is not PlotInterface plot) return;
        plot.CreateModifyPrompt();
    }

}
