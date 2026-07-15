using System.Threading.Tasks;
using Benzene.Abstractions.Middleware;
using Benzene.Core.Middleware;
using Benzene.Diagnostics;
using Moq;
using Xunit;

namespace Benzene.Test.Diagnostics;

public class DebugMiddlewareDecoratorTest
{
    [Fact]
    public void Name_DelegatesToTheInnerMiddleware()
    {
        var inner = new Mock<IMiddleware<object>>();
        inner.Setup(x => x.Name).Returns("my-middleware");

        var decorator = new DebugMiddlewareDecorator<object>(inner.Object);

        Assert.Equal("my-middleware", decorator.Name);
    }

    [Fact]
    public async Task HandleAsync_DelegatesToTheInnerMiddleware_WithTheSameContextAndNext()
    {
        var context = new object();
        var nextCalled = false;
        Task Next() { nextCalled = true; return Task.CompletedTask; }

        var inner = new Mock<IMiddleware<object>>();
        inner.Setup(x => x.Name).Returns("my-middleware");
        inner.Setup(x => x.HandleAsync(context, It.IsAny<System.Func<Task>>()))
            .Returns<object, System.Func<Task>>((_, next) => next());

        var decorator = new DebugMiddlewareDecorator<object>(inner.Object);

        await decorator.HandleAsync(context, Next);

        inner.Verify(x => x.HandleAsync(context, It.IsAny<System.Func<Task>>()), Times.Once);
        Assert.True(nextCalled);
    }

    [Fact]
    public void Wrap_ReturnsADebugMiddlewareDecoratorAroundTheGivenMiddleware()
    {
        var inner = new Mock<IMiddleware<object>>();
        inner.Setup(x => x.Name).Returns("my-middleware");

        var wrapped = new DebugMiddlewareWrapper().Wrap(new NullServiceResolver(), inner.Object);

        Assert.IsType<DebugMiddlewareDecorator<object>>(wrapped);
        Assert.Equal("my-middleware", wrapped.Name);
    }
}
