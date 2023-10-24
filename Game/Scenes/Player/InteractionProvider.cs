namespace Squiggles.CheeseFruitGroves.Player;

using Godot;
using Squiggles.Core.Interaction;
using System.Collections.Generic;
using System.Linq;

public partial class InteractionProvider : Node3D {

  [Signal] public delegate void OnInteractionChangeEventHandler(Node3D inter);

  [Export] private bool _autoSelectObjects = true;
  // [Export] private Node3D _debugMesh;
  [Export] private Node3D _positionDerive;
  [Export] private Area3D _interactionArea;
  [Export] private float _maxInteractionDistance;
  [Export] private int _raycastSampleRadius = 16;
  [Export(PropertyHint.Layers3DPhysics)] private uint _collisionLayerMask = 1;

  public IInteractable Current { get; private set; }

  public bool Active {
    get => IsPhysicsProcessing();
    set => SetPhysicsProcess(value);
  }

  // public override void _Ready() => _debugMesh.TopLevel = true;

  public override void _PhysicsProcess(double delta) {
    // try to get raycast object first

    var raycastObj = GetRayCastObject(Vector2.Zero);
    raycastObj ??= GetRayCastObject(new(_raycastSampleRadius, _raycastSampleRadius));
    raycastObj ??= GetRayCastObject(new(_raycastSampleRadius, -_raycastSampleRadius));
    raycastObj ??= GetRayCastObject(new(-_raycastSampleRadius, -_raycastSampleRadius));
    raycastObj ??= GetRayCastObject(new(-_raycastSampleRadius, _raycastSampleRadius));
    if (raycastObj is not null) {
      HandleObject(raycastObj);
    }
    else {
      HandleObject(GetAreaObject());
    }
    // fallback on area collision by distance
  }

  private void HandleObject(IInteractable foundObj) {
    if (foundObj != Current) {
      if (_autoSelectObjects) {
        (Current as ISelectable)?.OnDeselect();
        (foundObj as ISelectable)?.OnSelect();
      }
      Current = foundObj;
      EmitSignal(nameof(OnInteractionChange), Current as Node3D);
    }
  }

  private IInteractable GetRayCastObject(Vector2 offset) {
    var camera = GetViewport().GetCamera3D();
    var samplePoint = (GetViewport().GetVisibleRect().Size / 2f) + offset;
    var start = camera.ProjectRayOrigin(samplePoint);
    var end = start + (camera.ProjectRayNormal(samplePoint) * _maxInteractionDistance * 10f); // extra distance in case hitting the edge of the object
    var rayParams = new PhysicsRayQueryParameters3D {
      From = start,
      To = end,
      CollideWithAreas = true,
      CollideWithBodies = true,
      CollisionMask = _collisionLayerMask
    };
    var result = GetWorld3D().DirectSpaceState.IntersectRay(rayParams);
    var collider = (result?.ContainsKey("collider") ?? false) ? result["collider"].AsGodotObject() as Node3D : null;
    var position = (result?.ContainsKey("position") ?? false) ? result["position"].AsVector3() : Vector3.Up * 1000f;

    if (position.DistanceTo(_positionDerive.GlobalPosition) <= _maxInteractionDistance) {
      // _debugMesh.GlobalPosition = position;
      return collider as IInteractable;
    }
    // _debugMesh.GlobalPosition = _positionDerive.GlobalPosition + (Vector3.Up * 1.5f);
    return null;
  }

  private IInteractable GetAreaObject() {

    var composite = new List<IInteractable>();
    composite.AddRange(_interactionArea.GetOverlappingAreas().Where((n) => n is IInteractable).ToList().ConvertAll((n) => n as IInteractable));
    composite.AddRange(_interactionArea.GetOverlappingBodies().Where((n) => n is IInteractable).ToList().ConvertAll((n) => n as IInteractable));
    if (composite.Count <= 0) { return null; }
    var result = composite.MinBy((obj) => (obj as Node3D).GlobalPosition.DistanceTo(_positionDerive.GlobalPosition));
    return (result as Node3D).GlobalPosition.DistanceTo(_positionDerive.GlobalPosition) <= _maxInteractionDistance ? result : null;
  }
}
