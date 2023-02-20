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
                "--Debug",
                "-m","BerlingradAIE.SC2Map"
            };
        }
        Parser.Default.ParseArguments<CLArgs>(args).WithParsed(Run);

    }

    static void Run(CLArgs clArgs)
    {
        var controller = new BotController
        {
            CLArgs = clArgs,
        };
        controller.Initialize();
        while (!controller.exitProgram)
            controller.Update();
        controller.Update();
    }
}