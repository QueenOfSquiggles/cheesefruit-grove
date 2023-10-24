namespace Squiggles.CheeseFruitGroves.Plot;

using Godot;
using Squiggles.CheeseFruitGroves.Item;
using Squiggles.CheeseFruitGroves.Resource;
using Squiggles.Core.Extension;
using System;

public partial class PlotItemSpawner : Node3D {

  [Export] private Item _item;
  [Export] private PackedScene _worldItem;
  [Export] private Marker3D _spawnLocation;
  [Export] private float _spawnForce = 10.0f;
  [Export] private float _spawnAngle = 45.0f;
  private readonly Random _rand = new();

  public void SpawnItem() {
	var node = _worldItem.Instantiate() as WorldItem;
	AddChild(node);
	node.SetItem(_item);
	node.AddToGroup("instanced"); // marks as an instance object. Use for all objects that need to be re-instanced on loading
	node.GlobalPosition = _spawnLocation.GlobalPosition;
	var angleRad = Mathf.DegToRad(_spawnAngle);
	var direction = new Vector3(_rand.NextGuass() * angleRad, 1, _rand.NextGuass() * angleRad);
	node.ApplyCentralImpulse(direction.Normalized() * _spawnForce);
  }

}
