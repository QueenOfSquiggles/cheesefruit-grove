namespace Squiggles.CheeseFruitGroves.Plot;

using System;
using Godot;
using Squiggles.CheeseFruitGroves.Data;
using Squiggles.CheeseFruitGroves.GUI;
using Squiggles.CheeseFruitGroves.Item;
using Squiggles.CheeseFruitGroves.Resource;
using Squiggles.Core.CharStats;
using Squiggles.Core.Data;
using Squiggles.Core.Extension;
using Squiggles.Core.Scenes.Registration;

public partial class FarmPlot : Node3D, IPlotTypeHandler, IHasSaveData {

  [Export] private CharStatManager _stats;
  [Export] private PackedScene _guiScene;
  [Export] private MeshInstance3D[] _cropMeshes = Array.Empty<MeshInstance3D>();
  [Export] private float _cropSpawnAngle = 15f;
  [Export] private float _cropSpawnForce = 1f;
  [Export] private float _cropSpawnHeight = .2f;
  [Export] private GpuParticles3D _growthParticles;
  private ItemCrop _crop;
  private int _cropStage;
  private float _cropStageRemainder;
  private readonly Random _rand = new();
  private float _angle;


  public override void _Ready() {
    _angle = Mathf.DegToRad(_cropSpawnAngle);
    SetProcess(false);
    ClearMeshes();

  }

  private void ClearMeshes() {
    foreach (var mesh in _cropMeshes) {
      mesh.Mesh = null;
    }
  }

  private void PlantCrop(int stage = 0) {
    _growthParticles.Emitting = true;
    if (stage >= _crop.CropStageMeshes.Length) {
      foreach (var mesh in _cropMeshes) {
        if (mesh.Mesh is null) { continue; }
        // only spawn for spawned crops
        SpawnYield(mesh.GlobalTransform);
      }
      ClearMeshes();
      _crop = null;
      _cropStage = 0;
      _cropStageRemainder = -1;
      SetProcess(false);
    }
    else {
      _cropStage = stage;
      _cropStageRemainder = _crop.CropStageDurations[stage];
      SetProcess(true);
      UpdateVisuals();
    }
  }

  private void SpawnYield(Transform3D globTransform) {
    var yield = Mathf.Max(Mathf.CeilToInt(_crop.BaseYield * _stats.GetStat("YieldFactor")), 1);
    for (var i = 0; i < yield; i++) {
      var worldItem = GD.Load<PackedScene>("res://Game/Scenes/Items/world_item.tscn")?.Instantiate() as WorldItem;
      AddChild(worldItem);
      worldItem.SetItem(_crop);
      worldItem.GlobalTransform = globTransform;
      worldItem.GlobalPosition += Vector3.Up * _cropSpawnHeight;
      worldItem.ApplyImpulse(new Vector3(_rand.NextGuass() * _angle, 1, _rand.NextGuass() * _angle).Normalized() * _cropSpawnForce);
    }

  }

  private void UpdateVisuals() {
    ClearMeshes();
    if (_crop is not null && _cropStage < _crop.CropStageMeshes.Length && _cropStage >= 0) {
      for (var i = 0; i < (int)_stats.GetStat("CropCount"); i++) {
        _cropMeshes[i].Mesh = _crop?.CropStageMeshes[_cropStage];
      }
    }
  }

  public override void _Process(double delta) {
    _cropStageRemainder -= _stats.GetStat("GrowthFactor") * (float)delta;
    if (_cropStageRemainder <= 0) {
      PlantCrop(_cropStage + 1);
    }
  }

  public Control InstanceGUI() {
    var scene = _guiScene.Instantiate<Control>() as FarmPlotGUI;
    // hook into signals
    scene.OnPlantCrop += (item) => {
      _crop = RegistrationManager.GetResource<Item>(item) as ItemCrop;
      if (_crop is null) {
        ClearMeshes();
        return;
      }
      ClearMeshes();
      PlantCrop();
    };
    // return scene
    return scene;
  }

  public void Serialize(SaveDataBuilder builder) {
    builder.PutString("crop", _crop?.GetRegistryID() ?? "null");
    builder.PutInt("stage", _cropStage);
    builder.PutFloat("remainder", _cropStageRemainder);
    _stats.Serialize(builder);
  }
  public void Deserialize(SaveDataBuilder builder) {

    if (builder.GetString("crop", out var item) && item != "null") {
      _crop = RegistrationManager.GetResource<Item>(item) as ItemCrop;
    }
    _cropStage = builder.GetInt("stage", out var stage) ? stage : _cropStage;
    _cropStageRemainder = builder.GetFloat("remainder", out var rem) ? rem : _cropStageRemainder;
    UpdateVisuals();
    SetProcess(_cropStageRemainder > 0);
    _stats.Deserialize(builder);
  }
}
