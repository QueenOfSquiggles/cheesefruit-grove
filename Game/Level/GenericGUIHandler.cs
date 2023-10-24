namespace Squiggles.CheeseFruitGroves.GUI;

using Godot;
using Squiggles.Core.Events;
using System;

public partial class GenericGUIHandler : Control {


  private Control _gui;

  public override void _Ready() {
    EventBus.GUI.RequestGUI += CreateGUI;
    EventBus.GUI.RequestCloseGUI += ClearGUI;
    Visible = false;
  }

  public override void _ExitTree() {
    EventBus.GUI.RequestGUI -= CreateGUI;
    EventBus.GUI.RequestCloseGUI -= ClearGUI;
  }

  public void CreateGUI(Control gui) {
    ClearGUI();
    AddChild(gui);
    _gui = gui;
    Visible = true;
    EventBus.Gameplay.TriggerRequestPlayerAbleToMove(false);
    EventBus.GUI.TriggerUnableToInteract();
    Input.MouseMode = Input.MouseModeEnum.Visible;
    _gui.TreeExiting += ClearGUI;
  }

  public void ClearGUI() {
    Input.MouseMode = Input.MouseModeEnum.Captured;
    EventBus.Gameplay.TriggerRequestPlayerAbleToMove(true);
    Visible = false;
    if (IsInstanceValid(_gui)) {
      _gui?.QueueFree();
    }
  }
}
