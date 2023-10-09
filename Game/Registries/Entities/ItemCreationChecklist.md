# Debugging Checklist
-[ ] RigidBody3D is on collision layer 2 ("items_gravity")
-[ ] RididBody3D has a child `WorldItemComponent`
-[ ] Item IDs match in `WorldItemComponent` and Item resource file
-[ ] Item Resource file is stored in "res://Game/Registries/Entities/" or a subfolder of such. (folders with an underscore prefix are skipped)

# How to create an item
This is a little guide for asset creation, specifically in the MarkDown format so it can easily be read in the Godot editor or on GitHub.

## Item Resource
1. Create a new resource of type `WorldEntity`
2. Provide the `ID`, `CanCollect`, and `InventoryIcon` values
3. We will add the `WorldScene` later.

## Item Scene
1. Create a 3D scene
2. Ensure there is a `RigidBody3D` which is on the collision layer `items_gravity` (layer 2).
3. Add a `WorldItemComponent` as a child of the `RigidBody3D`
Path for such scene is "res://Scenes/ItemSystems/Components/world_item_component.tscn"
4. Set the `ItemID` property on the `WorldItemComponent` to the item ID that you set for the item resource file.
5. Add the `RigidBody3D` to the group "item"
6. Go into the Item Resource file you made and add the scene you created to the `WorldScene` property.

**Now your item should be able to be collected and stored in the game.**
