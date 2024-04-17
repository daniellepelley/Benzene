namespace Benzene.CodeGen.Core;

public interface ICodeBuilder<in T>
{
    ICodeFile[] BuildCodeFiles(T source);
}
