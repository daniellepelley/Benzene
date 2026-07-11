using Benzene.Core.DI;
using Xunit;

namespace Benzene.Test.Core.Core.DI;

public class RegistrationRecorderTest
{
    private interface IFoo
    {
    }

    private class Foo : IFoo
    {
    }

    [Fact]
    public void IsTypeRegistered_AlwaysReturnsFalse()
    {
        var recorder = new RegistrationRecorder();

        Assert.False(recorder.IsTypeRegistered<Foo>());
        Assert.False(recorder.IsTypeRegistered(typeof(Foo)));
    }

    [Fact]
    public void CreateServiceResolverFactory_ReturnsNull()
    {
        var recorder = new RegistrationRecorder();

        Assert.Null(recorder.CreateServiceResolverFactory());
    }

    [Fact]
    public void AddServiceResolver_ReturnsSameInstance()
    {
        var recorder = new RegistrationRecorder();

        Assert.Same(recorder, recorder.AddServiceResolver());
    }

    [Fact]
    public void AddScoped_RecordsRegisteredTypes()
    {
        var recorder = new RegistrationRecorder();

        recorder.AddScoped<Foo>();
        recorder.AddScoped<IFoo, Foo>();
        recorder.AddScoped(typeof(Foo));
        recorder.AddScoped(typeof(IFoo), typeof(Foo));
        recorder.AddScoped(new Foo());
        recorder.AddScoped(_ => new Foo());

        Assert.Equal(6, recorder.GetTypes().Length);
        Assert.All(recorder.GetTypes(), t => Assert.True(t == typeof(Foo) || t == typeof(IFoo)));
    }

    [Fact]
    public void AddTransient_RecordsRegisteredTypes()
    {
        var recorder = new RegistrationRecorder();

        recorder.AddTransient<Foo>();
        recorder.AddTransient<IFoo, Foo>();
        recorder.AddTransient(typeof(Foo));
        recorder.AddTransient(typeof(IFoo), typeof(Foo));
        recorder.AddTransient(new Foo());
        recorder.AddTransient(_ => new Foo());

        Assert.Equal(6, recorder.GetTypes().Length);
        Assert.All(recorder.GetTypes(), t => Assert.True(t == typeof(Foo) || t == typeof(IFoo)));
    }

    [Fact]
    public void AddSingleton_RecordsRegisteredTypes()
    {
        var recorder = new RegistrationRecorder();

        recorder.AddSingleton<Foo>();
        recorder.AddSingleton<IFoo, Foo>();
        recorder.AddSingleton(typeof(Foo));
        recorder.AddSingleton(typeof(IFoo), typeof(Foo));
        recorder.AddSingleton(new Foo());
        recorder.AddSingleton(_ => new Foo());

        Assert.Equal(6, recorder.GetTypes().Length);
        Assert.All(recorder.GetTypes(), t => Assert.True(t == typeof(Foo) || t == typeof(IFoo)));
    }
}
