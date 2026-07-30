Include ..\AGENTS.md

# Replatform — Mod-Specific Agent Instructions

## Identity
- **Assembly:** `replatform`
- **Namespace:** `Calloatti.Replatform`
- **Framework:** Harmony, Bindito DI
- **Publicizer:** removes `Timberborn.BlueprintSystem`
- **ModId:** `Calloatti.Replatform`
- **Min Game Version:** 1.0.12.9 — uses `timberborn-decompiled-1.0.*`

## What This Mod Does
Allows replatforming (resurfacing) terrain tiles. Adds a replatform tool that modifies terrain surface elevation at the block level, letting players reshape land after map creation.

## Source Architecture (`Version-1.0/Source/`)

| File | Role |
|---|---|
| `ModStarter.cs` | Entry point — `IModStarter` |
| `ReplatformConfigurator.cs` | DI configurator |
| `ReplatformService.cs` | Core replatforming logic |
| `ReplatformPatches.cs` | Harmony patches |
| `ReplatformModifyBlueprints.cs` | Blueprint modifications for replatforming |
| `ReplatformableSpec.cs` | ComponentSpec for replatformable entities |

## Version Folders
- `Version-1.0` — targets game 1.0.x.x
- `Version-1.1` — targets game 1.1.x.x
