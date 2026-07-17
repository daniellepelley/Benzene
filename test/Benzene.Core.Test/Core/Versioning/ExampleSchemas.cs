using System.Collections.Generic;

// Example payload schemas used by the versioning/casting tests. Each version lives in its own
// namespace with identical type names - that is the convention SchemaTypeMatcher relies on to
// match nested and polymorphic types between schema versions without explicit registration.

namespace Benzene.Test.Core.Versioning.Schemas.V1
{
    public class OrderPayload
    {
        public string Id { get; set; }
        public int Quantity { get; set; }
        public string CustomerName { get; set; }
        public decimal? Discount { get; set; }
        public OrderStatus Status { get; set; }
        public Address ShippingAddress { get; set; }
        public List<OrderLine> Lines { get; set; }
    }

    public class Address
    {
        public string City { get; set; }
        public string PostCode { get; set; }
    }

    public class OrderLine
    {
        public string Sku { get; set; }
        public int Count { get; set; }
    }

    public enum OrderStatus
    {
        Pending = 0,
        Shipped = 1
    }

    public class Drawing
    {
        public string Name { get; set; }
        public Shape Shape { get; set; }
    }

    public abstract class Shape
    {
        public string Colour { get; set; }
    }

    public class Circle : Shape
    {
        public double Radius { get; set; }
    }

    public class Oval : Shape
    {
        public double Width { get; set; }
        public double Height { get; set; }
    }
}

namespace Benzene.Test.Core.Versioning.Schemas.V2
{
    public class OrderPayload
    {
        public string Id { get; set; }
        public int Quantity { get; set; }
        public decimal? Discount { get; set; }
        public string Currency { get; set; }
        public OrderStatus Status { get; set; }
        public Address ShippingAddress { get; set; }
        public List<OrderLine> Lines { get; set; }
    }

    public class Address
    {
        public string City { get; set; }
        public string PostCode { get; set; }
    }

    public class OrderLine
    {
        public string Sku { get; set; }
        public int Count { get; set; }
    }

    public enum OrderStatus
    {
        Pending = 0,
        Shipped = 1
    }

    public class Drawing
    {
        public string Name { get; set; }
        public Shape Shape { get; set; }
    }

    public abstract class Shape
    {
        public string Colour { get; set; }
    }

    public class Circle : Shape
    {
        public double Radius { get; set; }
    }

    public class Ellipse : Shape
    {
        public double Width { get; set; }
        public double Height { get; set; }
    }
}

namespace Benzene.Test.Core.Versioning.Schemas.V3
{
    public class OrderPayload
    {
        public string Id { get; set; }
        public int Quantity { get; set; }
        public decimal? Discount { get; set; }
        public string Currency { get; set; }
        public string Reference { get; set; }
    }
}
