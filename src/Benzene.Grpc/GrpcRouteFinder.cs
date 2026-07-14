namespace Benzene.Grpc;

public class GrpcRouteFinder : IGrpcRouteFinder
{
    private readonly IDictionary<string, IGrpcMethodDefinition> _grpcMethodDefinitionsByMethod;

    public GrpcRouteFinder(IGrpcMethodFinder grpcMethodFinder)
    {
        _grpcMethodDefinitionsByMethod = grpcMethodFinder.FindDefinitions()
            .ToDictionary(x => x.Method, x => x, StringComparer.OrdinalIgnoreCase);
    }

    public IGrpcMethodDefinition? Find(string method)
    {
        return _grpcMethodDefinitionsByMethod.TryGetValue(method, out var definition) ? definition : null;
    }
}

