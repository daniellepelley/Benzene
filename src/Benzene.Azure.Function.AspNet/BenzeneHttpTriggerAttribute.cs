using System;
using Microsoft.Azure.Functions.Worker;

namespace Benzene.Azure.Function.AspNet;

/// <summary>
/// Declares an HTTP-triggered Azure Function that forwards into the built <c>IAzureFunctionApp</c>,
/// so you don't hand-write the <c>[Function]</c>/<c>[HttpTrigger]</c> boilerplate class. Place it at
/// assembly scope; Benzene's source generator emits the trigger function for you. You own the
/// <see cref="Name"/> and <see cref="Route"/> — nothing is hard-coded.
/// </summary>
/// <remarks>
/// Example: <c>[assembly: BenzeneHttpTrigger(Name = "orders", Route = "{*restOfPath}")]</c>.
/// Multiple declarations are allowed (each becomes its own function). Requires the
/// <c>Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore</c> extension package to be
/// referenced directly by the app (a Functions tooling requirement, not a Benzene one).
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class BenzeneHttpTriggerAttribute : Attribute
{
    /// <summary>The Azure Function name (must be unique across the app). Also names the generated class.</summary>
    public string Name { get; set; } = "benzene";

    /// <summary>The HTTP route template. Defaults to a catch-all (<c>{*restOfPath}</c>) so Benzene's own routing handles every path.</summary>
    public string Route { get; set; } = "{*restOfPath}";

    /// <summary>The trigger's authorization level. Defaults to <see cref="AuthorizationLevel.Anonymous"/>.</summary>
    public AuthorizationLevel AuthorizationLevel { get; set; } = AuthorizationLevel.Anonymous;

    /// <summary>The HTTP methods this function accepts. Defaults to <c>get, post, put, delete, options</c>.</summary>
    public string[] Methods { get; set; } = { "get", "post", "put", "delete", "options" };
}
