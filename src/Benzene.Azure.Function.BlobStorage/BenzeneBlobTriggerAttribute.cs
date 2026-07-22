using System;

namespace Benzene.Azure.Function.BlobStorage;

/// <summary>
/// Declares a Blob Storage-triggered Azure Function that forwards into the built <c>IAzureFunctionApp</c>,
/// so Benzene's source generator emits the <c>[Function]</c>/<c>[BlobTrigger]</c> class for you.
/// Include a <c>{name}</c> token in <see cref="Path"/> to bind the blob name. Place at assembly scope;
/// multiple declarations allowed.
/// </summary>
/// <remarks>Requires the <c>Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs</c> package referenced directly by the app.</remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class BenzeneBlobTriggerAttribute : Attribute
{
    /// <summary>The Azure Function name (unique across the app).</summary>
    public string Name { get; set; } = "benzene-blob";

    /// <summary>The blob path template, e.g. <c>incoming/{name}</c> (the <c>{name}</c> token binds the blob name).</summary>
    public string Path { get; set; } = "";

    /// <summary>The app-setting name holding the storage connection. Defaults to <c>AzureWebJobsStorage</c>.</summary>
    public string Connection { get; set; } = "AzureWebJobsStorage";
}
