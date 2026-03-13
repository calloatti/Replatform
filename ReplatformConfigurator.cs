using Bindito.Core;
using Timberborn.TemplateInstantiation;

namespace Calloatti.Replatform
{
  [Context("Game")]
  [Context("MapEditor")]
  public class ReplatformConfigurator : Configurator
  {
    protected override void Configure()
    {
      Bind<ReplatformService>().AsSingleton();

      // You were right—this is required for the decorator to function
      Bind<Replatformable>().AsTransient();

      MultiBind<TemplateModule>().ToProvider(ProvideTemplateModule).AsSingleton();
    }

    private static TemplateModule ProvideTemplateModule()
    {
      TemplateModule.Builder builder = new TemplateModule.Builder();

      // This tells the game to look for our Spec in the JSON and attach the Component
      builder.AddDecorator<ReplatformableSpec, Replatformable>();

      return builder.Build();
    }
  }
}