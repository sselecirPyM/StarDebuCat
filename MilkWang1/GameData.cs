using StarDebuCat.Data;
using System.Collections.Generic;

namespace MilkWang1;

public class GameData
{
    public HashSet<UnitType> selfBuild;
    public Dictionary<Abilities, UnitType> morphToUnit;
    public Dictionary<UnitType, int> buildCompleteFoodCost;
    public Dictionary<string, object> Exports;
}
