using System.Collections.Generic;

namespace StarDebuCat.Data;

public class AutoCast
{
    public float energyRequired;
    public float range;
    public Abilities ability;

    public float enemyEnegyRequired;
    public float enemyCountRequired;

    public bool noEnemy;
}

public partial class GameData
{
    public HashSet<UnitType> autoBuild;
    public HashSet<UnitType> refineries;
    public HashSet<UnitType> vespeneGeysers;
    public HashSet<UnitType> mineralFields;
    public HashSet<UnitType> building;
    public HashSet<UnitType> armies;
    public HashSet<UnitType> workers;
    public HashSet<UnitType> commandCenters;
    public HashSet<UnitType> flying;
    public Dictionary<Abilities, UnitType> morphToUnit;
    public Dictionary<UnitType, int> buildCompleteFoodCost;
    public Dictionary<UnitType, AutoCast> autoCast;

    public List<UpgradeRequirement> upgradeRequirements;
}
