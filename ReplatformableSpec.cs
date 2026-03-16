using System.Collections.Generic;
using Timberborn.BaseComponentSystem;
using Timberborn.BlueprintSystem;

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

  public class Replatformable : BaseComponent
  {
  }
}