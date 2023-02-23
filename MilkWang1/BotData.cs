using StarDebuCat.Data;
using System.Collections.Generic;

namespace MilkWang1;

public class BuildRail
{
    public Dictionary<UnitType, int>[] buildSequence;
    public int[] buildSequenceStart;
}

public class BotStrategy
{
    public string Name;
    public string Description;

    public BuildRail[] buildRails;
    public int attackCount;
}

public class BotData
{
    public Dictionary<UnitType, int>[] buildCounts;
    public Dictionary<string, UnitType[]> unitLists;
    public UnitType[][] typeGroup;

    public Dictionary<UnitType, Abilities> onIdle;

    public UnitType supplyBuilding;
    public UnitType supplyUnit;
    public UnitType workerType;

    public object[] receptors;
    public object[] runs;
    public Race race;
    public BotStrategy[] botStrategies;

}
