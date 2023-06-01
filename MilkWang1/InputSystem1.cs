using SC2APIProtocol;
using StarDebuCat;
using System;

namespace MilkWang1;

public class InputSystem1
{
    public int port;
    public int gamePort;
    public string map;
    public Race Race;
    public bool isLadderGame;

    public string enemyId;

    public GameConnection gameConnection;

    public int playerId;


    public ResponseGameInfo gameInfo;
    public ResponseData gameData;
    public ResponseObservation observation;
    public Status status;
    public Result Result;
    public AIBuild ComputerAIBuild;

    #region computer
    public Difficulty ComputerDifficulty = Difficulty.VeryHard;
    public Race ComputerRace = Race.Random;
    #endregion

    public void Init2()
    {
        StartGame();
        gameData = gameConnection.RequestGameData();
        gameInfo = gameConnection.RequestGameInfo();
    }

    //public void Update2()
    //{
    //    observation = gameConnection.RequestObservation();
    //}

    public void StartGame()
    {
        gameConnection.Connect("127.0.0.1", gamePort);
        if (isLadderGame)
        {
            JoinGameLadder(Race, port);
        }
        else
        {
            var player1 = new PlayerSetup
            {
                Type = PlayerType.Participant
            };
            var player2 = new PlayerSetup
            {
                Race = ComputerRace,
                Type = PlayerType.Computer,
                Difficulty = ComputerDifficulty,
                AiBuild = ComputerAIBuild
            };

            //var player1 = new PlayerSetup
            //{
            //    Type = PlayerType.Participant
            //};
            //var player2 = new PlayerSetup
            //{
            //    Type = PlayerType.Participant
            //};
            gameConnection.NewGame(player1, player2, map);
            JoinGameLocal(Race);
        }
    }

    void JoinGameLadder(Race race, int startPort)
    {
        var joinGame = new RequestJoinGame
        {
            Race = race,
            Options = new InterfaceOptions
            {
                Raw = true,
                Score = true,
            },

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
        };

        var responseJoinGame = gameConnection.Request(joinGame);
        if (responseJoinGame.ShouldSerializeerror())
        {
            throw new Exception(string.Format("{0} {1}", responseJoinGame.error.ToString(), responseJoinGame.ErrorDetails));
        }

        playerId = (int)responseJoinGame.PlayerId;
    }

    void JoinGameLocal(Race race)
    {
        var joinGame = new RequestJoinGame
        {
            Race = race,
            Options = new InterfaceOptions
            {
                Raw = true,
                Score = true,
            }
        };

        var responseJoinGame = gameConnection.Request(joinGame);
        if (responseJoinGame.ShouldSerializeerror())
        {
            throw new Exception(string.Format("Response error \ndetail:{0}", responseJoinGame.ErrorDetails));
        }
        playerId = (int)responseJoinGame.PlayerId;
    }
}
