using System;

namespace Benzene.Examples.App.Results;

public interface IHasId : IHasId<Guid>
{}

public interface IHasId<TId>
{
    TId Id { get; }
}