namespace Benzene.Extras.Patches;

public interface IPatchMessage
{
    IList<string> UpdatedFields { get; }
}
