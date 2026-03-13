using HarmonyLib;
using Timberborn.ModManagerScene;
using UnityEngine; // Add this

namespace Calloatti.Replatform
{
  public class ReplatformPlugin : IModStarter
  {
    public void StartMod(IModEnvironment modEnvironment)
    {
      Debug.Log("[Replatform] Mod initialized! Patching Harmony...");
      Harmony harmony = new Harmony("calloatti.replatform");
      harmony.PatchAll();
      Debug.Log("[Replatform] Harmony patching complete.");
    }
  }
}