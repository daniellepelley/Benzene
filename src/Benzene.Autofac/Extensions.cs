using Autofac;
using Benzene.Abstractions.DI;

namespace Benzene.Autofac;

public static class Extensions
{
    public static ContainerBuilder UsingBenzene(this ContainerBuilder containerBuilder)
    {
        CreateAutofacBenzeneServiceContainer(containerBuilder);
        return containerBuilder;
    }

    public static ContainerBuilder UsingBenzene(this ContainerBuilder containerBuilder, Action<IBenzeneServiceContainer> action)
    {
        var autofacBenzeneServiceContainer = CreateAutofacBenzeneServiceContainer(containerBuilder);
        action(autofacBenzeneServiceContainer);
        return containerBuilder;
    }

    private static AutofacBenzeneServiceContainer CreateAutofacBenzeneServiceContainer(ContainerBuilder containerBuilder)
    {
        var autofacBenzeneServiceContainer = new AutofacBenzeneServiceContainer(containerBuilder);
        autofacBenzeneServiceContainer.AddScoped<IBenzeneServiceContainer>(_ => autofacBenzeneServiceContainer);
        autofacBenzeneServiceContainer.AddServiceResolver();
        return autofacBenzeneServiceContainer;
    }
}