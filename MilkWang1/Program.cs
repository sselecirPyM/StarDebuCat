using StarDebuCat.Core;
using StarDebuCat.Utility;
using System;
namespace MilkWang1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var controller = new BotController();
            Fusion fusion;
            if (args.Length == 0)
            {
                int port = 5678;

                SC2GameHelp.LaunchSC2(port, out var maps);
                fusion = new Fusion(controller);
                var inputSystem = controller.inputSystem;
                inputSystem.port = port;
                inputSystem.gamePort = port;
                inputSystem.map = maps + "/" + "JagannathaAIE.SC2Map";
                inputSystem.Race = SC2APIProtocol.Race.Terran;
            }
            else
            {
                CLArgs clArgs = new CLArgs(args);

                fusion = new Fusion(controller);
                var inputSystem = controller.inputSystem;
                inputSystem.ladderGame = true;
                inputSystem.Race = SC2APIProtocol.Race.Terran;
                inputSystem.port = clArgs.StartPort;
                inputSystem.gamePort = clArgs.GamePort;
            }

            fusion.InitializeSystems();

            while (!controller.inputSystem.exitProgram)
            {
                fusion.Update();
            }
            fusion.Dispose();
        }
    }
}