using System;

namespace Benzene.Elements.Core.Results
{
    public interface IHasId : IHasId<Guid>
    {
    }

    public interface IHasId<TId>
    {
        TId Id { get; }
    }
}