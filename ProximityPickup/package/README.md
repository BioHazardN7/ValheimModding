# Proximity Pickup

Walk near a treasure, a mushroom or any other "pickable" object and it is picked up automatically, exactly as if you had pressed **E** on it. You choose which objects are affected and how close you need to be.

## Features

- Automatic interaction with pickable objects inside a configurable radius
- Fully configurable list of prefabs (works for dungeon treasures, random loot, plants, etc.)
- Event-driven: objects are registered when they spawn, so there is no periodic world scan and no polling delay
- Respects the UI: does nothing while the inventory, menu, chat or console is open, or while you are dead / teleporting
- Client-side: no need to install it on the server
- Optional debug log to find exact prefab names

## Default prefabs

```
Pickable_DolmenTreasure
Pickable_ForestCryptRandom
Pickable_Item
Pickable_MorkHallaTreasure
Pickable_MountainCaveRandom
Pickable_SunkenCryptRandom
```

Items are picked the same way as a stone or a branch: they drop and can then be put into your inventory.

## Installation

1. Install [BepInEx for Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/).
2. Put `ProximityPickup.dll` into `BepInEx/plugins`.
3. Start the game once to generate the config.

## Configuration

File: `BepInEx/config/BioHazardN7.ProximityPickup.cfg` (changes apply live, no restart needed).

| Section | Option | Default | Description |
|---|---|---|---|
| General | `Enabled` | `true` | Enable or disable the mod. |
| General | `Radius` | `5` | Horizontal radius in meters around the player. |
| General | `MaxDistanceBelow` | `0.5` | Max distance in meters below the player. Deeper objects (e.g. buried treasure) are ignored; there is no limit above. |
| General | `MaxPerFrame` | `3` | Max objects interacted with per frame. |
| General | `RetryDelay` | `3` | Seconds before the same object is interacted with again. |
| General | `Prefabs` | see above | Comma-separated prefab names (without `(Clone)`, case ignored). |
| Debug | `DebugLog` | `false` | Log registered objects, their components and interaction results. |

## Finding prefab names

Enable `DebugLog`, stand near the object and check `BepInEx/LogOutput.log` for `Registered '...'` lines. Or use the in-game console command `printnetobj <radius>`.

## How it works

A small Harmony patch on `ZNetView.Awake` checks each newly spawned network object against your prefab list and stores only the matching ones. Every frame the mod checks the distance to those few objects and calls the normal interaction (`Pickable.Interact`, or any other interactable on the object) for those inside the radius.

## Compatibility

Client-side only. Uses the game's normal interaction, so ownership and syncing are handled by the game itself.