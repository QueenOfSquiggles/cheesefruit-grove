using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using queen.data;
using queen.error;
using queen.events;
using queen.extension;

public partial class FarmPlot : PlotInterface
{

    [Export] private NodePath PathCropModelRoot;
    [Export] private NodePath PathModifyPopup;
    [Export] private float HarvestVerticalOffset = 0.5f;
    [Export] private NodePath PathItemStorage;
    private ItemCrop _Crop;
    private int _Stage;
    private float _StageTimer;

    private Node3D _CropRoot;
    private PopupMenu _ModifyPopup;
    private ItemStorage _ItemStorage;

    public override void _Ready()
    {
        this.GetSafe(PathCropModelRoot, out _CropRoot);
        this.GetSafe(PathModifyPopup, out _ModifyPopup);
        this.GetSafe(PathItemStorage, out _ItemStorage);
    }

    private void OnSetItem(string item, int _)
    {
        var temp_crop = RegistrationManager.GetResource<ItemCrop>(item);
        if (temp_crop is null) return;

        _Crop = temp_crop;
        _Stage = 0;
        _StageTimer = _Crop.GrowthStageDuration[_Stage];
        UpdateDisplayModels();
        var rand = new Random();
        DoForEachPlant((plant) => plant.RotateY(rand.NextSingle() * 360.0f));
    }

    private void RemoveItem()
    {
        // harvest if removing crop while at harvestable level
        if (_Crop is not null && _Stage == _Crop.GrowthStages.Length - 1) Harvest();

        // null crop removes everything
        _Crop = null;
        _Stage = 0;
        UpdateDisplayModels();
    }

    public override void _Process(double delta)
    {
        if (_Crop is null) return;
        _StageTimer -= (float)delta;
        if (_StageTimer < 0)
        {
            ProgressNextStage(_Stage + 1);
        }
    }

    private void ProgressNextStage(int stage)
    {
        if (stage >= _Crop.GrowthStages.Length)
        {
            Harvest();
        }
        else { _Stage = stage; }
        UpdateDisplayModels();
        _StageTimer = _Crop.GrowthStageDuration[_Stage];
    }

    private void UpdateDisplayModels()
    {
        Mesh crop_mesh = _Crop?.GrowthStages[_Stage];
        DoForEachPlant((plant) => plant.Mesh = crop_mesh);
    }

    private void Harvest()
    {
        var world_item = _Crop?.WorldScene;
        if (world_item is null) return;
        DoForEachPlant((plant) =>
        {
            _Stage = _Crop.OnHarvestReturnStage;
            var inst = world_item.Instantiate() as Node3D;
            GetParent().AddChild(inst);
            inst.GlobalTransform = plant.GlobalTransform;
            inst.GlobalPosition += new Vector3(0.0f, HarvestVerticalOffset, 0.0f);
        });
    }

    private delegate void PlantStepper(MeshInstance3D plant_mesh);
    private void DoForEachPlant(PlantStepper step)
    {
        // OMG these delegate systems are so helpful for making my code readable. Gonna have to do some refactor with this sometime.
        var instances = _CropRoot.GetChildren().Where((c) => c is MeshInstance3D).ToList();
        foreach (var m in instances)
        {
            if (m is MeshInstance3D mi) step(mi);
        }
    }

    public override void CreateModifyPrompt()
    {
        _ModifyPopup.PopupCentered();
        Events.Gameplay.TriggerRequestPlayerAbleToMove(false);
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    private void HandlePopupSelection(int index)
    {
        switch (index)
        {
            case 0: // Remove Crops
                _ItemStorage.SetCurrentItem("", 0);
                _Crop = null;
                UpdateDisplayModels();
                break;
            case 1: // Harvest
                if (_Stage == _Crop.GrowthStages.Length - 1)
                {
                    Harvest();
                    UpdateDisplayModels();
                    _StageTimer = _Crop.GrowthStageDuration[_Stage];
                }
                break;
            default:
                // anything needs to be here??
                break;
        }
        Events.Gameplay.TriggerRequestPlayerAbleToMove(true);
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void SavePlotData(ref SaveDataBuilder data)
    {
        if (data is null) return;
        data.PutString("_Crop", _Crop is null ? "" : _Crop.ID);
        data.PutInt("_Stage", _Stage);
        data.PutFloat("_StageTimer", _StageTimer);
    }
    public override void LoadPlotData(SaveDataBuilder data)
    {
        if (data.GetString("_Crop", out string crop) && crop != "") _ItemStorage.SetCurrentItem(crop, 1);
        if (data.GetInt("_Stage", out int stage)) _Stage = stage;
        if (data.GetFloat("_StageTimer", out float timer)) _StageTimer = timer;
        UpdateDisplayModels();
    }


}
