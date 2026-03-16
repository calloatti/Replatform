using HarmonyLib;
using System.Collections.Generic;
using Timberborn.BlockSystem;
using Timberborn.Coordinates;
using Timberborn.DeconstructionSystem;
using UnityEngine;

namespace Calloatti.Replatform
{
  public static class ValidationContext
  {
    public static BlockObject CurrentValidatingObject;
  }

  [HarmonyPatch(typeof(BlockObject), nameof(BlockObject.IsValid))]
  public static class BlockObject_IsValid_Patch
  {
    public static void Prefix(BlockObject __instance) => ValidationContext.CurrentValidatingObject = __instance;
    public static void Postfix() => ValidationContext.CurrentValidatingObject = null;
  }

  [HarmonyPatch(typeof(BlockObject), nameof(BlockObject.IsAlmostValid))]
  public static class BlockObject_IsAlmostValid_Patch
  {
    public static void Prefix(BlockObject __instance) => ValidationContext.CurrentValidatingObject = __instance;
    public static void Postfix() => ValidationContext.CurrentValidatingObject = null;
  }

  [HarmonyPatch("Timberborn.BlockSystem.BlockService", "AnyNonOverridableObjectsAt")]
  public static class BlockService_AnyNonOverridableObjectsAt_Patch
  {
    public static void Postfix(Vector3Int coordinates, BlockOccupations occupations, ref bool __result, object __instance)
    {
      if (!__result) return;

      BlockObject placing = ValidationContext.CurrentValidatingObject;
      if (placing != null && placing.GetComponent<ReplatformableSpec>() != null)
      {
        IBlockService blockService = __instance as IBlockService;
        if (blockService == null) return;

        List<BlockObject> intersecting = new List<BlockObject>();
        blockService.GetIntersectingObjectsAt(coordinates, occupations, intersecting);

        bool validOverlap = true;
        bool foundAny = false;

        foreach (var obj in intersecting)
        {
          if (obj == placing) continue;
          foundAny = true;

          // BULLETPROOF: Both must be replatformable and the existing one must be finished.
          // We removed the height check so Big-into-Small works too.
          if (obj.GetComponent<ReplatformableSpec>() == null || !obj.IsFinished || obj.Blocks.Size.z == placing.Blocks.Size.z)
          {
            validOverlap = false;
            break;
          }
        }

        if (foundAny && validOverlap) __result = false;
      }
    }
  }

  [HarmonyPatch(typeof(BlockObject), "AddToService")]
  public static class BlockObject_AddToService_Patch
  {
    public struct SwapData
    {
      public int OldZ;
      public int OldHeight;
      public Orientation Orientation;
      public FlipMode FlipMode;
      public Vector3Int BaseCoords;
      public ReplatformableSpec Spec;
      public bool WasFinished;
    }

    public static void Prefix(BlockObject __instance, object ____blockService, out List<SwapData> __state)
    {
      __state = null;
      if (__instance.IsPreview || __instance.AddedToService) return;

      var placingSpec = __instance.GetComponent<ReplatformableSpec>();
      if (placingSpec != null)
      {
        IBlockService blockService = ____blockService as IBlockService;
        HashSet<BlockObject> toSwap = new HashSet<BlockObject>();

        foreach (Block block in __instance.PositionedBlocks.GetAllBlocks())
        {
          List<BlockObject> intersecting = new List<BlockObject>();
          blockService.GetIntersectingObjectsAt(block.Coordinates, block.Occupation, intersecting);

          foreach (var obj in intersecting)
          {
            if (obj != __instance && obj.GetComponent<ReplatformableSpec>() != null && obj.IsFinished && obj.Blocks.Size.z != __instance.Blocks.Size.z)
            {
              toSwap.Add(obj);
            }
          }
        }

        if (toSwap.Count > 0)
        {
          __state = new List<SwapData>();
          foreach (var oldPlatform in toSwap)
          {
            __state.Add(new SwapData
            {
              OldZ = oldPlatform.Coordinates.z,
              OldHeight = oldPlatform.Blocks.Size.z,
              Orientation = oldPlatform.Orientation,
              FlipMode = oldPlatform.FlipMode,
              BaseCoords = oldPlatform.Coordinates,
              Spec = oldPlatform.GetComponent<ReplatformableSpec>(),
              WasFinished = oldPlatform.IsFinished
            });

            Deconstructible deconstructible = oldPlatform.GetComponent<Deconstructible>();
            if (deconstructible != null) deconstructible.DisableDeconstruction();
            ReplatformService.Instance.EntityService.Delete(oldPlatform);
          }
        }
      }
    }

    public static void Postfix(BlockObject __instance, List<SwapData> __state)
    {
      if (__instance.IsPreview || !__instance.AddedToService || __state == null) return;

      int newZ = __instance.Coordinates.z;
      int newHeight = __instance.Blocks.Size.z;

      foreach (var data in __state)
      {
        FillGap(data, newZ + newHeight, data.OldZ + data.OldHeight); // Above
        FillGap(data, data.OldZ, newZ); // Below
      }
    }

    private static void FillGap(SwapData data, int bottom, int top)
    {
      int remainingHeight = top - bottom;
      int currentZ = bottom;

      while (remainingHeight > 0)
      {
        string filler = GetBestFit(data.Spec, remainingHeight, out int sizeUsed);
        if (filler == null) break;

        Placement p = new Placement(new Vector3Int(data.BaseCoords.x, data.BaseCoords.y, currentZ), data.Orientation, data.FlipMode);
        ReplatformService.Instance.SpawnFiller(filler, p, data.WasFinished);

        remainingHeight -= sizeUsed;
        currentZ += sizeUsed;
      }
    }

    private static string GetBestFit(ReplatformableSpec spec, int gapHeight, out int heightUsed)
    {
      heightUsed = 0;
      if (string.IsNullOrEmpty(spec?.AvailablePlatforms)) return null;

      // The caching logic implemented here
      if (spec.ParsedPlatforms == null)
      {
        spec.ParsedPlatforms = new System.Collections.Generic.List<(int Height, string Name)>();

        string[] platformEntries = spec.AvailablePlatforms.Split(',');

        foreach (string entry in platformEntries)
        {
          string[] parts = entry.Split(':');
          if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int h))
          {
            spec.ParsedPlatforms.Add((h, parts[1].Trim()));
          }
        }

        // Sort descending to pick the largest possible fit
        spec.ParsedPlatforms.Sort((a, b) => b.Height.CompareTo(a.Height));
      }

      foreach (var p in spec.ParsedPlatforms)
      {
        if (gapHeight >= p.Height)
        {
          heightUsed = p.Height;
          return p.Name;
        }
      }
      return null;
    }
  }
}