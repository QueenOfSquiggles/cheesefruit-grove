namespace Squiggles.CheeseFruitGroves.Item;

using System.Collections.Generic;
using System.Linq;
using Godot;
using Squiggles.CheeseFruitGroves.Data;
using Squiggles.Core.Data;

public class Inventory : IHasSaveData {


  private readonly Dictionary<string, int> _items = new();

  public IEnumerable<KeyValuePair<string, int>> Iterator => _items;

  public void AddItem(string id, int qty = 1) {
    if (HasItem(id)) {
      _items[id] += qty;
    }
    else {
      _items[id] = qty;
    }
    CleanData();
  }

  public void RemoveItem(string id, int qty = 1) {
    if (HasItem(id)) {
      _items[id] -= qty;
    }
    CleanData();
  }
  public bool HasItem(string id, int qty = 1) => _items.ContainsKey(id) && _items[id] >= qty;

  public bool ConsumeItem(string id, int qty = 1) {
    if (!HasItem(id, qty)) { return false; }
    RemoveItem(id, qty);
    return true;
  }

  private void CleanData() {
    var dead = _items.Where((entry) => entry.Value <= 0).ToList().ConvertAll((objIn) => objIn.Key);
    foreach (var d in dead) {
      _items.Remove(d);
    }
  }

  private const string EMBED_KEY = "__inventory";

  public void Serialize(SaveDataBuilder builder) {
    var selfBuilder = new SaveDataBuilder();
    foreach (var entry in _items) {
      if (entry.Value <= 0) { continue; }
      selfBuilder.PutInt(entry.Key, entry.Value);
    }
    builder.Append(selfBuilder, EMBED_KEY);
  }
  public void Deserialize(SaveDataBuilder builder) {
    if (builder.LoadEmbedded(EMBED_KEY) is not SaveDataBuilder selfBuilder) {
      return;
    }

    foreach (var key in selfBuilder.Keys) {
      if (selfBuilder.GetInt(key, out var qty) && qty > 0) {
        AddItem(key, qty);
      }
    }
  }

}
