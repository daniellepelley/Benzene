namespace Benzene.Http;

public interface IHttpStatusCodeMapper
{
    string Map(string serviceResultStatus);
}
