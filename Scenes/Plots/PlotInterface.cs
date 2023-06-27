using System;
using Godot;
using queen.error;

public partial class PlotInterface : Node3D
{
    private Plot PlotType;

    public virtual void SetPlotType(Plot p_plot_type) => PlotType = p_plot_type;

    public virtual void CreateModifyPrompt()
    {
        Print.Debug($"Create Modify Prompt for '{PlotType?.ID}' - Not yet implemented");
    }
}
