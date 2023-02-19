using MilkWangBase;
using MilkWangBase.Attributes;
using MilkWangP1;
using StarDebuCat;

namespace MilkWang1;

public class BotController
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
    public ProtossBot1 protossBot1;
    [System]
    public BattleSystem battleSystem;
    [System]
    public BuildSystem buildSystem;
    [System]
    public CommandSystem commandSystem;
    [System]
    public DebugSystem debugSystem;
}
