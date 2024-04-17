using System;
using System.Collections.Generic;
using System.IO;
using Benzene.CodeGen.Client;
using Benzene.Test.Autogen.CodeGen.Helpers;
using Benzene.Test.Autogen.CodeGen.Model;
using Xunit;

namespace Benzene.Test.Autogen.CodeGen.Service;

public class MessageHandlerBuilderTest
{
    private const string BaseNameSpace = "Platform.Service.Clients.User";

    private string LoadExpected(string fileName) => File.ReadAllText($"{Directory.GetCurrentDirectory()}/Autogen/CodeGen/Service/Examples/{fileName}.txt");

    [Fact]
    public void Builds_MessageHandler_UserGet_Test()
    {
        var expected = LoadExpected("LambdaService_UserGet");
        var expectedGetUserMessage = LoadExpected("LambdaService_GetUserMessage");
        var expectedUserDto = LoadExpected("LambdaService_UserDto");

        var dictionary = new Dictionary<string, (Type, Type, Type)>
        {
            { "user:get", (typeof(GetUserMessage), typeof(GetUserMessage), typeof(UserDto)) }
        };

        var lambdaServiceSdkBuilder = new MessageHandlerBuilder(BaseNameSpace);

        var result = lambdaServiceSdkBuilder.Build(dictionary.ToEventServiceDocument());

        Assert.Equal(expected, result["UserGetMessageHandler.cs"], ignoreLineEndingDifferences: true);
        Assert.Equal(expectedGetUserMessage, result["GetUserMessage.cs"], ignoreLineEndingDifferences: true);
        Assert.Equal(expectedUserDto, result["UserDto.cs"], ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Builds_MessageHandler_UserFull_Test()
    {
        var expectedGetUserMessageHandler = LoadExpected("LambdaService_UserGet");
        var expectedCreateUserMessageHandler = LoadExpected("LambdaService_UserCreate");
        var expectedGetUserMessage = LoadExpected("LambdaService_GetUserMessage");
        var expectedCreateUserMessage = LoadExpected("LambdaService_CreateUserMessage");
        var expectedUserDto = LoadExpected("LambdaService_UserDto");
        var expectedInternalDto = LoadExpected("LambdaService_InternalDto");
    
        var dictionary = new Dictionary<string, (Type, Type, Type)>
        {
            { "user:get", (typeof(GetUserMessage), typeof(GetUserMessage), typeof(UserDto)) },
            { "user:create", (typeof(CreateUserMessage), typeof(CreateUserMessage), typeof(string)) }
        };
    
        var lambdaServiceSdkBuilder = new MessageHandlerBuilder(BaseNameSpace);
    
        var result = lambdaServiceSdkBuilder.Build(dictionary.ToEventServiceDocument());

        Assert.Equal(expectedGetUserMessageHandler, result["UserGetMessageHandler.cs"], ignoreLineEndingDifferences: true);
        Assert.Equal(expectedCreateUserMessageHandler, result["UserCreateMessageHandler.cs"], ignoreLineEndingDifferences: true);
        Assert.Equal(expectedGetUserMessage, result["GetUserMessage.cs"], ignoreLineEndingDifferences: true);
        Assert.Equal(expectedCreateUserMessage, result["CreateUserMessage.cs"], ignoreLineEndingDifferences: true);
        Assert.Equal(expectedCreateUserMessage, result["CreateUserMessage.cs"], ignoreLineEndingDifferences: true);
        Assert.Equal(expectedUserDto, result["UserDto.cs"], ignoreLineEndingDifferences: true);
        Assert.Equal(expectedInternalDto, result["InternalDto.cs"], ignoreLineEndingDifferences: true);
    }
}
