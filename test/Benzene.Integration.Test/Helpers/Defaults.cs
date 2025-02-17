namespace Benzene.Integration.Test.Helpers;

public static class Defaults
{
    public const string Topic = "example";
    public const string TopicNoResponse = "example-no-response";
    public const string TopicWithGuid = "example-guid";
    public const string Path = "/example";
    public const string PathNoResponse = "/example-no-response";
    public const string Method = "GET";
    public const string Version2 = "2.0";
    public const string HealthCheckTopic = "healthcheck";
    public const int Id = 42;
    public const string Name = "some-name";
    public static object MessageAsObject = new { name = "some-name"};
    public const string Message = "{\"name\":\"some-name\"}";
    public const string ResponseMessage = "{\"id\":42,\"name\":\"some-name\",\"mapped\":null}";
    public const string LambdaName = "some-lambda";
    public const string SqsQueueUrl = "some-url";
    public const string StateMachineArn = "some-arn";

    public const string RbacApplication = "elements";
    public const string RbacResource = "example";
    public const string RbacAction = "get";
    public const string RbacAction2 = "get2";
    public const string TenantId = "some-tenant-id";
    public const string UserId = "some-user-id";
}

 
