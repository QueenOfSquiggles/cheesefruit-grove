namespace Squiggles.CheeseFruitGroves.GUI;

using Godot;
using Squiggles.CheeseFruitGroves.Player;
using Squiggles.CheeseFruitGroves.Resource;
using Squiggles.Core.Extension;
using Squiggles.Core.Scenes.Registration;
using System;
using System.Linq;

public partial class FarmPlotGUI : PanelContainer {
  [Signal] public delegate void OnPlantCropEventHandler(string item);
  [Signal] public delegate void OnAddUpgradeEventHandler(string upgrade);
  [Signal] public delegate void OnClearCropsEventHandler();
  [Signal] public delegate void OnClearPlotEventHandler();

  [Export] private Control _plantableCropsRoot;

  public override void _Ready() {
    var playerInventory = GetTree().GetFirstNodeInGroup("player")?.GetComponent<PlayerInventory>();
    if (playerInventory is null) {
      QueueFree();
      return;
    }
    var crops = playerInventory.Inventory.Iterator
      .Where((item) => RegistrationManager.GetResource<Item>(item.Key) is ItemCrop);
    foreach (var entry in crops) {
      var btn = new Button {
        Text = $"Plant {entry.Key} ({entry.Value} available)"
      };
      _plantableCropsRoot.AddChild(btn);
      btn.Pressed += () => {
        if (playerInventory.Inventory.ConsumeItem(entry.Key, 1)) {
          DoPlantCrop(entry.Key);
          QueueFree(); // closes GUI
        }
      };
    }
  }

  private void DoPlantCrop(string item) => EmitSignal(nameof(OnPlantCrop), item);
  private void DoAddUpgrade(string upgrade) => EmitSignal(nameof(OnAddUpgrade), upgrade);
  private void DoClearCrops() => EmitSignal(nameof(OnClearCrops));
  private void DoClearPlot() => EmitSignal(nameof(OnClearPlot));
}
