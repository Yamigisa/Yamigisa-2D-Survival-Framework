# Yamigisa 2D Survival Framework

A Unity 6 framework for building top-down 2D survival games. The project includes a playable demo scene, procedural chunk-based world generation, survival attributes, inventory, crafting, equipment, placeables, destroyable resources, animals, save/load support, and editor tools for creating new game content.

## Features

- Top-down 2D character controller with keyboard and gamepad-ready bindings
- Procedural chunk streaming with biome zones and seeded chunk data
- Inventory system with stackable items, hotbar selection, item actions, dropping, splitting, and using items
- Crafting system driven by `ScriptableObject` item recipes and craft groups
- Placeable objects, including storage, crafting placeables, and beds
- Equipment system with weapon, armor, accessory, and bag slots
- Survival attributes such as health, hunger, and thirst
- Destroyable resources with required tool groups, loot drops, and optional regrowth stages
- Animal data and prefabs for passive/aggressive survival encounters
- Save/load system for player state, inventory, equipment, world time, chunks, destroyables, interactive objects, and storages
- Pause/death UI flow with save, load, resume, and quit buttons
- Unity editor tools for creating items, destroyables, animals, placeables, biomes, characters, scenes, and required scene objects
- Bundled demo scene and pixel-art assets for prototyping

## Requirements

- Unity `6000.0.34f1` or newer Unity 6 version
- Universal Render Pipeline 2D
- Unity Input System
- UGUI

The package dependencies are tracked in `Packages/manifest.json`.

## Getting Started

1. Clone or download this repository.
2. Open the project folder in Unity Hub.
3. Use Unity `6000.0.34f1` when prompted, or install that editor version.
4. Open the demo scene:

   ```text
   Assets/YamigisaEngine/Scenes/Demo.unity
   ```

5. Press Play.

If you create a new scene, use the built-in setup tools:

1. Open `Tools > Yamigisa Engine > Create New Scene`.
2. Open `Tools > Yamigisa Engine > Scene Initializer`.
3. Assign or auto-find the initializer settings.
4. Click `Initialize Scene` to add required framework objects.

## Default Controls

| Action | Keyboard / Mouse |
| --- | --- |
| Move | WASD |
| Sprint | Left Shift |
| Jump | Space |
| Crouch | Left Control |
| Primary interaction | Left Mouse Button |
| Secondary interaction | F |
| Inventory | I |
| Use item | E |
| Crafting | C |
| Cancel / pause | Escape |
| Save | F5 |
| Load | F9 |

Gamepad bindings are also defined in `CharacterControls` when the Input System is enabled.

## Editor Tools

The framework adds tools under:

```text
Tools > Yamigisa Engine
```

Available workflows include:

- Create Object
- Duplicate Object
- Replace Prefab In Scene
- Create New Scene
- Scene Initializer
- Delete Save

The `Create Object` window can generate prefabs and data assets for:

- Items
- Destroyables
- Animals
- Placeables
- Biomes
- Characters

Most runtime content is data-driven through `ScriptableObject` assets stored under `Assets/YamigisaEngine/Resources`.

## Project Structure

```text
Assets/YamigisaEngine/
  Animation/          Character and animal animation assets
  Art/                Pixel-art characters, animals, enemies, objects, UI, and tiles
  Documentation/      PDF documentation for the level editor/workflow
  External Packages/  Bundled third-party art packages used by the demo
  Prefabs/            Runtime prefabs for managers, player, UI, items, placeables, animals, etc.
  Resources/          Data assets loaded at runtime
  Scenes/             Demo scene and scene assets
  Scripts/            Framework source code
```

Important script areas:

```text
Scripts/Actions      Item and interaction action implementations
Scripts/Character    Player, combat, biome, attributes, movement, controls, and world chunks
Scripts/Data         Save/load contracts and save manager
Scripts/Editor       Yamigisa editor tooling
Scripts/Gameplay     Managers, crafting, world generation, placeables, storage, beds, animals, biomes
Scripts/Inventory    Inventory and item slot logic
Scripts/Item         Item data, item database, and item behavior
Scripts/Patterns     Shared reusable gameplay patterns
Scripts/UI           Runtime UI components
```

## Data-Driven Content

Items are defined with `ItemData` assets. Each item can define:

- Item type: resource, equipment, consumable, or placeable
- World and inventory icons
- Stack and drop behavior
- Item actions
- Crafting requirements and result amount
- Consumable effects
- Equipment stat modifiers
- Resource regrowth stages
- Associated world prefab

Crafting groups are loaded from `Resources/Groups`, while craftable items are loaded from `Resources/Items`. This makes it easy to add new resources, tools, consumables, placeables, and equipment without rewriting gameplay code.

## Save System

Save data is written to:

```text
Application.persistentDataPath/save.json
```

The save manager supports:

- Manual save with `F5`
- Manual load with `F9`
- Auto-save interval
- Save on quit
- Save on sleep
- Per-system save toggles for world time, player, inventory, equipment, chunks, storages, destroyables, and interactive objects

Any runtime component can participate in saving by implementing `ISavable`.

## Demo Content

The project includes sample data for:

- Forest and meadow biomes
- Health, hunger, and thirst attributes
- Items such as wood, rock, food, tools, equipment, bags, and placeables
- Crafting groups for resources, tools, consumables, placeables, and advanced crafting
- Animals such as boar and deer
- Actions such as attack, build, craft, destroy, drop, eat, equip, open storage, sleep, and split

## Notes

- The project uses the `SURVIVAL_ENGINE` scripting define for standalone builds.
- Some art assets are bundled under `External Packages`; check the original asset licenses before redistributing or using them commercially.
- Generated Unity folders such as `Library`, `Logs`, and `UserSettings` should not be committed.

## License

No license file is currently included in this repository. Add a license before publishing if you want others to use, modify, or redistribute the framework.
