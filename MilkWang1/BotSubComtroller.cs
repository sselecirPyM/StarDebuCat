﻿using MilkWangBase.Attributes;

namespace MilkWang1;

public class BotSubController
{
    [System]
    public InputSystem1 inputSystem;
    [System]
    public AnalysisSystem1 analysisSystem;
    [System]
    public PredicationSystem1 predicationSystem;
    [System]
    public MarkerSystem1 markerSystem;
    [System]
    public TerranBot1 terranBot1;
    [System]
    public BattleSystem1 battleSystem;
    [System]
    public BuildSystem1 buildSystem;
    [System]
    public CommandSystem1 commandSystem;
    [System]
    public DebugSystem debugSystem;

    //public void Update()
    //{
    //    analysisSystem.Update();
    //    predicationSystem.Update();
    //    markerSystem.Update();
    //    terranBot1.Update();
    //    battleSystem.Update();
    //    buildSystem.Update();
    //    commandSystem.Update();
    //    debugSystem.Update();
    //}
}
