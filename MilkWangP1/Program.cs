using CommandLine;
using MilkWangBase.Core;
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
                "-m","BerlingradAIE.SC2Map"
            };
        }
        Parser.Default.ParseArguments<CLArgs>(args).WithParsed(Run);
    }

    static void Run(CLArgs clArgs)
    {
        var controller = new BotController();
        Fusion fusion;

        fusion = new Fusion(controller);
        var inputSystem = controller.inputSystem;
        inputSystem.ladderGame = true;
        inputSystem.Race = SC2APIProtocol.Race.Protoss;
        inputSystem.port = clArgs.StartPort;
        inputSystem.gamePort = clArgs.GamePort;
        if (clArgs.Map != null)
        {
            StarDebuCat.Utility.SC2GameHelp.LaunchSC2(clArgs.StartPort, out var maps);
            inputSystem.map = maps + "/" + clArgs.Map;
            inputSystem.ladderGame = false;
        }
        fusion.InitializeSystems();

        while (!controller.inputSystem.exitProgram)
        {
            fusion.Update();
        }
        fusion.Dispose();
    }
}