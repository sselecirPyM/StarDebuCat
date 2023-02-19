using MilkWangBase;
using MilkWangBase.Attributes;
using MilkWangBase.Utility;
using StarDebuCat.Algorithm;
using StarDebuCat.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MilkWang1;

public class TerranBot1
{
    AnalysisSystem1 analysisSystem;
    PredicationSystem1 predicationSystem;
    MarkerSystem markerSystem;
    BattleSystem1 battleSystem;
    BuildSystem1 buildSystem;

    Random random = new Random();


    [XFind("CollectUnits", Alliance.Enemy)]
    public List<Unit> enemyUnits;
    [XFind("CollectUnits", Alliance.Enemy, "Army")]
    public List<Unit> enemyArmy;

    [XFind("CollectUnits", Alliance.Self)]
    public List<Unit> myUnits;

    [XFind("CollectUnits", Alliance.Self, "CommandCenter")]
    public List<Unit> commandCenters;

    [XFind("CollectUnits", Alliance.Self, "Army")]
    public List<Unit> armies;

    [XFind("CollectUnits", Alliance.Neutral, "MineralField")]
    public List<Unit> minerals;


    [XFind("QuadTree", Alliance.Self)]
    public QuadTree<Unit> myUnits1;

    HashSet<Unit> keepers = new();

    List<Unit> enemyNearbys = new();
    List<Unit> friendNearbys = new();

    bool initialized = false;
    bool enemyFindInit = false;

    public Stack<Vector2> enemyBases = new();

    public BotData BotData;

    void Update()
    {

        if (!initialized)
            PostInitialize();

        if (!enemyFindInit && buildSystem.resourcePoints != null)
            EnemyFindInit();

        var s1 = BotData.buildCounts[0];
        foreach (var c in s1)
        {
            buildSystem.requireUnitCount[c.Key] = c.Value;
        }

        buildSystem.requireUnitCount[UnitType.TERRAN_COMMANDCENTER] = (int)analysisSystem.GameLoop / 3360 - predicationSystem.GetPredictTotal(UnitType.TERRAN_ORBITALCOMMAND);
        var unitDictionary = analysisSystem.unitDictionary;

        if (battleSystem.mainTarget == Vector2.Zero)
        {
            battleSystem.mainTarget = analysisSystem.StartLocations[0];
        }
        myUnits1.ClearSearch(friendNearbys, battleSystem.mainTarget, 3);

        bool changeTarget = friendNearbys.Count > 0;

        if (changeTarget)
        {
            var randomUnit = friendNearbys.GetRandom(random);
            if (!keepers.Any(u => Vector2.Distance(u.position, battleSystem.mainTarget) < 7))
                keepers.Add(randomUnit);
            else
            {

            }

            battleSystem.mainTarget = FindEnemy();
        }
        keepers.RemoveWhere(u => analysisSystem.deadUnits.Contains(u));

        foreach (var deadUnit in analysisSystem.deadUnits)
        {
            markerSystem.AddMark(deadUnit.position, "Dead", 30);
        }

        int attackCount = 20;
        foreach (var army in armies)
        {
            if (armies.Count > attackCount)
            {
                battleSystem.units[army] = UnitBattleType.AttackMain;
            }
            else if (Vector2.Distance(battleSystem.protectPosition, army.position) > 30)
            {
                battleSystem.units[army] = UnitBattleType.Undefined;
            }
        }
        battleSystem.esc.Clear();
        foreach (var keeper in keepers)
            battleSystem.esc.Add(keeper);
    }

    Vector2 FindEnemy()
    {
        if (enemyBases.Count > 0)
        {
            return enemyBases.Pop();
        }

        Vector2 target = battleSystem.mainTarget;
        enemyNearbys.Clear();
        foreach (var enemy in enemyUnits)
        {
            Vector2 target1 = enemy.position;
            if ((!enemy.isFlying || !DData.Zerg.Contains(enemy.type)) && analysisSystem.pathing.Query(target1) != 0)
                enemyNearbys.Add(enemy);
        }

        if (enemyNearbys.Count > 0)
        {
            var enemy = enemyNearbys.GetRandom(random);
            target = enemy.position;
        }
        else
        {
            for (int i = 0; i < 3; i++)
            {
                target = minerals.GetRandom(random).position;
                if (analysisSystem.visable.Query(target) != 2)
                    break;
            }
        }

        return target;
    }


    void PostInitialize()
    {
        initialized = true;
        var commandCenterPosition = commandCenters[0].position;
        if (analysisSystem.patioPointsMerged.Count > 0)
            battleSystem.protectPosition = analysisSystem.patioPointsMerged.Nearest(commandCenterPosition);
        else
            battleSystem.protectPosition = commandCenterPosition;
    }

    void EnemyFindInit()
    {
        enemyFindInit = true;
    }
}
