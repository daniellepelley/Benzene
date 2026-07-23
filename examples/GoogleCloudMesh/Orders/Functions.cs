using Benzene.GoogleCloud.Functions.Http;

namespace Benzene.Examples.GoogleCloudMesh.Orders;

/// <summary>HTTP function: serves the Cloud Service Profile the mesh polls. Deploy with --trigger-http.</summary>
public class HttpFunction : GoogleCloudFunctionHost<Startup> { }
