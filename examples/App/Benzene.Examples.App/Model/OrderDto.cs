using System;
using Benzene.Core.Results;
using Benzene.Examples.App.Results;

namespace Benzene.Examples.App.Model;

public class OrderDto : IHasId
{
    public string Name { get; set; }
    public string Status { get; set; }
    public Guid Id { get; set; }
}