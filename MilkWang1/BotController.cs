using MilkWangBase;
using MilkWangBase.Core;
using MilkWangBase.Utility;
using Newtonsoft.Json;
using StarDebuCat;
using System;
using System.IO;

namespace MilkWang1;

public class BotController : IDisposable
{
    public GameConnection gameConnection;

    public CLArgs CLArgs;
    public BotSubController subController;
    Fusion fusion;

    public bool exitProgram;

    public void Initialize()
    {
        //File.WriteAllText("requirement.json", JsonConvert.SerializeObject(StarDebuCat.Data.DData.UpgradeRequirements,new JsonSerializerSettings { Converters = { new StringEnumConverter()},DefaultValueHandling=DefaultValueHandling.Ignore }));

        var botData = JsonConvert.DeserializeObject<BotData>(File.ReadAllText("BotData/terran.json"),
            new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize
            });
        var gameData = JsonConvert.DeserializeObject<GameData>(File.ReadAllText("GameData/GameData.json"),
            new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize
            });
        gameConnection = new GameConnection();
        subController = new BotSubController();
        var inputSystem = subController.inputSystem = new InputSystem1();

        inputSystem.ladderGame = true;
        inputSystem.Race = (SC2APIProtocol.Race)botData.race;
        inputSystem.port = CLArgs.StartPort;
        inputSystem.gamePort = CLArgs.GamePort;
        if (CLArgs.Map != null)
        {
            StarDebuCat.Utility.SC2GameHelp.LaunchSC2(CLArgs.StartPort, out var maps);
            inputSystem.map = maps + "\\" + CLArgs.Map;
            inputSystem.ladderGame = false;
            inputSystem.ComputerDifficulty = CLArgs.ComputerDifficulty;
            inputSystem.ComputerRace = CLArgs.ComputerRace;
            inputSystem.ComputerAIBuild = CLArgs.AIBuild;
        }
        inputSystem.gameConnection = gameConnection;

        while (!inputSystem.readyToPlay && !inputSystem.exitProgram)
        {
            inputSystem.Update1();
        }
        if (inputSystem.exitProgram)
        {
            exitProgram = true;
            return;
        }

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
        subController.inputSystem.Update1();
        exitProgram = subController.inputSystem.exitProgram;
        if (!exitProgram)
            fusion.Update();
    }

    public void Dispose()
    {
        fusion.Dispose();
        gameConnection.Dispose();
    }
}
