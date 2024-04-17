using Benzene.Core.DI;
using Xunit;

namespace Benzene.Test.Core.Core.DI;

public class RegistrationCheckTest
{
    [Fact]
    public void RegistrationChecksDeduplicate()
    {
        var result = RegistrationCheck.Create(typeof(Benzene.Azure.Core.Kafka.KafkaRegistrations), typeof(Benzene.Azure.Core.Kafka.KafkaRegistrations));
        Assert.NotNull(result);
    }
}
