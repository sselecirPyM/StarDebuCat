using MilkWangBase;
using MilkWangBase.Core;
using MilkWangBase.Utility;
using StarDebuCat;
using System;

namespace MilkWang1;

public class BotController : IDisposable
{
    public GameConnection gameConnection;

    public CLArgs CLArgs;
    public BotData botData;
    public BotSubComtroller subComtroller;
    Fusion fusion;

    public bool exitProgram;

    public void Initialize()
    {
        gameConnection = new GameConnection();
        subComtroller = new BotSubComtroller();
        var inputSystem = subComtroller.inputSystem = new InputSystem1();

        inputSystem.ladderGame = true;
        inputSystem.Race = SC2APIProtocol.Race.Terran;
        inputSystem.port = CLArgs.StartPort;
        inputSystem.gamePort = CLArgs.GamePort;
        if (CLArgs.Map != null)
        {
            StarDebuCat.Utility.SC2GameHelp.LaunchSC2(CLArgs.StartPort, out var maps);
            inputSystem.map = maps + "/" + CLArgs.Map;
            inputSystem.ladderGame = false;
            inputSystem.ComputerDifficulty = CLArgs.ComputerDifficulty;
            inputSystem.ComputerRace = CLArgs.ComputerRace;
        }
        inputSystem.gameConnection = gameConnection;

        while (!inputSystem.readyToPlay && !inputSystem.exitProgram)
        {
            inputSystem.Update();
        }
        if (inputSystem.exitProgram)
        {
            exitProgram = true;
            return;
        }

        fusion = new Fusion(subComtroller);
        subComtroller.terranBot1.BotData = botData;
        subComtroller.battleSystem.BotData = botData;
        subComtroller.buildSystem.BotData = botData;
        subComtroller.commandSystem.gameConnection = gameConnection;
        subComtroller.debugSystem.enable = CLArgs.Debug;
        subComtroller.debugSystem.gameConnection = gameConnection;

        fusion.InitializeSystems();
    }

    public void Update()
    {
        fusion.Update();
        exitProgram = subComtroller.inputSystem.exitProgram;
    }

    public void Dispose()
    {
        fusion.Dispose();
    }
}
