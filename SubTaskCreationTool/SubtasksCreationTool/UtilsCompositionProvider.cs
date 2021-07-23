using System;
using System.Composition;
using System.Composition.Convention;
using System.Composition.Hosting;
using OneInc.ProcessOne.Libs.Composition;

namespace SubtasksCreationTool
{
    public static class UtilsCompositionProvider
    {
        private static readonly Lazy<CompositionProvider> GlobalCompositionProvider = new Lazy<CompositionProvider>(() =>
            InitializeProvider(new ConsoleApplicationContainerConfiguration(DefineConventions(null))));

        public static Export<CompositionContext> GlobalCompositionContext => GlobalCompositionProvider.Value.RequestScopeFactory.CreateExport();

        private static CompositionProvider InitializeProvider(ContainerConfiguration configuration)
        {
            var compositionProvider = new CompositionProvider();
            compositionProvider.Initialize(configuration);
            return compositionProvider;
        }

        private static AttributedModelProvider DefineConventions(Type customLogicSettingsProviderType)
        {
            var conventionBuilder = new ConventionBuilder();

            return conventionBuilder;
        }
    }
}
