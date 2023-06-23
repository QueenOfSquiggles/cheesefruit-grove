namespace menus;

using System;
using System.Threading.Tasks;
using Godot;
using queen.error;
using queen.extension;

public partial class MainMenu : Control
{
    [Export(PropertyHint.File, "*.tscn")] private string play_scene;
    [Export(PropertyHint.File, "*.tscn")] private string options_scene;
    [Export(PropertyHint.File, "*.tscn")] private string credits_scene;

    [ExportGroup("Node Paths")]
    [Export] private NodePath PathButtonsControlPanel;

    private Control ButtonsPanel;

    private Node? CurrentPopup = null;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        this.GetSafe(PathButtonsControlPanel, out ButtonsPanel);
    }
    private void OnBtnPlay()
    {
        Scenes.LoadSceneAsync(play_scene);
    }

    private async void OnBtnOptions()
    {
        if (CurrentPopup is OptionsMenu) return;
        await ClearOldSlidingScene();
        CreateNewSlidingScene(options_scene);
    }

    private async void OnBtnCredits()
    {
        if (CurrentPopup is CreditsScene) return;
        await ClearOldSlidingScene();
        CreateNewSlidingScene(credits_scene);
    }

    private async Task ClearOldSlidingScene()
    {
        if (CurrentPopup is null || !IsInstanceValid(CurrentPopup)) return;
        var sliding_comp = CurrentPopup.GetComponent<SlidingPanelComponent>();
        if (sliding_comp is not null)
        {
            sliding_comp.RemoveScene();
            await ToSignal(CurrentPopup, "tree_exited");
        }
    }

    private void CreateNewSlidingScene(string scene_file)
    {
        var packed = GD.Load<PackedScene>(scene_file);
        var scene = packed?.Instantiate<Control>();
        if (scene is null) return;
        scene.GlobalPosition = new Vector2(ButtonsPanel.Size.X, 0);
        AddChild(scene);
        CurrentPopup = scene;
    }

    private void OnBtnQuit()
    {
        Print.Debug("Quitting game");
        GetTree().Quit();
    }


}
