using Benzene.Extras.Patches;

namespace Benzene.Test.Examples;

public class ExamplePatchPayload : PatchMessage
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
}
