using System;
using System.Linq;
using Benzene.Abstractions.DI;
using Benzene.Abstractions.MessageHandlers;
using Benzene.Abstractions.Messages;
using Benzene.Core.Messages;
using Benzene.FluentValidation;
using Benzene.Microsoft.Dependencies;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Benzene.Test.Plugins.FluentValidation;

public class ValidationWarmUpTaskTest
{
    public class WarmRequest
    {
    }

    public class WarmRequestValidator : AbstractValidator<WarmRequest>
    {
        public static int Constructions;

        public WarmRequestValidator()
        {
            Constructions++;
        }
    }

    private sealed class FakeDefinition : IMessageHandlerDefinition
    {
        public FakeDefinition(Type requestType) => RequestType = requestType;
        public ITopic Topic => new Topic("t");
        public Type RequestType { get; }
        public Type ResponseType => typeof(object);
        public Type HandlerType => typeof(object);
    }

    private sealed class FakeFinder : IMessageHandlersFinder
    {
        private readonly IMessageHandlerDefinition[] _definitions;
        public FakeFinder(params Type[] requestTypes) => _definitions = requestTypes.Select(t => (IMessageHandlerDefinition)new FakeDefinition(t)).ToArray();
        public IMessageHandlerDefinition[] FindDefinitions() => _definitions;
    }

    [Fact]
    public void ValidationWarmUpTask_ConstructsTheDiValidatorForEachHandlerRequestType()
    {
        var services = new ServiceCollection();
        var container = new MicrosoftBenzeneServiceContainer(services);
        container.AddFluentValidation(new[] { typeof(WarmRequestValidator) });
        container.AddSingleton<IMessageHandlersFinder>(_ => new FakeFinder(typeof(WarmRequest)));
        var factory = new MicrosoftServiceResolverFactory(services.BuildServiceProvider());

        // Ignore the eager construction AddFluentValidation does for its schema builder; the DI
        // IValidator<T> singleton the middleware resolves is constructed lazily - warm-up forces it now.
        WarmRequestValidator.Constructions = 0;
        using var resolver = factory.CreateScope();
        new ValidationWarmUpTask().WarmUp(resolver);

        Assert.True(WarmRequestValidator.Constructions >= 1);
        Assert.NotNull(resolver.TryGetService<IValidator<WarmRequest>>()); // and it's the resolvable singleton
    }
}
