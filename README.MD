Replatform

https://steamcommunity.com/sharedfiles/filedetails/?id=3684033679

# Replatform: Technical Architecture

This document outlines the internal structure and logic of the **Replatform** mod for Timberborn. It is intended for other modders who want to understand how the dynamic platform splitting/merging is achieved or how to make their own custom platforms compatible.

## Overview
Replatform allows players to dynamically replace existing platforms with larger or smaller variants without destroying the buildings placed on top of them. It achieves this by bypassing standard validation checks, intercepting the placement event, deleting the old platforms, and instantly spawning "filler" platforms to close any resulting gaps.

## 1. Data Structure (JSON)
To avoid writing a custom Timberborn deserializer module, the mod uses a primitive comma-separated string in the building's `.blueprint.json` file. The engine's `BasicDeserializer` struggles with custom arrays of objects, so this single-string approach is both performant and crash-proof.

**Property:** `AvailablePlatforms`
**Format:** `"Height:TemplateName,Height:TemplateName"`

```json
{
  "ReplatformableSpec": {
    "AvailablePlatforms": "1:Platform.Folktails,2:DoublePlatform.Folktails,3:TriplePlatform.Folktails"
  }
}
```
Every platform in a specific "family" (e.g., all Folktails platforms) must share this exact same string. This acts as a lookup table for the C# code.

## 2. Core C# Components

### `ReplatformableSpec` & `Replatformable`
* **`ReplatformableSpec`**: A `ComponentSpec` record that holds the parsed `AvailablePlatforms` string.
* **`Replatformable`**: An empty `BaseComponent` that acts as a tag/marker.
* **Configuration**: `ReplatformConfigurator` uses `AddDecorator<ReplatformableSpec, Replatformable>()` to dynamically attach the component to any building that includes the JSON spec.

### `ReplatformService`
A singleton service responsible for spawning the filler platforms. It uses Timberborn's `BlockObjectFactory` and `TemplateNameMapper`.
* If the deleted platform was fully built, it uses `CreateFinished` so top-level buildings don't collapse.
* If the deleted platform was unbuilt, it uses `CreateUnfinished`.

## 3. Harmony Patches (The Logic Engine)

The mod relies on four targeted Harmony patches to manipulate the placement system.

### A. Context Tracking (`IsValid` & `IsAlmostValid`)
Timberborn's placement validation passes primitive coordinates rather than the actual `BlockObject` being placed.
* **Patches**: `BlockObject.IsValid`, `BlockObject.IsAlmostValid`
* **Action**: A `Prefix` grabs the `__instance` being validated and stores it in a static `ValidationContext`, clearing it in the `Postfix`. This allows downstream methods to know exactly *what* is being placed.

### B. Validation Bypass (`AnyNonOverridableObjectsAt`)
Standard placement rules block buildings from overlapping. 
* **Patch**: `BlockService.AnyNonOverridableObjectsAt`
* **Action**: A `Postfix` checks if the space is blocked. If the blocking object(s) and the placing object both have the `Replatformable` component, and the blocking objects are fully finished, the mod forces `__result = false`. This tricks the game into showing a green "valid" placement hologram.

### C. The Swap Sequence (`AddToService`)
This is where the actual replacement happens, split between a Prefix and Postfix.
* **Patch**: `BlockObject.AddToService`

**The Prefix (Deletion):**
1. Runs just before the new platform is officially added to the game's block grid.
2. Identifies all intersecting `Replatformable` objects.
3. Records their data (Coordinates, Z-Height, Orientation, FlipMode, Finished State) into a `SwapData` struct passed to the Postfix via `__state`.
4. Disables their `Deconstructible` component to prevent refunds/ruins, then calls `EntityService.Delete()` to erase them.

**The Postfix (Filling Gaps):**
1. Runs immediately after the new platform is added.
2. Calculates vertical gaps:
   * **Upper Gap**: Between the top of the new platform and the top of the old platform(s).
   * **Lower Gap**: Between the bottom of the old platform(s) and the bottom of the new platform.
3. Parses the `AvailablePlatforms` string from the JSON.
4. Uses `GetBestFit()` to sort available templates by height (descending) and loops through the gap, spawning the largest possible platform templates to fill the void via `ReplatformService`.

## Compatibility
Because the mod relies on the `AvailablePlatforms` string mapping, it is inherently faction-safe and mod-safe. A Folktails platform will only ever spawn Folktails fillers, and custom modded platforms can easily opt-in just by adding the `ReplatformableSpec` to their blueprints.