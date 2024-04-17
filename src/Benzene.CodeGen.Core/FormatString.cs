namespace Benzene.CodeGen.Core;

public struct FormatString
{
    public FormatString(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
