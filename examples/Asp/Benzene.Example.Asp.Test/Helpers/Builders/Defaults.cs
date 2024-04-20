namespace Benzene.Example.Asp.Test.Helpers.Builders;

public static class Defaults
{
    public const string Id = "aaa38217-37c5-4101-8cea-bbad5a3da000";
    public const string Id2 = "aaa38217-37c5-4101-8cea-bbad5a3da001";
    public const string UserId = "abc38217-37c5-4101-8cea-bbad5a3da501";
    public const string UserId2 = "def38217-37c5-4101-8cea-bbad5a3da502";
    public const string OrderId = "abc38217-37c5-4101-8cea-bbad5a3da511";

    public static class Order
    {
        public const string Id = "77968516-6225-442f-a032-a14d0d422044";
        public const string Id2 = "77968516-6225-442f-a032-a14d0d422045";
        public const string Status = "some-status";
        public const string Status2 = "some-other-status";
        public const string Name = "Aesop Stuff";
        public const string Name2 = "Socrates Stuff";
    }

    public static class ErrorStatus
    {
        public const string NotFound = "NotFound";
        public const string ValidationError = "ValidationError";
    }
}