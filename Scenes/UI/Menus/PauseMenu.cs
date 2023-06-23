using System;
using System.Threading.Tasks;
using Godot;
using queen.extension;

public partial class PauseMenu : Control
{

    [Export(PropertyHint.File, "*.tscn")] private string main_menu_file;
    [Export(PropertyHint.File, "*.tscn")] private string options_menu_file;

    [Export] private NodePath PathSlidingSceneRoot = "SlidePanelRoot";

    private Control CurrentPopup = null;
    private Control SlidingSceneRoot;

    public override void _Ready()
    {
        GetTree().Paused = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        this.GetSafe(PathSlidingSceneRoot, out SlidingSceneRoot);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed("ui_cancel"))
        {
            ReturnToPlay();
            GetViewport().SetInputAsHandled();
        }
    }

    private void ReturnToPlay()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        GetTree().Paused = false;
        QueueFree();
    }

    private void ExitToMainMenu()
    {
        GetTree().Paused = false;
        Scenes.LoadSceneAsync(main_menu_file);
    }

    private void OnBtnOptions()
    {
        if (CurrentPopup is null || !IsInstanceValid(CurrentPopup))
        {
            CreateNewSlidingScene(options_menu_file);
        }
        else
        {
            CurrentPopup?.GetComponent<SlidingPanelComponent>()?.RemoveScene();
        }

    }

    private void CreateNewSlidingScene(string scene_file)
    {
        var packed = GD.Load<PackedScene>(scene_file);
        var scene = packed?.Instantiate<Control>();
        if (scene is null) return;
        scene.GlobalPosition = new Vector2(Size.X, 0);
        SlidingSceneRoot.AddChild(scene);
        CurrentPopup = scene;
    }
}
