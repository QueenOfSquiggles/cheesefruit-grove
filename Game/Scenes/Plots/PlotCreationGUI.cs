namespace Squiggles.CheeseFruitGroves.GUI;

using Godot;
using Squiggles.CheeseFruitGroves.Plot;
using Squiggles.Core.Scenes.Registration;

public partial class PlotCreationGUI : PanelContainer {

  [Signal] public delegate void OnBuildPlotTypeEventHandler(string type);

  [Export] private Control _buttonRoot;

  public override void _Ready() {
	var plotTypes = RegistrationManager.GetAllResourcesForType<PlotType>();
	foreach (var plot in plotTypes) {
	  var btn = new Button {
		Text = $"Create {plot.PlotName} (${plot.Cost})"
	  };
	  _buttonRoot.AddChild(btn);
	  btn.Pressed += () => {
		EmitSignal(SignalName.OnBuildPlotType, plot.GetRegistryID());
		QueueFree();
	  };
	}
  }
}
