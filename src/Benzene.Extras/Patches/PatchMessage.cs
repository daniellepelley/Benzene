namespace Benzene.Extras.Patches;

public class PatchMessage : IPatchMessage
{
    public IList<string> UpdatedFields { get; } = new List<string>();
}
