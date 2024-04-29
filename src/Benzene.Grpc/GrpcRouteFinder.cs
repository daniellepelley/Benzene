namespace Benzene.Grpc;

public class GrpcRouteFinder : IGrpcRouteFinder
{
    private readonly IGrpcMethodDefinition[] _grpcMethodDefinitions;

    public GrpcRouteFinder(IGrpcMethodFinder grpcMethodFinder)
    {
        _grpcMethodDefinitions = grpcMethodFinder.FindDefinitions();
    }

    public IGrpcMethodDefinition? Find(string method)
    {
        return  _grpcMethodDefinitions.FirstOrDefault(x => x.Method.ToLowerInvariant() == method.ToLowerInvariant());
    }
}

