using System;
using Godot;
using queen.extension;

public partial class MarketStructure : Node3D
{
    [Export] private NodePath PathItemStorage;
    [Export] private NodePath PathValueLabel;

    private ItemStorage _Item;
    private Label3D _ValueLabel;
    private int _CurrentMarketValue = 0;
    private Random _Rand = new();

    private const string _VALUE_FORMAT = "N0";

    public override void _Ready()
    {
        this.GetSafe(PathItemStorage, out _Item);
        this.GetSafe(PathValueLabel, out _ValueLabel);
        _ValueLabel.Text = "";

    }
    private void OnInteract()
    {
        if (_CurrentMarketValue <= 0) return;
        var stat = (GetTree().GetFirstNodeInGroup("player") as FarmingController).GetComponent<CharStatManager>();
        if (stat is null) return;

        stat.ModifyStaticStat("money", _CurrentMarketValue);
        _Item.SetCurrentItem("", 0);
    }

    private void OnItemChange(string item, int qty)
    {
        if (item == "" || qty <= 0) _CurrentMarketValue = 0;
        else _CurrentMarketValue = GetMarketValue(item) * qty;

        _ValueLabel.Text = _CurrentMarketValue.ToString(_VALUE_FORMAT);
        if (_CurrentMarketValue == 0) _ValueLabel.Text = "";
    }

    private int GetMarketValue(string id)
    {
        var we = RegistrationManager.GetResource<WorldEntity>(id);
        if (we is null) return 0;
        _Rand = new(((int)id.Hash()) + GetDateHash());
        float offset = (_Rand.NextSingle() - 0.5f) * 2.0f * we.MarketValueRange;
        int n_val = Mathf.RoundToInt(we.MarketValue + offset);

        return Mathf.Max(n_val, 1);
    }

    private static int GetDateHash()
    {
        var d = DateTime.Today;
        return d.DayOfYear + d.Year + d.Month;
    }


}
