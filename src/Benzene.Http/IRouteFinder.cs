namespace Benzene.Http;

public interface IRouteFinder
{
    HttpTopicRoute? Find(string method, string path);
}