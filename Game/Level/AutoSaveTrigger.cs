namespace Squiggles.CheeseFruitGroves.Data;

using Godot;
using Squiggles.Core.Data;
using Squiggles.Core.Events;
using Squiggles.Core.Extension;

public partial class AutoSaveTrigger : Node {

  public override void _Ready() {
    var timer = this.GetComponent<Timer>();
    var time = GameplaySettings.GetFloat("auto_save_interval");
    timer.Start(time <= 1.0f ? 60.0f : time);
  }

  public void DoSave() => EventBus.Data.TriggerSerializeAll();
}
