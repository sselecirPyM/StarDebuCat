using SC2APIProtocol;
using StarDebuCat;
using StarDebuCat.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MilkWangBase
{
    public class InputSystem
    {
        public bool exitProgram = false;

        [Diffusion("Frame")]
        public ulong frame = 0;

        [Diffusion("ReadyToPlay")]
        public bool readyToPlay = false;

        public int port;
        public int gamePort;
        public string map;
        public Race Race;
        public bool ladderGame;

        public GameConnection gameConnection;

        public int playerId;


        public ResponseGameInfo gameInfo;
        public ResponseData gameData;
        public ResponseObservation observation;
        public Status status;

        void Update()
        {
            if (!readyToPlay && status == Status.Unset)
            {
                StartGame();
                gameData = gameConnection.RequestGameData();
                gameInfo = gameConnection.RequestGameInfo();
            }
            if (!readyToPlay)
                return;
            observation = gameConnection.RequestObservation();
            status = gameConnection.status;
            if (status == Status.Ended || status == Status.Quit)
            {
                foreach (var result in observation.PlayerResult)
                {
                    if (result.PlayerId == playerId)
                        Console.WriteLine("Result: {0}", result.Result);
                }
                readyToPlay = false;
                exitProgram = true;
                gameConnection.LeaveGame();
            }
            if (!readyToPlay)
                return;

            gameConnection.RequestStep(1);
            frame++;
        }

        public void StartGame()
        {
            gameConnection.Connect("127.0.0.1", gamePort);
            if (ladderGame)
            {
                JoinGameLadder(Race, port);
            }
            else
            {
                var player2 = new PlayerSetup
                {
                    Race = Race.Random,
                    Type = PlayerType.Computer,
                    Difficulty = Difficulty.VeryHard
                };
                gameConnection.NewGame(player2, map);
                JoinGame(Race);
            }
            readyToPlay = true;
        }

        void JoinGameLadder(Race race, int startPort)
        {
            var joinGame = new RequestJoinGame();
            joinGame.Race = race;

            joinGame.SharedPort = startPort + 1;
            joinGame.ServerPorts = new PortSet();
            joinGame.ServerPorts.GamePort = startPort + 2;
            joinGame.ServerPorts.BasePort = startPort + 3;

            joinGame.ClientPorts.Add(new PortSet());
            joinGame.ClientPorts[0].GamePort = startPort + 4;
            joinGame.ClientPorts[0].BasePort = startPort + 5;

            joinGame.Options = new InterfaceOptions
            {
                Raw = true,
                Score = true,
            };

            var request = new Request();
            request.JoinGame = joinGame;

            var response = gameConnection.Request(request);
            var responseJoinGame = response.JoinGame;
            if (responseJoinGame.Error != ResponseJoinGame.Types.Error.Unset)
            {
                throw new Exception(string.Format("{0} {1}", responseJoinGame.Error.ToString(), responseJoinGame.ErrorDetails));
            }

            playerId = (int)responseJoinGame.PlayerId;
        }

        void JoinGame(Race race)
        {
            var request = new Request
            {
                JoinGame = new RequestJoinGame
                {
                    Race = race,
                    Options = new InterfaceOptions
                    {
                        Raw = true,
                        Score = true,
                    }
                }
            };
            var response = gameConnection.Request(request);
            var responseJoinGame = response.JoinGame;

            if (responseJoinGame.Error != ResponseJoinGame.Types.Error.Unset)
            {
                throw new Exception(string.Format("Response error \ndetail:{0}", response.JoinGame.ErrorDetails));
            }
            playerId = (int)responseJoinGame.PlayerId;
        }
    }
}
