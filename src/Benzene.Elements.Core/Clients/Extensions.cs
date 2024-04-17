using System;
using Benzene.Clients;
using Benzene.Core.Logging;

namespace Benzene.Elements.Core.Clients;

public static class Extensions
{
    public static ClientBuilder WithCorrelationId(this ClientBuilder source)
    {
        return source.WithDependencyWrapper(new CorrelationIdBenzeneMessageClientWrapper());
    }

}
