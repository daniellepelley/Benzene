using Autofac;
using Benzene.Abstractions.DI;

namespace Benzene.Autofac;

public static class Extensions
{
    public static ContainerBuilder UsingBenzene(this ContainerBuilder containerBuilder, Action<IBenzeneServiceContainer> action)
    {
        var microsoftBenzeneServiceContainer = new AutofacBenzeneServiceContainer(containerBuilder);
        action(microsoftBenzeneServiceContainer);
        return containerBuilder;
    }
}