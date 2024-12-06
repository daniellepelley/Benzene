using System.Reflection;
using Benzene.Abstractions.MessageHandling;
using Benzene.Core.Helper;
using Benzene.Core.MessageHandling;
using Benzene.Http.Routing;
using Benzene.Schema.OpenApi.EventService;

namespace Benzene.CodeGen.Core;

public static class CodeBuilderExtensions
{
    public static IDictionary<string, string> ToFilesDictionary(this ICodeFile[] source)
    {
        return source.ToDictionary(x => x.Name, ToText);
    }
    
    public static string ToText(this ICodeFile source)
    {
        return string.Join("", source.Lines.Select(x => $"{x}{Environment.NewLine}"));
    }

    public static string[] ToLines(this string source)
    {
        return source.Split(new []{ Environment.NewLine }, StringSplitOptions.None);
    }

    public static ICodeFile[] BuildCodeFiles(this ICodeBuilder<EventServiceDocument> source, IMessageHandlerDefinition[] messageHandlerDefinitions)
    {
        return source.BuildCodeFiles(messageHandlerDefinitions.ToEventServiceDocument());
    }
    
    public static ICodeFile[] BuildCodeFiles(this ICodeBuilder<EventServiceDocument> source, IHttpEndpointDefinition[] httpEndpointDefinitions, IMessageHandlerDefinition[] messageHandlerDefinitions)
    {
        return source.BuildCodeFiles(httpEndpointDefinitions.ToEventServiceDocument(messageHandlerDefinitions));
    }

    public static ICodeFile[] BuildCodeFiles(this ICodeBuilder<EventServiceDocument> source, params Assembly[] assemblies)
    {
        return source.BuildCodeFiles(Utils.GetAllTypes(assemblies).ToArray());
    }
    
    public static ICodeFile[] BuildCodeFiles(this ICodeBuilder<EventServiceDocument> source, params Type[] types)
    {
        var messageHandlersFinder = new ReflectionMessageHandlersFinder(types);
        var messageHandlerDefinitions = messageHandlersFinder.FindDefinitions();

        var httpEndpointFinder = new ReflectionHttpEndpointFinder(messageHandlersFinder);
        var httpEndpointDefinitions = httpEndpointFinder.FindDefinitions();
        return source.BuildCodeFiles(httpEndpointDefinitions.ToEventServiceDocument(messageHandlerDefinitions));
    }

}
