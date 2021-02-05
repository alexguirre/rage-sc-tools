namespace ScTools
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Builder;
    using System.CommandLine.Help;
    using System.CommandLine.Invocation;
    using System.CommandLine.Parsing;
    using System.CommandLine.IO;
    using System.Linq;

    using ScTools.Cli;

    public sealed class CommandManager
    {
        private readonly IConsole console;
        private readonly ScriptManager scriptMgr;
        private readonly Command rootCommand;
        private readonly Parser parser;
        private bool running = true;

        public CommandManager(ScriptManager scriptManager)
        {
            console = new SystemConsole();
            scriptMgr = scriptManager;
            rootCommand = BuildCommands();
            parser = new CommandLineBuilder(rootCommand).Build();
        }

        private Command BuildCommands()
        {
            var exit = new Command("exit") { };
            exit.Handler = CommandHandler.Create(() => running = false);

            var help = new Command("help")
            {
                new Argument<string?>("command", () => null)
            };
            help.Handler = CommandHandler.Create<string>(Command_Help);

            var list = new Command("list", "List registered scripts") { };
            list.Handler = CommandHandler.Create(Command_List);

            var listThreads = new Command("list-threads", "List executing script threads") { };
            listThreads.Handler = CommandHandler.Create(Command_ListThreads);

            var register = new Command("register", "Register external scripts")
            {
                new Argument<FileGlob[]>(
                    "scripts",
                    "The input YSC files. Supports glob patterns.")
                    .AtLeastOne(),
            };
            register.Handler = CommandHandler.Create<FileGlob[]>(Command_Register);

            var unregister = new Command("unregister", "Unregister a script")
            {
                new Argument<string>("script"),
            };
            unregister.Handler = CommandHandler.Create<string>(Command_Unregister);

            var start = new Command("start", "Start a new script thread")
            {
                new Argument<string>("script"),
                new Argument<uint>("stack-size"),
            };
            start.Handler = CommandHandler.Create<string, uint>(Command_Start);

            var kill = new Command("kill", "Kill a script thread")
            {
                new Argument<uint>("thread-id"),
            };
            kill.Handler = CommandHandler.Create<uint>(Command_Kill);

            return new Command(">")
            {
                exit, help, list, listThreads, register, unregister, start, kill
            };
        }

        private void WriteLine(string text) => console.Out.WriteLine(text);

        public void MainLoop()
        {
            while (running)
            {
                var cmd = Console.ReadLine();
                if (cmd != null)
                {
                    parser.Invoke(cmd, console);
                }
            }
        }

        private void Command_Help(string? command)
        {
            var helpBuilder = new RootHelpBuilder(console);

            if (command is not null && rootCommand.Children.GetByAlias(command) is ICommand cmd)
            {
                helpBuilder.Write(cmd);
            }
            else
            {
                helpBuilder.Write(rootCommand);
            }
        }

        private void Command_List()
        {
            foreach (var (name, i) in scriptMgr.EnumerateRegisteredScripts())
            {
                WriteLine($"\t{name}\t{i}");
            }
        }

        private void Command_ListThreads()
        {
            WriteLine("\tNOT IMPLEMENTED");
        }

        private void Command_Register(FileGlob[] scripts)
        {
            foreach (var f in scripts.SelectMany(glob => glob.Matches))
            {
                scriptMgr.RegisterScript(f);
            }
        }

        private void Command_Unregister(string script)
        {
            WriteLine("\tNOT IMPLEMENTED");
        }

        private void Command_Start(string script, uint stackSize)
        {
            WriteLine("\tNOT IMPLEMENTED");
        }

        private void Command_Kill(uint threadId)
        {
            WriteLine("\tNOT IMPLEMENTED");
        }

        private sealed class RootHelpBuilder : HelpBuilder
        {
            public RootHelpBuilder(IConsole console, int? columnGutter = null, int? indentationSize = null, int? maxWidth = null) : base(console, columnGutter, indentationSize, maxWidth)
            {
            }

            public override void Write(ICommand command)
            {
                Indent();
                base.Write(command);
                Outdent();
            }

            protected override void AddUsage(ICommand command)
            {
                if (command.Parents.Count == 0)
                {
                    // empty to remove usage section of root command
                }
                else
                {
                    base.AddUsage(command);
                }
            }
        }
    }
}
