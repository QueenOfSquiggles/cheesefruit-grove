using System;
using Godot;
using queen.error;
using queen.extension;

public partial class ItemStoragePillar : Node3D
{
    [Signal] public delegate void OnStorageChangeEventHandler(string itemID, int quantity);
    [Signal] public delegate void OnStorageEmptiedEventHandler();
    [Signal] public delegate void OnStorageFullEventHandler(string itemID, int quantity);

    [Export] private int QuantityCap = -1; // -1 for no cap (on god)
    [Export] private bool AllowDropping = true;
    [Export] private bool AllowInfiniteDrop = false;
    [Export] private bool ShowQuantityDisplay = true;
    [ExportGroup("Item IO Settings")]
    [Export] private float ItemDropPhysicalOffset = 0.75f;
    [ExportSubgroup("Drop Rate", "DropRate")]
    [Export] private float DropRateMaxTime = 1.5f;
    [Export] private float DropRateCurveDuration = 10.0f;
    [Export] private Curve DropRateCurve;
    [ExportSubgroup("Item Collector", "CollectorOption")]
    [Export] private string[] CollectorOptionItemFilter = Array.Empty<string>();
    [Export] private string[] CollectorOptionGroupFilter = Array.Empty<string>();
    [Export] private string SuctionAreaGroupName = "suction_area";


    [ExportGroup("Node Paths")]
    [Export] private NodePath PathItemCollector;
    [Export] private NodePath PathItemTextureDisplay;
    [Export] private NodePath PathItemQuantityDisplay;
    [ExportGroup("Debugging")]
    [ExportSubgroup("Auto Add Item", "AutoAddItem")]
    [Export] private bool AutoAddItemEnabled = false;
    [Export] private string AutoAddItemID = "";
    [Export] private int AutoAddItemQuantity = 1;

    private ItemCollector _Collector;
    private string _CurrentItem = "";
    private int _Quantity = 0;
    private float _DropTimer = 0.0f;
    private bool _IsDropping = false;
    private float _DroppingDuration = 0.0f;

    private Area3D _SuctionAreaRef;
    private Sprite3D _ItemSprite;
    private Label3D _ItemLabel;

    public override void _Ready()
    {
        this.GetSafe(PathItemCollector, out _Collector);
        this.GetSafe(PathItemTextureDisplay, out _ItemSprite);
        this.GetSafe(PathItemQuantityDisplay, out _ItemLabel);

        _Collector.OnItemPickup += OnItemPickup;
        if (!CollectorOptionItemFilter.IsEmpty()) _Collector.IDFilters = CollectorOptionItemFilter;
        if (!CollectorOptionGroupFilter.IsEmpty()) _Collector.GroupFilters = CollectorOptionGroupFilter;

        _ItemSprite.Texture = null;
        _ItemLabel.Text = "";
        _ItemLabel.Visible = ShowQuantityDisplay;

        if (AutoAddItemEnabled) SetCurrentItem(AutoAddItemID, AutoAddItemQuantity);
    }

    public override void _Process(double d)
    {
        if (_SuctionAreaRef is null) return;
        _IsDropping = _SuctionAreaRef.Gravity > 0.5f;
        if (!_IsDropping) return;
        _DropTimer -= (float)d;
        _DroppingDuration += (float)d;
        if (_DropTimer > 0) return;
        DropItem();
        _DropTimer = GetDropTimer();
    }


    public void SetCurrentItem(string item, int qty)
    {
        _CurrentItem = item;
        if (_CurrentItem != "") _Collector.IDFilters = new string[] { item }; // filter only same ID
        else EmitSignal(nameof(OnStorageEmptied));
        _Quantity = qty;
        _ItemLabel.Text = GetQuantityDisplay();
        var we = RegistrationManager.GetResource<WorldEntity>(_CurrentItem);
        _ItemSprite.Texture = we?.InventoryIcon as Texture2D;

        EmitSignal(nameof(OnStorageChange), _CurrentItem, _Quantity);
    }

    public void DropItem()
    {
        if (!AllowDropping) return;
        if (_Quantity <= 0) return;
        var item = RegistrationManager.GetResource<WorldEntity>(_CurrentItem);
        if (item is null) return;

        if (!AllowInfiniteDrop)
        {
            _Quantity -= 1;
            EmitSignal(nameof(OnStorageChange), _CurrentItem, _Quantity);
        }
        _ItemLabel.Text = GetQuantityDisplay();
        var node = item.WorldScene?.Instantiate() as Node3D;
        var offset = (_SuctionAreaRef.GlobalPosition - _Collector.GlobalPosition).Normalized();
        GetParent().AddChild(node);
        node.GlobalPosition = _Collector.GlobalPosition + offset * ItemDropPhysicalOffset;


        if (_Quantity <= 0)
        {
            if (_Quantity <= 0) EmitSignal(nameof(OnStorageEmptied));
            _CurrentItem = "";
            _ItemSprite.Texture = null;
            _ItemLabel.Text = GetQuantityDisplay();
            _Collector.IDFilters = Array.Empty<string>(); // filter only same ID
        }
    }

    private void OnItemPickup(Node3D nodeRef)
    {
        if (_IsDropping) return;// Don't pick up items when actively dropping
        if (QuantityCap > 0 && _Quantity >= QuantityCap) return;
        if (_CurrentItem == "")
        {
            // set new item
            var comp = nodeRef.GetComponent<WorldItemComponent>();
            if (comp is null) return;
            SetCurrentItem(comp.ItemID, 1);
        }
        else
        {
            // add to existing
            _Quantity++;
            EmitSignal(nameof(OnStorageChange), _CurrentItem, _Quantity);
            if (_Quantity >= QuantityCap) EmitSignal(nameof(OnStorageFull), _CurrentItem, _Quantity);

            _ItemLabel.Text = GetQuantityDisplay();
        }
        nodeRef?.QueueFree();
    }
    private void OnAreaEnter(Area3D area)
    {
        if (area is null || !area.IsInGroup(SuctionAreaGroupName)) return;
        _IsDropping = AllowDropping;
        _DroppingDuration = 0.0f;
        _DropTimer = 0.0f; // instant reaction
        _SuctionAreaRef = area;
    }

    private void OnAreaExit(Area3D area)
    {
        if (area is null || !area.IsInGroup(SuctionAreaGroupName)) return;
        _IsDropping = false;
        _SuctionAreaRef = null;
    }

    private string GetQuantityDisplay()
    {
        if (_Quantity <= 1) return "";
        return $"{_Quantity}";
    }

    private float GetDropTimer()
    {
        float f = Mathf.Clamp(_DroppingDuration / DropRateCurveDuration, 0.0f, 1.0f);
        return DropRateCurve.SampleBaked(f) * DropRateMaxTime;
    }

}
