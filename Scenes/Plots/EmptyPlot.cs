using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using queen.data;
using queen.events;
using queen.extension;

public partial class EmptyPlot : Node3D
{

    [Export] private NodePath PathPromptNewPlot;
    [Export] private NodePath PathPlotRoot;
    [Export] private NodePath PathInteractionTrigger;
    [Export] private NodePath PathPlotLabel;

    private Node3D PlotRoot;
    private Label3D PlotLabel;
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

        Events.Data.SerializeAll += SaveToData;
        Events.Data.Reload += LoadFromData;
        LoadFromData();
    }

    public override void _ExitTree()
    {
        Events.Data.SerializeAll -= SaveToData;
        Events.Data.Reload -= LoadFromData;
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


    private const string KEY_PLOT = "plot_type";
    private void ModifyExistingPlot()
    {
        if (PlotRoot?.GetChild(0) is not PlotInterface plot) return;
        plot.CreateModifyPrompt();
    }

    private void SaveToData()
    {
        var builder = new SaveDataBuilder(GetFile());

        if (PlotRoot.GetChildCount() > 0)
        {
            if (PlotRoot.GetChild(0) is PlotInterface plot)
            {
                builder.PutString(KEY_PLOT, plot.PlotType is null ? "" : plot.PlotType.ID);
                plot.SavePlotData(ref builder);
            }
        }
        builder.SaveToFile();
    }
    private void LoadFromData()
    {
        var builder = new SaveDataBuilder(GetFile()).LoadFromFile();
        if (builder.GetString(KEY_PLOT) != "")
        {
            var plot = RegistrationManager.GetResource<Plot>(builder.GetString(KEY_PLOT));
            if (plot is null) return;
            MakeNewPlot(plot);
            (PlotRoot.GetChild(0) as PlotInterface)?.LoadPlotData(builder);
        }
    }

    private string GetFile()
    {
        return $"{GetTree().CurrentScene.Name}/plot__{Name}.json";
    }

}
