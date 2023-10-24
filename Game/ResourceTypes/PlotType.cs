namespace Squiggles.CheeseFruitGroves.Plot;

using Godot;
using Squiggles.Core.Data;
using System;

[GlobalClass, Icon("res://Game/Assets/UsefulEditorIcons/plot_resource.svg")]
public partial class PlotType : Resource, IRegistryID {

  [Export] public string PlotName;
  [Export] public int Cost;
  [Export] public PlotUpgrade[] Upgrades = Array.Empty<PlotUpgrade>();
  [Export] public PackedScene PlotScene;

  public string GetRegistryID() => PlotName;
}
