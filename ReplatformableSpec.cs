using Timberborn.BaseComponentSystem;
using Timberborn.BlueprintSystem;

namespace Calloatti.Replatform
{
  public record ReplatformableSpec : ComponentSpec
  {
    // A single string bypasses Timberborn's array deserialization crashes completely.
    [Serialize]
    public string AvailablePlatforms { get; init; }
  }

  public class Replatformable : BaseComponent
  {
  }
}