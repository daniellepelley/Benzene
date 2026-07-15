using System;
using System.IO;
using System.Threading.Tasks;
using Benzene.CodeGen.Core;
using Xunit;

namespace Benzene.Test.Autogen.CodeGen.Core;

public class CodeFileWriterTest
{
    private class TestCodeFile : ICodeFile
    {
        public string Name { get; set; }
        public string[] Lines { get; set; }
    }

    [Fact]
    public async Task CreateAsync_DirectoryDoesNotExist_CreatesItAndWritesEachFile()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), "benzene-codefilewriter-test-" + Guid.NewGuid());
        try
        {
            var codeFiles = new ICodeFile[]
            {
                new TestCodeFile { Name = "Foo.cs", Lines = new[] { "namespace Foo;", "public class Foo { }" } },
                new TestCodeFile { Name = "Bar.cs", Lines = new[] { "namespace Bar;", "public class Bar { }" } }
            };

            await new CodeFileWriter().CreateAsync(codeFiles, directoryPath);

            Assert.True(Directory.Exists(directoryPath));
            Assert.Equal(new[] { "namespace Foo;", "public class Foo { }" }, await File.ReadAllLinesAsync(Path.Combine(directoryPath, "Foo.cs")));
            Assert.Equal(new[] { "namespace Bar;", "public class Bar { }" }, await File.ReadAllLinesAsync(Path.Combine(directoryPath, "Bar.cs")));
        }
        finally
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }
}
