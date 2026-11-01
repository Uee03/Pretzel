# Pretzel Editor

Standalone editor utilities that work in any project. No dependencies on other Pretzel modules, and no Pretzel runtime code. Editor only.

Editors that belong to one module live in that module instead: the Data Format window and the `SerializableGuid` drawer in `pretzel.data`, and the `UIFader` inspector in `pretzel.ui.ugui`.

## Menus

- `GameObject > Pretzel > Group Selected Objects`: parents the selection under a new empty object, with Undo.
- `Pretzel > Create > Prototype Sprites`: draws white placeholder sprites (Square, RoundedSquare, Circle, Triangle, Diamond, Hexagon) into `Assets/_Project/Art/Sprites/Prototype`, one world unit wide at 64 pixels per unit, with a 9-slice border on the rounded square. Tint them with the `SpriteRenderer` colour to block out a prototype without importing art. Existing files are never overwritten. `PrototypeSprites.Create(folder)` does the same from code.
