namespace Squiggles.CheeseFruitGroves.Resource;

using Godot;

[GlobalClass]
public partial class ItemCrop : Item {

  [ExportCategory("Crop Data")]
  [Export] public int BaseYield = 1;
  [Export] public float[] CropStageDurations;
  [Export] public Mesh[] CropStageMeshes;

}
