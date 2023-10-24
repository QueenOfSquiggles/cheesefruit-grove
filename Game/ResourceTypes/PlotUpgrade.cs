namespace Squiggles.CheeseFruitGroves.Plot;

using Godot;
using Squiggles.Core.Data;
using System;

[GlobalClass, Icon("res://Game/Assets/UsefulEditorIcons/world_entity_resource.svg")]
public partial class PlotUpgrade : Resource, IRegistryID {

  [Export] public string UpgradeName = "";
  [Export] public int Cost = 100;
  [Export] public PackedScene UpgradeScene;


  public string GetRegistryID() => UpgradeName;
}
