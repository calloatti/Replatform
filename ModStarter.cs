using HarmonyLib;
using Timberborn.ModManagerScene;
using UnityEngine;
using Calloatti.Config;

namespace Calloatti.Replatform
{
  public class ModStarter : IModStarter
  {
    // 1. Declare the globally accessible static instance
    public static SimpleConfig Config { get; private set; }
    public static string ModPath { get; private set; }

    public void StartMod(IModEnvironment modEnvironment)
    {
      ModPath = modEnvironment.ModPath;
      // 2. Instantiate the config. This instantly runs the TXT synchronization.
      Config = new SimpleConfig(modEnvironment.ModPath);

      Debug.Log("[Replatform] Mod initialized! Patching Harmony...");
      Harmony harmony = new Harmony("calloatti.replatform");
      harmony.PatchAll();
      Debug.Log("[Replatform] Harmony patching complete.");
    }
  }
}