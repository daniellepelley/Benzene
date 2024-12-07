using System.Linq;
using System.Reflection;
using Benzene.Core.MessageHandlers;
using Benzene.Core.MessageHandling;
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

        var exampleRoute = findRoutes.First(x => x.Topic == Defaults.Topic);
        
        Assert.Equal("GET", exampleRoute.Method);
        Assert.Equal("/example", exampleRoute.Path);
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
