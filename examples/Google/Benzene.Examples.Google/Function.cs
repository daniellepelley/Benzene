using Benzene.GoogleCloud.Functions.Http;

namespace Benzene.Examples.Google;

/// <summary>
/// The Cloud Functions Gen2 deploy entry point (point <c>gcloud functions deploy --entry-point</c>
/// at <c>Benzene.Examples.Google.Function</c>) - hosts the exact same <see cref="Startup"/> class
/// <c>Program.cs</c> runs on Cloud Run.
/// </summary>
public class Function : GoogleCloudFunctionHost<Startup>
{
}
