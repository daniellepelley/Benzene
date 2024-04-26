namespace Benzene.Http.Routing;

public interface IRouteFinder
{
    HttpTopicRoute? Find(string method, string path);
}