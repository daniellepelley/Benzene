using System;
using System.Collections.Generic;
using System.Linq;
using Benzene.CodeGen.Client;
using Benzene.Test.Autogen.CodeGen.Helpers;
using Benzene.Test.Autogen.CodeGen.Model;
using Xunit;

namespace Benzene.Test.Autogen.CodeGen.Client;

public class AtomicClientSdkBuilderTest
{
    private const string BaseNameSpace = "Benzene.Service.Clients";

    private static IDictionary<string, string> Build(IDictionary<string, (Type, Type, Type)> topics)
    {
        var builder = new AtomicClientSdkBuilder(BaseNameSpace);
        return builder.Build(topics.ToEventServiceDocument());
    }

    [Fact]
    public void EmitsOneClientPerTopic_NamedPerTopic()
    {
        var result = Build(new Dictionary<string, (Type, Type, Type)>
        {
            { "user:get", (typeof(GetUserMessage), typeof(GetUserMessage), typeof(UserDto)) },
            { "user:create", (typeof(CreateUserMessage), typeof(CreateUserMessage), typeof(string)) },
        });

        // Each topic gets its own self-contained client folder + interface, named from the topic
        // (UserGet, UserCreate), rather than one shared UserServiceClient covering every topic.
        Assert.Contains("UserGet/UserGetServiceClient.cs", result.Keys);
        Assert.Contains("UserGet/IUserGetServiceClient.cs", result.Keys);
        Assert.Contains("UserCreate/UserCreateServiceClient.cs", result.Keys);
        Assert.Contains("UserCreate/IUserCreateServiceClient.cs", result.Keys);
    }

    [Fact]
    public void EachClient_ScopesMethodAndRequiredTopics_ToItsOwnTopic()
    {
        var result = Build(new Dictionary<string, (Type, Type, Type)>
        {
            { "user:get", (typeof(GetUserMessage), typeof(GetUserMessage), typeof(UserDto)) },
            { "user:create", (typeof(CreateUserMessage), typeof(CreateUserMessage), typeof(string)) },
        });

        var getClient = result["UserGet/UserGetServiceClient.cs"];

        // The user:get atomic client sends only user:get and validates only that topic (+ healthcheck)
        // at startup — it never references user:create.
        Assert.Contains("\"user:get\"", getClient);
        Assert.DoesNotContain("user:create", getClient);
        Assert.Contains("RequiredTopics = { \"user:get\", \"healthcheck\" }", getClient);
    }

    [Fact]
    public void OnlyGeneratesDtosReachableFromTheTopic()
    {
        // Two topics with disjoint payloads: user:get ⇒ UserDto, tenant:get ⇒ TenantDto. Each atomic
        // client must carry only its own DTO, not the other topic's — proving schema filtering works.
        var result = Build(new Dictionary<string, (Type, Type, Type)>
        {
            { "user:get", (typeof(GetUserMessage), typeof(GetUserMessage), typeof(UserDto)) },
            { "tenant:get", (typeof(GetTenantMessage), typeof(GetTenantMessage), typeof(TenantDto)) },
        });

        // Each client folder carries only its own topic's DTO, not the other topic's.
        Assert.Contains("UserGet/UserDto.cs", result.Keys);
        Assert.Contains("TenantGet/TenantDto.cs", result.Keys);
        Assert.DoesNotContain("UserGet/TenantDto.cs", result.Keys);
        Assert.DoesNotContain("TenantGet/UserDto.cs", result.Keys);
        // The hash embedded in each client is topic-scoped, so the two clients have different hashes.
        var userHash = HashLine(result["UserGet/UserGetServiceClient.cs"]);
        var tenantHash = HashLine(result["TenantGet/TenantGetServiceClient.cs"]);
        Assert.NotEqual(userHash, tenantHash);
    }

    [Fact]
    public void SkipsReservedTopics_ByDefault()
    {
        var document = new Dictionary<string, (Type, Type, Type)>
        {
            { "user:get", (typeof(GetUserMessage), typeof(GetUserMessage), typeof(UserDto)) },
        }.ToEventServiceDocument();
        document.Requests.First(x => x.Topic == "user:get").Reserved = true;

        var result = new AtomicClientSdkBuilder(BaseNameSpace).Build(document);

        Assert.DoesNotContain("UserGet/UserGetServiceClient.cs", result.Keys);
    }

    private static string HashLine(string clientSource) =>
        clientSource.Split('\n').First(line => line.Contains("HashCode =>"));
}
