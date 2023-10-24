namespace Squiggles.CheeseFruitGroves.Item;

using Godot;
using Squiggles.CheeseFruitGroves.Data;
using Squiggles.CheeseFruitGroves.Player;
using Squiggles.CheeseFruitGroves.Resource;
using Squiggles.Core.Data;
using Squiggles.Core.Error;
using Squiggles.Core.Extension;
using Squiggles.Core.Interaction;
using Squiggles.Core.Scenes.Registration;

public partial class WorldItem : RigidBody3D, IInteractable, ISelectable, IHasSaveData {


  [Export] private Item _item;
  [Export] private float _outlineMargin = 0.1f;

  private MeshInstance3D _itemMesh;
  private CollisionShape3D _collider;
  private MeshInstance3D _highlightMesh;


  public override void _Ready() {

    _itemMesh = GetNode<MeshInstance3D>("ItemMesh");
    _highlightMesh = GetNode<MeshInstance3D>("ItemOutline");
    _collider = GetNode<CollisionShape3D>("Collider");
    OnDeselect();
    RefreshVisuals();
  }

  public void SetItem(Item item) {
    _item = item;
    RefreshVisuals();
  }

  private void RefreshVisuals() {
    if (_item is null) { return; }
    _itemMesh.Mesh = _item.ItemMesh;
    _collider.Shape = _itemMesh.Mesh.CreateConvexShape();
    _highlightMesh.Mesh = _itemMesh.Mesh.CreateOutline(_outlineMargin);
  }

  public string GetActiveName() => _item?.ItemName ?? "";
  public bool GetIsActive() => true;
  public bool Interact() {
    if (GetTree()?.GetFirstNodeInGroup("player")?.GetComponent<PlayerInventory>() is PlayerInventory player) {
      player?.Inventory?.AddItem(_item.GetRegistryID());
      QueueFree();
      Print.Debug($"Picked up: {GetActiveName()}");
      return true;
    }
    return false;
  }
  public void OnDeselect() {
    if (!IsInstanceValid(_highlightMesh)) { return; }
    _highlightMesh.Visible = false;
  }
  public void OnSelect() {
    if (!IsInstanceValid(_highlightMesh)) { return; }
    _highlightMesh.Visible = true;
  }

  public void Serialize(SaveDataBuilder builder) {
    builder.PutBool("exists", true);
    builder.PutVector3(nameof(GlobalPosition), GlobalPosition);
    builder.PutVector3(nameof(GlobalRotation), GlobalRotation);
    builder.PutString("item", _item.GetRegistryID());
  }
  public void Deserialize(SaveDataBuilder builder) {
    if (builder.GetBool("exists", out var value) && !value) {
      QueueFree();
      return;
    }
    GlobalPosition = builder.GetVector3(nameof(GlobalPosition), out var gPos) ? gPos : GlobalPosition;
    GlobalRotation = builder.GetVector3(nameof(GlobalRotation), out var gRot) ? gRot : GlobalRotation;

    if (builder.GetString("item", out var itemID)) {
      var item = RegistrationManager.GetResource<Item>(itemID);
      if (item is not null) {
        _item = item;
        RefreshVisuals();
      }
    }
  }
}
