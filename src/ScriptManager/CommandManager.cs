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

    internal sealed class CommandManager
    {
        private const string Prompt = ">";

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
            parser = new CommandLineBuilder(rootCommand)
                            .UseTypoCorrections()
                            .UseParseErrorReporting()
                            .UseExceptionHandler()
                            .Build();

            scriptMgr.Output += OnScriptManagerOutput;
        }

        private Command BuildCommands()
        {
            /*var exit = new Command("exit") { };
            exit.Handler = CommandHandler.Create(Command_Exit);

            var help = new Command("help")
            {
                new Argument<string?>("command", () => null)
            };
            help.Handler = CommandHandler.Create<string>(Command_Help);

            var list = new Command("list", "List registered script programs.") { };
            list.Handler = CommandHandler.Create(Command_List);

            var listThreads = new Command("list-threads", "List executing script threads.") { };
            listThreads.Handler = CommandHandler.Create(Command_ListThreads);

            var listStacks = new Command("list-stacks", "List script stacks.") { };
            listStacks.Handler = CommandHandler.Create(Command_ListStacks);

            var register = new Command("register", "Register external scripts.")
            {
                new Argument<FileGlob[]>(
                    "scripts",
                    "The input YSC files. Supports glob patterns.")
                    .AtLeastOne(),
            };
            register.Handler = CommandHandler.Create<FileGlob[]>(Command_Register);

            var unregister = new Command("unregister", "Unregister a script.")
            {
                new Argument<string>("script", "The name of the script to unregister."),
            };
            unregister.Handler = CommandHandler.Create<string>(Command_Unregister);

            var start = new Command("start", "Start a new script thread.")
            {
                new Argument<string>("script"),
                new Argument<uint>("stack-size"),
            };
            start.Handler = CommandHandler.Create<string, uint>(Command_Start);

            var kill = new Command("kill", "Kill a script thread.")
            {
                new Argument<uint>("thread-id"),
            };
            kill.Handler = CommandHandler.Create<uint>(Command_Kill);*/

            return new Command(Prompt)
            {
               /* exit, help, list, listThreads, listStacks, register, unregister, start, kill*/
            };
        }

        private void Write(string text) => console.Out.Write(text);
        private void WriteLine(string text) => console.Out.WriteLine(text);

        public void MainLoop()
        {
            while (running)
            {
                scriptMgr.WaitForJobs();

                Write(Prompt);
                Write(" ");
                var cmd = Console.ReadLine();
                if (cmd != null)
                {
                    parser.Invoke(cmd, console);
                }
            }
        }

        private void OnScriptManagerOutput(string s)
        {
            WriteLine("\t" + s);
        }

        private void Command_Exit() => running = false;

        private void Command_Help(string? command)
        {
            /*var helpBuilder = new RootHelpBuilder(console);

            if (command is not null && rootCommand.Children.GetByAlias(command) is ICommand cmd)
            {
                helpBuilder.Write(cmd);
            }
            else
            {
                helpBuilder.Write(rootCommand);
            }*/
        }

        private void Command_List()
        {
            WriteLine($"\tName\tNum Refs\tLoaded");
            foreach (var (name, numRefs, loaded) in scriptMgr.EnumerateRegisteredScripts())
            {
                WriteLine($"\t{name}\t{numRefs}\t{loaded}");
            }
        }

        private void Command_ListThreads()
        {
            WriteLine($"\tID\tProgram ID (Name)\tState");
            foreach (var thread in scriptMgr.EnumerateScriptThreads())
            {
                WriteLine($"\t{thread.ThreadId}\t{thread.ProgramId} ({thread.ProgramName})\t{thread.State}");
            }
        }

        private void Command_ListStacks()
        {
            WriteLine($"\tSize\tAvailable");
            foreach (var stackGroup in scriptMgr.EnumerateScriptStacks()
                                                .GroupBy(s => s.Size)
                                                .OrderBy(g => g.Key))
            {
                var available = stackGroup.Count(s => !s.Used);
                var total = stackGroup.Count();
                WriteLine($"\t{stackGroup.Key}\t{available}/{total}");
            }
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
            scriptMgr.UnregisterScript(script);
        }

        private void Command_Start(string script, uint stackSize)
        {
            scriptMgr.StartThread(script, stackSize);
        }

        private void Command_Kill(uint threadId)
        {
            scriptMgr.KillThread(threadId);
        }

        /*private sealed class RootHelpBuilder : HelpBuilder
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
        }*/
    }
}
