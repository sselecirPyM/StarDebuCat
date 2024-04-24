using MilkWang2.Simulation;
using SC2APIProtocol;
using StarDebuCat;

namespace MilkWang2
{
    public class BotController : ResponseProcessor
    {
        uint playerId;

        UnitManager unitManager = new UnitManager();
        PathManager pathManager = new PathManager();
        CommandManager commandManager = new CommandManager();

        public override void StartGame(RequestJoinGame baseRequest)
        {
            commandManager.unitManager = unitManager;
            baseRequest.Race = Race.Terran;
            baseRequest.Options = new InterfaceOptions()
            {
                Raw = true,
                Score = true,
            };
            SendMessage(new Request()
            {
                JoinGame = baseRequest
            });
        }

        public override void OnResponseJoinGame(ResponseJoinGame responseJoinGame)
        {
            playerId = responseJoinGame.PlayerId;

            SendMessage(new Request()
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
            SendMessage(new Request()
            {
                GameInfo = new RequestGameInfo()
            });
            Next();
        }

        public override void OnResponseData(ResponseData responseData)
        {
            unitManager.Init(responseData);
        }

        public override void OnResponseGameInfo(ResponseGameInfo responseGameInfo)
        {
            pathManager.Init(responseGameInfo);
        }

        public override void OnResponseObservation(ResponseObservation responseObservation)
        {
            if (ExitGameCheck(responseObservation))
                return;
            unitManager.Update(responseObservation);

            commandManager.SendCommand(GameConnection);
            Next();
        }

        bool ExitGameCheck(ResponseObservation responseObservation)
        {
            if (responseObservation.PlayerResults.Count > 0)
            {
                foreach (var playerResult in responseObservation.PlayerResults)
                {
                    if (playerResult.PlayerId == playerId)
                    {
                        Console.WriteLine($"result: {playerResult.Result}");
                    }
                }
                return true;
            }
            return false;
        }

        void Next()
        {
            SendMessage(new Request()
            {
                Step = new RequestStep()
                {
                    Count = 1
                }
            });
            SendMessage(new Request()
            {
                Observation = new RequestObservation()
            });
        }
    }
}
