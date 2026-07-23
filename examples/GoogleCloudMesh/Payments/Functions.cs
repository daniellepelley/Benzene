using Benzene.GoogleCloud.Functions.Http;
using Benzene.GoogleCloud.Functions.PubSub;

namespace Benzene.Examples.GoogleCloudMesh.Payments;

/// <summary>HTTP function: serves the Cloud Service Profile the mesh polls. Deploy with --trigger-http.</summary>
public class HttpFunction : GoogleCloudFunctionHost<Startup> { }

/// <summary>Pub/Sub function: consumes events routed by the "topic" attribute. Deploy with --trigger-topic.</summary>
public class PubSubFunction : GooglePubSubFunctionHost<Startup> { }
