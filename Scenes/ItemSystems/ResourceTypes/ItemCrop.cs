using System;
using Godot;

[GlobalClass, Icon("res://Assets/UsefulEditorIcons/item_crop_resource.svg")]
public partial class ItemCrop : WorldEntity
{

    [Export] public int FinalGrowthStage;
    [Export] public int OnHarvestReturnStage;
    [Export] public Mesh[] GrowthStages;
    [Export] public float[] GrowthStageDuration;


}
