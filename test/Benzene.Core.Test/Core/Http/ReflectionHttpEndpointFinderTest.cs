using System.Linq;
using System.Reflection;
using Benzene.Core.MessageHandlers;
using Benzene.Http.Routing;
using Benzene.Test.Examples;
using Xunit;

namespace Benzene.Test.Core.Http;

public class ReflectionHttpEndpointFinderTest
{
    [Fact]
    public void FindRoutes()
    {
        var httpEndpointFinder = new ReflectionHttpEndpointFinder(new ReflectionMessageHandlersFinder(Assembly.GetExecutingAssembly()));

        var findRoutes = httpEndpointFinder.FindDefinitions();

        var exampleRoutes = findRoutes.Where(x => x.Topic == Defaults.Topic).ToArray();
        
        Assert.Equal("GET", exampleRoutes[0].Method);
        Assert.Equal("/example/{id}", exampleRoutes[0].Path);
        
        Assert.Equal("GET", exampleRoutes[1].Method);
        Assert.Equal("/example", exampleRoutes[1].Path);
    }
    
    [Fact]
    public void FindRoutes_NoResponse()
    {
        var httpEndpointFinder = new ReflectionHttpEndpointFinder(new ReflectionMessageHandlersFinder(Assembly.GetExecutingAssembly()));

        var findRoutes = httpEndpointFinder.FindDefinitions();

        var exampleNoResponseRoute = findRoutes.First(x => x.Topic == Defaults.TopicNoResponse);
        Assert.Equal("GET", exampleNoResponseRoute.Method);
        Assert.Equal("/example-no-response", exampleNoResponseRoute.Path); 
    }
}
