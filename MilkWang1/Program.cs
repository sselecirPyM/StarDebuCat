using CommandLine;
using MilkWangBase.Core;
using MilkWangBase.Utility;
using Newtonsoft.Json;
using System.IO;

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
        var botData = JsonConvert.DeserializeObject<BotData>(File.ReadAllText("BotData/terran.json"),
            new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.All });
        var controller = new BotController();
        Fusion fusion;

        fusion = new Fusion(controller);
        controller.terranBot1.BotData = botData;
        controller.battleSystem.BotData = botData;
        controller.buildSystem.BotData = botData;
        var inputSystem = controller.inputSystem;
        inputSystem.ladderGame = true;
        inputSystem.Race = SC2APIProtocol.Race.Terran;
        inputSystem.port = clArgs.StartPort;
        inputSystem.gamePort = clArgs.GamePort;
        if (clArgs.Map != null)
        {
            StarDebuCat.Utility.SC2GameHelp.LaunchSC2(clArgs.StartPort, out var maps);
            inputSystem.map = maps + "/" + clArgs.Map;
            inputSystem.ladderGame = false;
            inputSystem.ComputerDifficulty = clArgs.ComputerDifficulty;
            inputSystem.ComputerRace = clArgs.ComputerRace;
        }
        fusion.InitializeSystems();

        while (!controller.inputSystem.exitProgram)
        {
            fusion.Update();
        }
        fusion.Dispose();

    }
}