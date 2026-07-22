using Benzene.Abstractions.DI;
using DryIoc;

namespace Benzene.DryIoc;

public static class Extensions
{
    /// <summary>
    /// Creates a DryIoc <see cref="Container"/> configured the way Benzene expects, matching
    /// Microsoft.Extensions.DependencyInjection semantics on the two points Benzene relies on:
    /// <list type="bullet">
    /// <item><b>Last-registration wins</b> on a single resolve (<see cref="Rules.SelectLastRegisteredFactory"/>)
    /// — so re-registering a service (e.g. overriding a Benzene default) resolves the later registration
    /// rather than throwing on multiple defaults.</item>
    /// <item><b>Greediest resolvable constructor</b> (<see cref="FactoryMethod.ConstructorWithResolvableArguments"/>)
    /// — Benzene registers types with more than one public constructor (e.g. its <c>JsonSerializer</c>),
    /// which DryIoc otherwise refuses to construct; MS DI and Autofac pick a constructor automatically.</item>
    /// </list>
    /// Prefer this over <c>new Container()</c> when wiring Benzene; if you bring your own container, apply
    /// the same two rules (or use DryIoc's <c>WithMicrosoftDependencyInjectionRules()</c>, which is a superset).
    /// </summary>
    public static IContainer CreateContainer()
        => new Container(rules => rules
            .WithFactorySelector(Rules.SelectLastRegisteredFactory())
            .With(FactoryMethod.ConstructorWithResolvableArguments));

    public static IContainer UsingBenzene(this IContainer container)
    {
        CreateDryIocBenzeneServiceContainer(container);
        return container;
    }

    public static IContainer UsingBenzene(this IContainer container, Action<IBenzeneServiceContainer> action)
    {
        var dryIocBenzeneServiceContainer = CreateDryIocBenzeneServiceContainer(container);
        action(dryIocBenzeneServiceContainer);
        return container;
    }

    private static DryIocBenzeneServiceContainer CreateDryIocBenzeneServiceContainer(IContainer container)
    {
        var dryIocBenzeneServiceContainer = new DryIocBenzeneServiceContainer(container);
        dryIocBenzeneServiceContainer.AddScoped<IBenzeneServiceContainer>(_ => dryIocBenzeneServiceContainer);
        dryIocBenzeneServiceContainer.AddServiceResolver();
        return dryIocBenzeneServiceContainer;
    }
}
