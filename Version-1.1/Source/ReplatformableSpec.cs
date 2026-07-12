using System.Collections.Generic;
using Timberborn.BaseComponentSystem;
using Timberborn.BlueprintSystem;
using Timberborn.Persistence;
using Timberborn.WorldPersistence;

namespace Calloatti.Replatform
{
  public record ReplatformableSpec : ComponentSpec
  {
    // A single string bypasses Timberborn's array deserialization crashes completely.
    [Serialize]
    public string AvailablePlatforms { get; init; }

    // The cache property added here
    public List<(int Height, string Name)> ParsedPlatforms { get; set; }
  }

  public class Replatformable : BaseComponent, IPersistentEntity
  {
    private static readonly ComponentKey ReplatformableKey = new ComponentKey("Replatformable");
    private static readonly PropertyKey<bool> IsGhostKey = new PropertyKey<bool>("IsGhost");

    public bool IsReplatformingGhost { get; set; }

    public void Save(IEntitySaver entitySaver)
    {
      if (IsReplatformingGhost)
      {
        entitySaver.GetComponent(ReplatformableKey).Set(IsGhostKey, true);
      }
    }

    public void Load(IEntityLoader entityLoader)
    {
      if (entityLoader.TryGetComponent(ReplatformableKey, out var objectLoader))
      {
        IsReplatformingGhost = objectLoader.Get(IsGhostKey);
      }
    }
  }
}