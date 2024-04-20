using System;
using System.ComponentModel.DataAnnotations;

namespace Benzene.Examples.App.Data;

public class Order
{
    [Key]
    public Guid Id { get; set; }

    public string Status { get; set; }
    public string Name { get; set; }
}