using Timberborn.BlockSystem;
using Timberborn.Buildings;
using Timberborn.Coordinates;
using Timberborn.EntitySystem;
using Timberborn.TemplateSystem;

namespace Calloatti.Replatform
{
  public class ReplatformService
  {
    public static ReplatformService Instance { get; private set; }

    public EntityService EntityService { get; }
    private readonly BlockObjectFactory _blockObjectFactory;
    private readonly TemplateNameMapper _templateNameMapper;

    public ReplatformService(EntityService entityService, BlockObjectFactory blockObjectFactory, TemplateNameMapper templateNameMapper)
    {
      EntityService = entityService;
      _blockObjectFactory = blockObjectFactory;
      _templateNameMapper = templateNameMapper;
      Instance = this;
    }

    public void SpawnFiller(string fillerTemplateName, Placement placement, bool wasFinished)
    {
      // Grab the prefab spec for the platform
      BlockObjectSpec fillerSpec = _templateNameMapper.GetTemplate(fillerTemplateName).GetSpec<BlockObjectSpec>();

      BlockObject filler;
      if (wasFinished)
      {
        filler = _blockObjectFactory.CreateFinished(fillerSpec, placement);
      }
      else
      {
        filler = _blockObjectFactory.CreateUnfinished(fillerSpec, placement);
        // Flag for ghost visuals
        var replatformable = filler.GetComponent<Replatformable>();
        if (replatformable != null)
        {
          replatformable.IsReplatformingGhost = true;
          // Force initial model refresh
          filler.GetComponent<BuildingModel>()?.ShowFinishedModel();
        }
      }

      filler.AddToServiceAfterLoad();
    }
  }
}