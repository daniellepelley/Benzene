using Benzene.Core.DI;
using Benzene.TempCore.DI;
using Xunit;

namespace Benzene.Test.Core.Core.DI;

public class RegistrationCheckTest
{
    [Fact]
    public void RegistrationChecksDeduplicate()
    {
        var result = RegistrationCheck.Create(typeof(Benzene.Azure.Kafka.KafkaRegistrations), typeof(Benzene.Azure.Kafka.KafkaRegistrations));
        Assert.NotNull(result);
    }
}
