namespace ScTools
{
    using System;
    using System.Collections.Concurrent;
    using System.CommandLine;
    using System.CommandLine.Builder;
    using System.CommandLine.Help;
    using System.CommandLine.Invocation;
    using System.CommandLine.Parsing;
    using System.IO;
    using System.Linq;

    using ScTools.Cli;
    using ScTools.Five;

    public sealed class CommandManager
    {
        private readonly ScriptManager scriptMgr;
        private readonly Command rootCommand;
        private readonly Parser parser;
        private bool running = true;

        public CommandManager(ScriptManager scriptManager)
        {
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
            help.Handler = CommandHandler.Create<IConsole, string>(Command_Help);

            var print = new Command("print")
            {
                new Argument<string>("text"),
            };
            print.Handler = CommandHandler.Create((string text) => Console.WriteLine(">> " + text));

            var register = new Command("register", "Register external scripts")
            {
                new Argument<FileGlob[]>(
                    "scripts",
                    "The input YSC files. Supports glob patterns.")
                    .AtLeastOne(),
            };
            register.Handler = CommandHandler.Create<FileGlob[]>(Command_Register);

            var list = new Command("list", "List registered scripts") { };
            list.Handler = CommandHandler.Create(Command_List);

            return new Command(">")
            {
                exit, help, print, register, list
            };
        }

        public void MainLoop()
        {
            while (running)
            {
                var cmd = Console.ReadLine();
                if (cmd != null)
                {
                    parser.Invoke(cmd);
                }
            }
        }

        private void Command_Help(IConsole console, string? command)
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

        private void Command_Register(FileGlob[] scripts)
        {
            foreach (var f in scripts.SelectMany(glob => glob.Matches))
            {
                scriptMgr.RegisterScript(f);
            }
        }

        private void Command_List()
        {
            if (!Util.IsInGame)
            {
                Console.WriteLine("Not in-game");
                return;
            }

            ref var scripts = ref CStreamedScripts.Instance;
            var size = scripts.GetSize();
            for (int i = 0; i < size; i++)
            {
                var name = CStreamedScripts.Instance.GetAssetName(i);

                Console.WriteLine($"#{i} -> {name}");
            }
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
