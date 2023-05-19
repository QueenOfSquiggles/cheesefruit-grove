using System;
using System.Collections.Generic;
using Godot;
using queen.error;
using queen.extension;

public partial class RegistrationManager : Node
{

    public static RegistrationManager Instance = null;

    // TODO create a unique dictionary for each registered type
    public Dictionary<string, Plot> Plots { get; private set; } = new();

    private const string REGISTRY_PATH_PLOTS = "res://Game/Registries/Plots/";

    public override void _Ready()
    {
        if (!this.EnsureSingleton(ref Instance)) return;
        Print.Debug("[Registration Manager] - Begin registration");
        Plots = LoadRegistry<Plot>(REGISTRY_PATH_PLOTS, "Plot");
        Print.Debug("[Registration Manager] - End registration");
    }

    private Dictionary<string, T> LoadRegistry<T>(string root_dir, string label = "unlabeled") where T : Resource
    {
        var registry = new Dictionary<string, T>();
        var dir = DirAccess.Open(root_dir);
        if (dir is null)
        {
            Print.Debug($"Failed to find root path for resource [{label}], expected path : {root_dir}");
            return registry; // empty registry
        }
        var files = GetAllFilesRecursive(dir);

        foreach (string f in files)
        {
            var temp = GD.Load<T>(f);
            if (temp is not null)
            {
                Print.Debug($"Found registry resource [{label}] : {f}");
                registry.Add(f.GetFile(), temp);
            }
            else
            {
                Print.Debug($"file '{f}' is not valid for type [{label}]");
            }
        }
        Print.Debug($"Total number of [{label}] resources found: {registry.Count}");
        return registry;
    }

    private List<string> GetAllFilesRecursive(DirAccess dir)
    {
        var files = new List<string>();
        foreach (var f in dir.GetFiles())
        {
            files.Add(dir.GetCurrentDir().PathJoin(f));
        }

        foreach (var d in dir.GetDirectories())
        {
            var subdir = DirAccess.Open(dir.GetCurrentDir().PathJoin(d));
            if (subdir is null) continue;
            files.AddRange(GetAllFilesRecursive(subdir));
        }
        return files;
    }

}
