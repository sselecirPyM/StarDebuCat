using StarDebuCat.Data;
using System.Collections.Generic;

namespace MilkWang1;

public class BotData
{
    public Dictionary<UnitType, int>[] buildCounts;
    public Dictionary<string, UnitType[]> unitLists;

    public Dictionary<UnitType, Abilities> onIdle;

    public UnitType supplyBuilding;
    public UnitType supplyUnit;
    public UnitType workerType;

    public Race race;
    public BotStrategy[] botStrategies;

}
