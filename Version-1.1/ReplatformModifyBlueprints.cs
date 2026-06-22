using Bindito.Core;
using System.Collections.Generic;
using System.IO;
using Timberborn.BlueprintSystem;
using UnityEngine;

namespace Calloatti.Replatform
{
  // 1. The generic modifier provider that injects platform JSON
  public class GenericPlatformModifier : IBlueprintModifierProvider
  {
    public string ModifierName => "GenericPlatformInjection";

    private List<string> _cachedKeys;
    private Dictionary<string, string> _factionInjections;

    private void InitializeFactionInjections()
    {
      if (_factionInjections != null) return;
      _factionInjections = new Dictionary<string, string>();

      if (string.IsNullOrEmpty(ModStarter.ModPath)) return;

      // Specifically target the "buildings" folder inside the active ModPath
      string buildingsDir = Path.Combine(ModStarter.ModPath, "buildings");
      if (!Directory.Exists(buildingsDir))
      {
        Debug.LogWarning($"[Replatform] Cannot find buildings directory at: {buildingsDir}");
        return;
      }

      // Find all JSON files only in the root of the buildings directory
      string[] jsonFiles = Directory.GetFiles(buildingsDir, "*.json", SearchOption.TopDirectoryOnly);

      foreach (string filePath in jsonFiles)
      {
        string textContent = File.ReadAllText(filePath);

        // We only care about JSON files that actually define a ReplatformableSpec
        if (textContent.Contains("ReplatformableSpec") && textContent.Contains("AvailablePlatforms"))
        {
          string fileName = Path.GetFileName(filePath);

          // "Whitepaws.blueprint.json" -> "whitepaws"
          string factionName = fileName.Replace(".blueprint.json", "").Replace(".json", "").ToLowerInvariant();

          _factionInjections[factionName] = textContent;
          Debug.Log($"[Replatform] Dynamically loaded injection spec for faction: {factionName}");
        }
      }
    }

    public IEnumerable<string> GetModifiers(string blueprintPath)
    {
      if (ModStarter.Config == null) yield break;

      // Lazy initialization of config targets
      if (_cachedKeys == null)
      {
        _cachedKeys = ModStarter.Config.GetAllKeys();
        Debug.Log($"[Replatform] Config cache initialized with {_cachedKeys.Count} keys.");
      }

      // Lazy initialization of the dynamic JSON files via System.IO
      InitializeFactionInjections();

      foreach (string buildingName in _cachedKeys)
      {
        if (ModStarter.Config.GetBool(buildingName))
        {
          string modifierJson = TryGetPlatformModifier(buildingName, blueprintPath);

          if (modifierJson != null)
          {
            Debug.Log($"[Replatform] Successfully injecting spec into {buildingName} at {blueprintPath}");
            yield return modifierJson;
          }
        }
      }
    }

    // Third: The generalized method that accepts any building name and the current path
    private string TryGetPlatformModifier(string buildingName, string blueprintPath)
    {
      // Extract the raw file name and convert to lowercase (e.g., "dam.folktails.blueprint")
      string rawFileName = Path.GetFileName(blueprintPath).ToLowerInvariant();

      // Convert our config key to lowercase as well for a safe comparison
      string lowerBuildingName = buildingName.ToLowerInvariant();

      // Robust check: matches exactly "dam.folktails" OR "dam.folktails.blueprint"
      if (rawFileName != lowerBuildingName && !rawFileName.StartsWith(lowerBuildingName + "."))
      {
        return null;
      }

      // Read the blueprint to ensure we don't inject a duplicate spec
      if (File.Exists(blueprintPath))
      {
        string blueprintContent = File.ReadAllText(blueprintPath);
        if (blueprintContent.Contains("ReplatformableSpec"))
        {
          Debug.LogWarning($"[Replatform] Aborting injection: {buildingName} already contains ReplatformableSpec in its JSON.");
          return null;
        }
      }

      // Dynamically check against all loaded faction JSON texts
      foreach (var kvp in _factionInjections)
      {
        string factionSuffix = "." + kvp.Key; // e.g., ".ironteeth", ".folktails", ".whitepaws"
        if (lowerBuildingName.EndsWith(factionSuffix))
        {
          return kvp.Value; // Return the exact text loaded from the JSON file
        }
      }

      return null;
    }
  }

  // 2. The Configurator that binds the generic provider to the game's engine
  [Context("Game")]
  [Context("MapEditor")]
  public class GenericPlatformConfigurator : Configurator
  {
    protected override void Configure()
    {
      MultiBind<IBlueprintModifierProvider>().To<GenericPlatformModifier>().AsSingleton();
    }
  }
}