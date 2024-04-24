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

    public GameConnectionFSM gameConnection;

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
        gameConnection.OnResponseGameInfo += OnResponseGameInfo;
        gameConnection.OnResponseData += OnResponseData;
        gameConnection.SendMessage(new Request()
        {
            Data = new RequestData()
            {
                AbilityId = true,
                BuffId = true,
                EffectId = true,
                UnitTypeId = true,
                UpgradeId = true,
            }
        });
        gameConnection.SendMessage(new Request()
        {
            GameInfo = new RequestGameInfo()
        });
    }

    private void OnResponseData(ResponseData obj)
    {
        gameConnection.OnResponseData -= OnResponseData;
        this.gameData = obj;
    }

    private void OnResponseGameInfo(ResponseGameInfo obj)
    {
        gameConnection.OnResponseGameInfo -= OnResponseGameInfo;
        this.gameInfo = obj;
    }

    public void StartGame()
    {
        gameConnection.Connect("127.0.0.1", gamePort);
        gameConnection.OnResponseJoinGame += OnResponseJoinGame;
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

            gameConnection.SendMessage(new RequestCreateGame()
            {
                LocalMap = new LocalMap()
                {
                    MapPath = map
                },
                PlayerSetups =
                {
                    player1,
                    player2
                }
            });
            JoinGameLocal(Race);
        }
    }

    private void OnResponseJoinGame(ResponseJoinGame responseJoinGame)
    {
        gameConnection.OnResponseJoinGame -= OnResponseJoinGame;
        if (responseJoinGame.ShouldSerializeerror())
        {
            throw new Exception(string.Format("{0} {1}", responseJoinGame.error.ToString(), responseJoinGame.ErrorDetails));
        }
        playerId = (int)responseJoinGame.PlayerId;
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

        gameConnection.SendMessage(new Request() { JoinGame = joinGame });
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

        gameConnection.SendMessage(new Request() { JoinGame = joinGame });
    }
}
