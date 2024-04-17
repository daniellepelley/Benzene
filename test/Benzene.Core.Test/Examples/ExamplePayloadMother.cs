using System.Linq;

namespace Benzene.Test.Examples;

public static class ExamplePayloadMother
{
    public static ExampleRequestPayload[] Create(int number)
    {
        return Enumerable.Range(0, number).Select(x =>
            new ExampleRequestPayload
            {
                Id = x,
                Name = $"name-{x}"
            }).ToArray();
    }
}
