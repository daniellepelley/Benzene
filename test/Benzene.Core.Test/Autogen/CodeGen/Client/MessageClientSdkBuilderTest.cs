using System;
using System.Collections.Generic;
using System.IO;
using Benzene.CodeGen.Client;
using Benzene.CodeGen.Core;
using Benzene.Test.Autogen.CodeGen.Helpers;
using Benzene.Test.Autogen.CodeGen.Model;
using Xunit;

namespace Benzene.Test.Autogen.CodeGen.Client;

public class MessageClientSdkBuilderTest
{
    private const string BaseNameSpace = "Platform.Service.Clients";
    private const string UserServiceName = "User";
    private const string TenantServiceName = "Tenant";

    private string LoadExpected(string fileName) => File.ReadAllText($"{Directory.GetCurrentDirectory()}/Autogen/CodeGen/Client/Examples/{fileName}.txt");

    [Fact]
    public void BuildsSdk_UserGet_Test()
    {
        var expected = LoadExpected("LambdaService_UserGet");

        var dictionary = new Dictionary<string, (Type, Type, Type)>
        {
            { "user:get", (typeof(GetUserMessage), typeof(GetUserMessage), typeof(MessageWrapper<UserDto>)) }
        };

        var lambdaServiceSdkBuilder = new MessageClientSdkBuilder(UserServiceName, BaseNameSpace);

        var result = lambdaServiceSdkBuilder.Build(dictionary.ToEventServiceDocument());

        Assert.Equal(expected, result["UserServiceClient.cs"], ignoreLineEndingDifferences: true);
    }
        
    [Fact]
    public void BuildsSdk_UserCreate_Test()
    {
        var expected = LoadExpected("LambdaService_UserCreate");

        var dictionary = new Dictionary<string, (Type, Type, Type)>
        {
            { "user:create", (typeof(CreateUserMessage), typeof(CreateUserMessage), typeof(Guid?)) }
        };

        var lambdaServiceSdkBuilder = new MessageClientSdkBuilder(UserServiceName, BaseNameSpace);

        var result = lambdaServiceSdkBuilder.Build(dictionary.ToEventServiceDocument());

        Assert.Equal(expected, result["UserServiceClient.cs"], ignoreLineEndingDifferences: true);
    }
        
    [Fact]
    public void BuildsSdk_UserFull_Test()
    {
        var expectedClass = LoadExpected("LambdaService_UserFull");
        var expectedInterface = LoadExpected("LambdaService_UserFull_Interface");
        var expectedGetUserMessage = LoadExpected("LambdaService_GetUserMessage");
        var expectedCreateUserMessage = LoadExpected("LambdaService_CreateUserMessage");
        var expectedUserDto = LoadExpected("LambdaService_UserDto");
        var expectedInternalDto = LoadExpected("LambdaService_InternalDto");

        var dictionary = new Dictionary<string, (Type, Type, Type)>
        {
            { "user:get", (typeof(GetUserMessage), typeof(GetUserMessage), typeof(UserDto)) },
            { "user:create", (typeof(CreateUserMessage), typeof(CreateUserMessage), typeof(string)) }
        };

        var lambdaServiceSdkBuilder = new MessageClientSdkBuilder(UserServiceName, BaseNameSpace);

        var result = lambdaServiceSdkBuilder.Build(dictionary.ToEventServiceDocument());

        Assert.Equal(expectedClass, result["UserServiceClient.cs"], ignoreLineEndingDifferences: true);
        Assert.Equal(expectedInterface, result["IUserServiceClient.cs"], ignoreLineEndingDifferences: true);
        Assert.Equal(expectedGetUserMessage, result["GetUserMessage.cs"], ignoreLineEndingDifferences: true);
        Assert.Equal(expectedCreateUserMessage, result["CreateUserMessage.cs"], ignoreLineEndingDifferences: true);
        Assert.Equal(expectedCreateUserMessage, result["CreateUserMessage.cs"], ignoreLineEndingDifferences: true);
        Assert.Equal(expectedUserDto, result["UserDto.cs"], ignoreLineEndingDifferences: true);
        Assert.Equal(expectedInternalDto, result["InternalDto.cs"], ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void BuildsSdk_GetUserMessage_Test()
    {
        var expected = LoadExpected("LambdaService_GetUserMessage");

        var dictionary = new Dictionary<string, (Type, Type, Type)>
        {
            { "user:get", (typeof(GetUserMessage), typeof(GetUserMessage), typeof(UserDto)) }
        };

        var lambdaServiceSdkBuilder = new MessageClientSdkBuilder(UserServiceName, BaseNameSpace);

        var result = lambdaServiceSdkBuilder.Build(dictionary.ToEventServiceDocument());

        Assert.Equal(expected, result["GetUserMessage.cs"], ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void BuildsSdk_CreateUserMessage_Test()
    {
        var expected = LoadExpected("LambdaService_CreateUserMessage");

        var dictionary = new Dictionary<string, (Type, Type, Type)>
        {
            { "user:create", (typeof(CreateUserMessage), typeof(CreateUserMessage), typeof(Guid?)) }
        };

        var lambdaServiceSdkBuilder = new MessageClientSdkBuilder(UserServiceName, BaseNameSpace);

        var result = lambdaServiceSdkBuilder.Build(dictionary.ToMessageHandlerDefinitions());

        Assert.Equal(expected, result["CreateUserMessage.cs"], ignoreLineEndingDifferences: true);
    }

    [Fact(Skip = "Not supported")]
    public void BuildsSdk_UpdateUserMessage_Test()
    {
        var expected = LoadExpected("LambdaService_UpdateUserMessage");

        var dictionary = new Dictionary<string, (Type, Type, Type)>
        {
            { "user:update", (typeof(UpdateUserMessage), typeof(UpdateUserMessage), typeof(Guid?)) }
        };

        var lambdaServiceSdkBuilder = new MessageClientSdkBuilder(UserServiceName, BaseNameSpace);

        var result = lambdaServiceSdkBuilder.Build(dictionary.ToEventServiceDocument());

        Assert.Equal(expected, result["UpdateUserMessage.cs"], ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void BuildsSdk_TenantFull_Test()
    {
        var expectedClass = LoadExpected("LambdaService_TenantFull");
        var expectedInterface = LoadExpected("LambdaService_TenantFull_Interface");

        var dictionary = new Dictionary<string, (Type, Type, Type)>
        {
            { "tenant:get", (typeof(GetTenantMessage), typeof(GetTenantMessage), typeof(TenantDto)) },
            { "tenant:create", (typeof(CreateTenantMessage), typeof(CreateTenantMessage), typeof(TenantDto)) }
        };

        var lambdaServiceSdkBuilder = new MessageClientSdkBuilder(TenantServiceName, BaseNameSpace);

        var result = lambdaServiceSdkBuilder.Build(dictionary.ToEventServiceDocument());

        Assert.Equal(expectedClass, result["TenantServiceClient.cs"], ignoreLineEndingDifferences: true);
        Assert.Equal(expectedInterface, result["ITenantServiceClient.cs"], ignoreLineEndingDifferences: true);
    }
}
