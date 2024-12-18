using Autofac;
using Benzene.Abstractions.DI;

namespace Benzene.Autofac;

public static class Extensions
{
    public static ContainerBuilder UsingBenzene(this ContainerBuilder containerBuilder, Action<IBenzeneServiceContainer> action)
    {
        var autofacBenzeneServiceContainer = new AutofacBenzeneServiceContainer(containerBuilder);
        action(autofacBenzeneServiceContainer);
        return containerBuilder;
    }
}