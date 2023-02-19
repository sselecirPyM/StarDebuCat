using MilkWangBase;
using MilkWangBase.Attributes;
using MilkWangBase.Core;
using StarDebuCat;
using System;

namespace MilkWang1;

public class BotController : IDisposable
{
    [System]
    public GameConnection gameConnection;
    [System]
    public InputSystem inputSystem;
    [System]
    public AnalysisSystem analysisSystem;
    [System]
    public PredicationSystem predicationSystem;
    [System]
    public MarkerSystem markerSystem;
    [System]
    public TerranBot1 terranBot1;
    [System]
    public BattleSystem1 battleSystem;
    [System]
    public BuildSystem1 buildSystem;
    [System]
    public CommandSystem commandSystem;
    [System]
    public DebugSystem debugSystem;

    public BotSubComtroller subComtroller;
    Fusion fusion;

    public void Initialize()
    {
        subComtroller = new BotSubComtroller();
        fusion = new Fusion(subComtroller);

    }

    public void Update()
    {
        fusion.Update();
    }

    public void Dispose()
    {
        fusion.Dispose();
    }
}
