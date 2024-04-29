namespace Benzene.Grpc;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class GrpcMethodAttribute : Attribute
{
    public GrpcMethodAttribute(string method)
    {
        Method = method;
    }

    public string Method { get; }
}