using Benzene.GoogleCloud.Functions.Http;

namespace Benzene.Examples.GoogleCloudMesh.Mesh;

/// <summary>The mesh HTTP function: serves the mesh UI + catalog artifacts + POST /mesh/refresh.</summary>
public class HttpFunction : GoogleCloudFunctionHost<Startup> { }
