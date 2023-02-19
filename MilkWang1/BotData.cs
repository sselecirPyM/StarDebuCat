using StarDebuCat.Data;
using System.Collections.Generic;

namespace MilkWang1;

public class BotData
{
    public Dictionary<UnitType, int>[] buildCounts;
    public Dictionary<string, UnitType[]> unitLists;
    public UnitType[][] typeGroup;

    public Dictionary<UnitType, Abilities> onIdle;

    public object[] receptors;
    public object[] runs;
}
