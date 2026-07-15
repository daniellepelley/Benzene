using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Benzene.CodeGen.Cli.Core;
using Benzene.CodeGen.Cli.Core.Parsing;
using Moq;
using Xunit;

namespace Benzene.Test.Cli.Core
{
    public class HelpCommandTest
    {
        private static Mock<ICommand> CreateCommand(string name, string description, string help)
        {
            var command = new Mock<ICommand>();
            command.Setup(x => x.Name).Returns(name);
            command.Setup(x => x.Description).Returns(description);
            command.Setup(x => x.GetHelp()).Returns(help);
            return command;
        }

        [Fact]
        public async Task ExecuteAsync_NoValue_ListsEveryCommandNameAndDescription()
        {
            var command = CreateCommand("build", "Builds the client code", "build help text");
            var helpCommand = new HelpCommand(new[] { command.Object });
            var args = new CommandArguments { Name = "help", Value = null, Attributes = new Dictionary<string, string?>() };

            var originalOut = Console.Out;
            using var capturedOut = new StringWriter();
            Console.SetOut(capturedOut);
            try
            {
                await helpCommand.ExecuteAsync(args);
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            var output = capturedOut.ToString();
            Assert.Contains("build", output);
            Assert.Contains("Builds the client code", output);
        }

        [Fact]
        public async Task ExecuteAsync_ValueMatchesACommand_PrintsThatCommandsHelp()
        {
            var command = CreateCommand("build", "Builds the client code", "build help text");
            var helpCommand = new HelpCommand(new[] { command.Object });
            var args = new CommandArguments { Name = "help", Value = "build", Attributes = new Dictionary<string, string?>() };

            var originalOut = Console.Out;
            using var capturedOut = new StringWriter();
            Console.SetOut(capturedOut);
            try
            {
                await helpCommand.ExecuteAsync(args);
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            Assert.Contains("build help text", capturedOut.ToString());
        }

        [Fact]
        public async Task ExecuteAsync_ValueDoesNotMatchAnyCommand_WritesErrorInsteadOfThrowing()
        {
            var helpCommand = new HelpCommand(Array.Empty<ICommand>());
            var args = new CommandArguments { Name = "help", Value = "does-not-exist", Attributes = new Dictionary<string, string?>() };

            var originalError = Console.Error;
            using var capturedError = new StringWriter();
            Console.SetError(capturedError);
            try
            {
                await helpCommand.ExecuteAsync(args);
            }
            finally
            {
                Console.SetError(originalError);
            }

            Assert.Contains("does-not-exist", capturedError.ToString());
        }
    }
}
