namespace Squiggles.CheeseFruitGroves.Resource;

using Godot;
using Squiggles.Core.Data;

[GlobalClass]
public partial class Item : Resource, IRegistryID {

  public enum Category {
    MISC, SPELL, FOOD, KEY, POTION, CRAFTING
  }

  [Export] public string ItemName = "";
  [Export] public Texture2D Icon;
  [Export] public Mesh ItemMesh;
  [Export] public int Value = 1;
  [Export] public float ValueVariance = 0;
  [Export] public int MaxStackSize = 999;
  [Export] public Category ItemCategory = Category.MISC;

  [Export(PropertyHint.MultilineText)] public string Description = "";

  public string GetRegistryID() => ItemName;
}
