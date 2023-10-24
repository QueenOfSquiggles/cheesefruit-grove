namespace Squiggles.CheeseFruitGroves.Level;

using Godot;
using Squiggles.CheeseFruitGroves.Data;
using Squiggles.Core.Data;
using Squiggles.Core.Error;
using Squiggles.Core.Events;
using System.Linq;

public partial class MainTestingLevel : Node3D {

  private const string SAVE_FILE = "level.json";
  private const string EMBED_NODE_IDENTFIER = "node::";
  public override void _Ready() {
    EventBus.Data.SerializeAll += HandleLevelSerialize;
    EventBus.Data.Reload += HandleLevelDeserialize;
    HandleLevelDeserialize(); //load saved data
  }

  public override void _ExitTree() {
    EventBus.Data.SerializeAll -= HandleLevelSerialize;
    EventBus.Data.Reload -= HandleLevelDeserialize;
  }

  private void HandleLevelSerialize() {
    var data = new SaveDataBuilder(SAVE_FILE);
    foreach (var entry in GetTree().GetNodesInGroup("level_data").Where((n) => n is IHasSaveData)) {
      var embed = new SaveDataBuilder();
      (entry as IHasSaveData)?.Serialize(embed);
      if (entry.IsInGroup("instanced")) {
        embed.PutString("instance_scene", entry.SceneFilePath);
      }
      data.Append(embed, EMBED_NODE_IDENTFIER + entry.GetPath());
    }
    data.SaveToFile();
  }

  private void HandleLevelDeserialize() {
    var data = new SaveDataBuilder(SAVE_FILE).LoadFromFile();
    if (data is null) { return; } // no save data present
    foreach (var target in data.Keys.Where((k) => k.StartsWith(EMBED_NODE_IDENTFIER))) {
      var embedded = data.LoadEmbedded(target);
      var path = target.Replace(EMBED_NODE_IDENTFIER, "");

      if (embedded.GetString("instance_scene", out var sceneFilePath)) {
        // instanced data
        var scene = GD.Load<PackedScene>(sceneFilePath);
        var absPath = new NodePath(path);
        for (var i = absPath.GetNameCount() - 2; i >= 0; i--) { // minus two to clear array len & child node name
          // walking up the tree checking if the nodes exist
          var pathBuild = "";
          for (var j = 0; j < i; j++) { pathBuild += "/" + absPath.GetName(j); }
          if (GetNodeOrNull(pathBuild) is Node parent) {
            Print.Debug($"Level instancing object to: [{pathBuild}] from scene: [{sceneFilePath}]");
            var node = scene?.Instantiate();
            parent.AddChild(node);
            (node as IHasSaveData).Deserialize(embedded);
            if (!node.IsInGroup("instanced")) {
              node.AddToGroup("instanced"); // sometimes nodes don't have the group by default.
            }
            break;
          }
        }
      }
      else {
        // static data
        if (GetNodeOrNull(path) is Node node && node is IHasSaveData saveData) {
          saveData.Deserialize(embedded);
        }
      }
    }
  }

}
