namespace MilkWang1.Micros;

public class VikingMicro : IMicro
{
    CommandSystem1 commandSystem;
    AnalysisSystem1 analysisSystem;
    BattleSystem1 battleSystem;

    public VikingMicro(CommandSystem1 commandSystem, AnalysisSystem1 analysisSystem, BattleSystem1 battleSystem)
    {
        this.commandSystem = commandSystem;
        this.analysisSystem = analysisSystem;
        this.battleSystem = battleSystem;
    }

    public void Micro(BattleUnit battleUnit)
    {

    }

    public void Update()
    {

    }
}
