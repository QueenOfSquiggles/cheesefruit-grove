namespace Squiggles.CheeseFruitGroves.Player;

using Godot;
using Squiggles.CheeseFruitGroves.Data;
using Squiggles.CheeseFruitGroves.Item;
using Squiggles.Core.Data;
using System;

public partial class PlayerInventory : Node, IHasSaveData {

  public Inventory Inventory { get; } = new();

  public void Serialize(SaveDataBuilder builder) => Inventory.Serialize(builder);
  public void Deserialize(SaveDataBuilder builder) => Inventory.Deserialize(builder);
}
