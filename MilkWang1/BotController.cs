using MilkWangBase.Core;
using MilkWangBase.Utility;
using StarDebuCat;
using StarDebuCat.Data;
using System;
using System.Runtime.InteropServices;
using static MilkWang1.Util;

namespace MilkWang1;


public class BotController : IDisposable
{
    public enum AppState
    {
        Initialize = 0,
        WaitForInput,
        Exit,
        Running,
    }
    public GameConnection gameConnection;

    public CLArgs CLArgs;
    public BotSubController subController;
    Fusion fusion;

    public bool exitProgram;

    public AppState appState;

    public void Initialize()
    {
        //File.WriteAllText("requirement.json", JsonConvert.SerializeObject(StarDebuCat.Data.DData.UpgradeRequirements,new JsonSerializerSettings { Converters = { new StringEnumConverter()},DefaultValueHandling=DefaultValueHandling.Ignore }));

        var botData = GetData<BotData>("BotData/terran.json");
        var gameData = GetData<GameData>("GameData/GameData.json");
        gameConnection = new GameConnection();
        subController = new BotSubController();
        var inputSystem = subController.inputSystem = new InputSystem1();

        inputSystem.isLadderGame = true;
        inputSystem.Race = (SC2APIProtocol.Race)botData.race;
        inputSystem.port = CLArgs.StartPort;
        inputSystem.gamePort = CLArgs.GamePort;
        inputSystem.enemyId = CLArgs.OpponentId;
        if (CLArgs.Map != null)
        {
            string maps = CLArgs.MapDir;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && CLArgs.MapDir == null)
                StarDebuCat.Utility.SC2GameHelp.LaunchSC2(CLArgs.StartPort, out maps);


            inputSystem.map = maps + "/" + CLArgs.Map;
            inputSystem.isLadderGame = false;
            inputSystem.ComputerDifficulty = CLArgs.ComputerDifficulty;
            inputSystem.ComputerRace = CLArgs.ComputerRace;
            inputSystem.ComputerAIBuild = CLArgs.AIBuild;
        }
        inputSystem.gameConnection = gameConnection;

        fusion = new Fusion(subController);
        fusion.AddData(botData);
        fusion.AddData(gameData);
        fusion.AddData(gameConnection);
        ;
        subController.debugSystem.enable = CLArgs.Debug;

        fusion.InitializeSystems();
        subController.terranBot1.TestStrategy = CLArgs.TestStrategy;
    }

    public void Update()
    {
        var inputSystem = subController.inputSystem;
        switch (appState)
        {
            case AppState.Initialize:
                inputSystem.Init2();
                appState = AppState.Running;
                break;
            //case BotState.WaitForInput:
            //    gameConnection.RequestStep(1);
            //    botState = BotState.Do;
            //    break;
            case AppState.Running:
                //gameConnection.RequestStep(1);
                //inputSystem.observation = gameConnection.RequestObservation();
                inputSystem.observation = gameConnection.StepObservation();
                //inputSystem.Update2();
                var status = gameConnection.status;
                if (status == SC2APIProtocol.Status.Ended || status == SC2APIProtocol.Status.Quit)
                {
                    appState = AppState.Exit;
                }
                else
                {
                    fusion.Update();
                    appState = AppState.Running;
                }
                break;
            case AppState.Exit:
                foreach (var result in inputSystem.observation.PlayerResults)
                {
                    if (result.PlayerId == inputSystem.playerId)
                    {
                        inputSystem.Result = result.Result;
                        Console.WriteLine("Result: {0}", result.Result);
                    }
                }
                gameConnection.LeaveGame();
                subController.terranBot1.OnExit();
                exitProgram = true;
                break;
        }
    }

    public void Dispose()
    {
        fusion.Dispose();
        gameConnection.Dispose();
    }
}
