using CommandLine;
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
                "--Debug",
                "-m","BerlingradAIE.SC2Map"
            };
        }
        Parser.Default.ParseArguments<CLArgs>(args).WithParsed(Run);

    }

    static void Run(CLArgs clArgs)
    {
        var botData = JsonConvert.DeserializeObject<BotData>(File.ReadAllText("BotData/terran.json"),
            new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.All });
        var controller = new BotController
        {
            CLArgs = clArgs,
            botData = botData
        };
        controller.Initialize();
        while (!controller.exitProgram)
            controller.Update();
        controller.Update();
    }
}