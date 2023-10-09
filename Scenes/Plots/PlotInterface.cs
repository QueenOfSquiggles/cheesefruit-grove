using System;
using System.Collections.Generic;
using Godot;
using queen.data;
using queen.error;

public partial class PlotInterface : Node3D
{

    public Plot PlotType;

    public virtual void SetPlotType(Plot p_plot_type) => PlotType = p_plot_type;

    public virtual void CreateModifyPrompt()
    {
        Print.Debug($"Create Modify Prompt for '{PlotType?.ID}' - Not yet implemented");
    }

    public virtual void LoadPlotData(SaveDataBuilder builder) { }

    public virtual void SavePlotData(ref SaveDataBuilder builder) { }
}
