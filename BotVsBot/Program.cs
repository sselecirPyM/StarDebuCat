using StarDebuCat;
using StarDebuCat.Utility;
using System.Diagnostics;
using System.IO;

namespace BotVsBot
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CLArgs clArgs = new CLArgs();
            clArgs.port = 5678;
            clArgs.MapPath = "D:/StarCraft II/Maps/BerlingradAIE.SC2Map";
            clArgs.BotPath = "MilkWang1";

            Run(clArgs);
        }

        static void Run(CLArgs clArgs)
        {
            BotVsBot(clArgs);
            //HumanVsBot(clArgs);
        }

        static void HumanVsBot(CLArgs clArgs)
        {
            int port = clArgs.port;
            SC2GameHelp.GetSC2Path(out var exe, out var dir);
            Launch(dir, exe, port + 2);
            Launch(dir, exe, port + 4);

            GameConnection gameConnection = new GameConnection();
            gameConnection.Connect("127.0.0.1", port + 2);

            CreateGame(gameConnection, clArgs.MapPath, true);

            string path = Path.GetFullPath(clArgs.BotPath);

            //LaunchBot(path, clArgs.BotPath, port + 2, port);
            LaunchBot(path, clArgs.BotPath, port + 4, port);
            gameConnection.Request(new SC2APIProtocol.Request()
            {
                JoinGame = new SC2APIProtocol.RequestJoinGame()
                {
                    HostIp = "127.0.0.1",
                    Race = SC2APIProtocol.Race.Terran,
                    ServerPorts = new SC2APIProtocol.PortSet
                    {
                        GamePort = port + 2,
                        BasePort = port + 3
                    },
                    ClientPorts =
                    {
                        new SC2APIProtocol.PortSet()
                        {
                            GamePort = port + 4,
                            BasePort = port + 5
                        }
                    }
                }
            });

            gameConnection.Dispose();
        }

        static void BotVsBot(CLArgs clArgs)
        {
            int port = clArgs.port;
            SC2GameHelp.GetSC2Path(out var exe, out var dir);
            Launch(dir, exe, port + 2);
            Launch(dir, exe, port + 4);

            GameConnection gameConnection = new GameConnection();
            gameConnection.Connect("127.0.0.1", port + 2);

            CreateGame(gameConnection, clArgs.MapPath, false);
            gameConnection.Dispose();

            string path = Path.GetFullPath(clArgs.BotPath);

            LaunchBot(path, clArgs.BotPath, port + 2, port);
            LaunchBot(path, clArgs.BotPath, port + 4, port);
        }

        static void CreateGame(GameConnection gameConnection, string mapPath, bool realtime)
        {
            gameConnection.Request(new SC2APIProtocol.Request
            {
                CreateGame = new SC2APIProtocol.RequestCreateGame()
                {
                    Realtime = realtime,
                    LocalMap = new SC2APIProtocol.LocalMap()
                    {
                        MapPath = mapPath
                    },
                    PlayerSetups =
                    {
                        new SC2APIProtocol.PlayerSetup()
                        {
                            Type=SC2APIProtocol.PlayerType.Participant,
                        },
                        new SC2APIProtocol.PlayerSetup()
                        {
                            Type=SC2APIProtocol.PlayerType.Participant,
                        }
                    }
                }
            });
        }

        static void LaunchBot(string path, string name, int port, int startPort)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo()
            {
                WorkingDirectory = path,
                FileName = path + '/' + name,
                ArgumentList =
                {
                    "--OpponentId","MilkWang1",
                    "-g",port.ToString(),
                    "-o",startPort.ToString(),
                },
                UseShellExecute = true
            };
            Process.Start(processStartInfo);
        }
        static void Launch(string starcraftDir, string fileName, int port)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo()
            {
                ArgumentList =
                {
                    "-listen","127.0.0.1",
                    "-port", port.ToString(),
                    "-displayMode", "0"
                },
                FileName = fileName,
                WorkingDirectory = Path.Combine(starcraftDir, "Support64")
            };
            Process.Start(processStartInfo);
        }
    }
}