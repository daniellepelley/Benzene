using System;

namespace Benzene.Core.Results;

public interface IHasId : IHasId<Guid>
{}

public interface IHasId<TId>
{
    TId Id { get; }
}