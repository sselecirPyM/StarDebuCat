using CommandLine;
using MilkWangBase.Utility;

namespace MilkWang1;

internal class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            int port = 5678;

            args = new string[]
            {
                "-g",port.ToString(),
                "-o",port.ToString(),
                "--debug",
                "--repeat","1",
                "-m","BerlingradAIE.SC2Map",
                //"--TestStrategy", "test_tank"
            };
        }
        var parser = new Parser(settings => settings.CaseSensitive = false);
        parser.ParseArguments<CLArgs>(args).WithParsed(Run);

    }

    static void Run(CLArgs clArgs)
    {
        int repeatCount = clArgs.Repeat;
        while (repeatCount > 0)
        {
            repeatCount--;
            var controller = new BotController
            {
                CLArgs = clArgs,
            };
            controller.Initialize();
            while (!controller.exitProgram)
                controller.Update();
            controller.Dispose();
        }
    }
}