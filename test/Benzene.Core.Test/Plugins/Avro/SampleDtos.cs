using System;
using System.Collections.Generic;

namespace Benzene.Test.Plugins.Avro;

public enum SampleStatus
{
    Pending,
    Filled,
    Cancelled
}

public class SampleOrderDto
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public long Reference { get; set; }
    public decimal Price { get; set; }
    public double Weight { get; set; }
    public bool Active { get; set; }
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public SampleStatus Status { get; set; }
    public List<string> Tags { get; set; } = new();
    public int? OptionalCount { get; set; }
    public SampleLegDto Leg { get; set; } = new();
}

public class SampleLegDto
{
    public string Label { get; set; } = string.Empty;
    public double Amount { get; set; }
}

public class Point
{
    public int X { get; set; }
    public int Y { get; set; }
}
