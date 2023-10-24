namespace Squiggles.CheeseFruitGroves.Plot;

using Godot;
using Squiggles.CheeseFruitGroves.Data;
using Squiggles.CheeseFruitGroves.GUI;
using Squiggles.Core.Data;
using Squiggles.Core.Events;
using Squiggles.Core.Extension;
using Squiggles.Core.Scenes.Interactables;
using Squiggles.Core.Scenes.Registration;
using System;
using System.Linq;

public partial class Plot : Node3D, IHasSaveData {

  [Export] private PackedScene _plotGUI;

  private PlotType _type;
  private PlotUpgrade[] _upgrades = Array.Empty<PlotUpgrade>();
  private Node _plotTypeNode;
  private InteractiveTrigger _interactions;
  private bool _isGUIOpen;

  public override void _Ready() {
    _interactions = this.GetComponent<InteractiveTrigger>();
    _interactions.CustomName = "Empty Plot";
  }


  public void OnInteract() {
    if (_isGUIOpen) { return; }

    Control gui;
    if (_plotTypeNode is IPlotTypeHandler handler) {
      gui = handler.InstanceGUI();

    }
    else {
      gui = _plotGUI.Instantiate<Control>();
      (gui as PlotCreationGUI).OnBuildPlotType +=
      (typeName) => {
        _type = RegistrationManager.GetResource<PlotType>(typeName);
        UpdateValues();
      };
    }

    EventBus.GUI.TriggerRequestGUI(gui);
    _isGUIOpen = true;
    gui.TreeExiting += () => _isGUIOpen = false;
  }

  private void UpdateValues() {
    _plotTypeNode?.QueueFree();
    if (_type is null) { return; }
    var node = _type.PlotScene.Instantiate();
    AddChild(node);
    _plotTypeNode = node;
    _interactions.CustomName = _type.PlotName;
  }

  public void Serialize(SaveDataBuilder builder) {
    builder.PutString("type", _type?.GetRegistryID() ?? "null");
    builder.PutVariant("upgrades", _upgrades.ToList().ConvertAll((u) => u.GetRegistryID()).ToArray());
    if (_plotTypeNode is IHasSaveData plotTypeData) {
      var embed = new SaveDataBuilder();
      plotTypeData.Serialize(embed);
      builder.Append(embed, "plot_data");
    }
  }

  public void Deserialize(SaveDataBuilder builder) {
    if (builder is null) { return; }
    if (builder.GetString("type", out var typeName)) {
      _type = RegistrationManager.GetResource<PlotType>(typeName);
      UpdateValues();
    }
    if (_plotTypeNode is IHasSaveData plotTypeData) {
      var embed = builder.LoadEmbedded("plot_data");
      plotTypeData.Deserialize(embed);
    }
  }

  public override void _UnhandledInput(InputEvent @event) {
    if (@event.IsActionPressed("ui_cancel") && _isGUIOpen) {
      EventBus.GUI.TriggerRequestCloseGUI();
      _isGUIOpen = false;
      this.HandleInput();
    }
  }
}
