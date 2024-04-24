using CommandLine;
using StarDebuCat;
using StarDebuCat.Utility;
using SC2APIProtocol;

namespace MilkWang2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                int port = 5678;

                args = new string[]
                {
                    "--GamePort",port.ToString(),
                    "--StartPort",port.ToString(),
                    "--debug",
                    "--repeat","1",
                    "-m","HardLead512V2AIE.SC2Map",
                        //"--AIBuild", "Timing",
                        //"--TestStrategy", "test_tank"
                };
            }
            var parser = new Parser(settings => settings.CaseSensitive = false);
            parser.ParseArguments<CLArgs>(args).WithParsed(Run);
        }


        static void Run(CLArgs clArgs)
        {
            int repeatCount = clArgs.Repeat;
            if (clArgs.Debug)
            {
                SC2GameHelp.LaunchSC2(clArgs.GamePort, out var maps);
            }
            int startPort = clArgs.StartPort;

            GameConnectionFSM2 gameConnection = new GameConnectionFSM2();
            gameConnection.Connect(clArgs.LadderServer, clArgs.GamePort);

            while (repeatCount > 0)
            {
                repeatCount--;
                gameConnection.responseProcessor = new BotController() { GameConnection = gameConnection };

                if (clArgs.Debug)
                {
                    gameConnection.SendMessage(new Request()
                    {
                        CreateGame = new RequestCreateGame()
                        {
                            LocalMap = new LocalMap()
                            {
                                MapPath = clArgs.Map
                            },
                            PlayerSetups = {
                                new PlayerSetup()
                                {
                                    Type = PlayerType.Participant,
                                },
                                new PlayerSetup()
                                {
                                    Type = PlayerType.Computer,
                                    Difficulty = Difficulty.VeryHard,
                                    Race = Race.Random,
                                },
                            }
                        }
                    });
                    gameConnection.responseProcessor.StartGame(new RequestJoinGame()
                    {

                    });
                }
                else
                {
                    gameConnection.responseProcessor.StartGame(new RequestJoinGame()
                    {
                        SharedPort = startPort + 1,
                        ServerPorts = new PortSet
                        {
                            GamePort = startPort + 2,
                            BasePort = startPort + 3
                        },
                        ClientPorts =
                        {
                            new PortSet()
                            {
                                GamePort = startPort + 4,
                                BasePort = startPort + 5
                            }
                        }
                    });
                }
                gameConnection.FSM();
            }
        }
    }
}
