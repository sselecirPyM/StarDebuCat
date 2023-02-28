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
    MarkerSystem1 markerSystem;
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

    public string TestStrategy;

    BotStrategy botStrategy;

    int attackCount;

    int attackTimer = 0;
    void Update()
    {

        if (!initialized)
            PostInitialize();

        if (!enemyFindInit && buildSystem.resourcePoints != null)
            EnemyFindInit();

        var frame = analysisSystem.currentFrameResource;
        int gameLoop = frame.GameLoop;
        float previousTime = (gameLoop - 1) / 22.4f;
        float currentTime = gameLoop / 22.4f;

        buildSystem.requireUnitCount.Clear();

        foreach (var rail in botStrategy.buildRails)
        {
            int[] sequenceStart = rail.buildSequenceStart;
            int findIndex = -1;
            for (int i = sequenceStart.Length - 1; i >= 0; i--)
            {
                //if (previousTime <= sequenceStart[i] && currentTime > sequenceStart[i])
                //{
                //    findIndex = i;
                //}
                if (currentTime > sequenceStart[i])
                {
                    findIndex = i;
                    break;
                }
            }
            if (findIndex < 0)
                continue;

            var s1 = rail.buildSequence[findIndex];
            foreach (var c in s1)
            {
                if (buildSystem.requireUnitCount.TryGetValue(c.Key, out var origin))
                {

                }
                buildSystem.requireUnitCount[c.Key] = c.Value + origin;
            }
        }

        //foreach (var deadUnit in analysisSystem.deadUnits)
        //{
        //    if (deadUnit.alliance == Alliance.Self)
        //    {
        //        if (buildSystem.requireUnitCount.TryGetValue(deadUnit.type, out var origin))
        //        {
        //            buildSystem.requireUnitCount[deadUnit.type] = Math.Max(origin - 1, 0);
        //        }
        //    }
        //}

        //buildSystem.requireUnitCount.TryGetValue(UnitType.TERRAN_COMMANDCENTER, out var commandCenterCount);
        //buildSystem.requireUnitCount[UnitType.TERRAN_COMMANDCENTER] = commandCenterCount - predicationSystem.GetPredictTotal(UnitType.TERRAN_ORBITALCOMMAND);
        var unitDictionary = analysisSystem.unitDictionary;

        if (battleSystem.mainTarget == Vector2.Zero)
        {
            battleSystem.mainTarget = analysisSystem.StartLocations[0];
        }
        myUnits1.ClearSearch(friendNearbys, battleSystem.mainTarget, 3);

        bool changeTarget = friendNearbys.Count > 0;

        attackCount = botStrategy.attackCount + Math.Clamp(frame.MineralLost + frame.VespeneLost - frame.MineralKill - frame.VespeneKill, -1000, 1000) / 150;

        if (gameLoop > 13440)
        {
            attackTimer++;
        }
        if (attackTimer > 1344)
        {
            attackTimer = 0;
            changeTarget = true;
        }
        if (changeTarget)
        {
            if (friendNearbys.TryGetRandom(random, out var randomUnit))
            {
                if (!keepers.Any(u => Vector2.Distance(u.position, battleSystem.mainTarget) < 7))
                {
                    keepers.Add(randomUnit);
                    if (!battleSystem.battleUnits.TryGetValue(randomUnit, out var bu))
                    {
                        battleSystem.battleUnits[randomUnit] = bu = new BattleUnit(randomUnit);
                    }
                    bu.battleType = UnitBattleType.ProtectArea;
                    bu.protectPosition = battleSystem.mainTarget;
                }
                else
                {

                }
            }
            else
            {
                Console.WriteLine("Lost target.");
            }

            battleSystem.mainTarget = FindEnemy();
        }
        keepers.RemoveWhere(u => analysisSystem.deadUnits.Contains(u));

        foreach (var deadUnit in analysisSystem.deadUnits)
        {
            markerSystem.AddMark(deadUnit.position, "Dead", 30);
        }

        foreach (var army in armies)
        {
            if (!battleSystem.battleUnits.TryGetValue(army, out var unit))
            {
                battleSystem.battleUnits[army] = unit = new BattleUnit(army);
            }
            if (keepers.Contains(army))
                continue;

            if (armies.Count > attackCount)
            {
                unit.battleType = UnitBattleType.AttackMain;
            }
            else if (Vector2.Distance(battleSystem.protectPosition, army.position) > 30)
            {
                unit.battleType = UnitBattleType.Undefined;
            }
            else
            {
                unit.battleType = UnitBattleType.ProtectArea;
                unit.protectPosition = battleSystem.protectPosition;
            }
        }
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
            if ((!enemy.isFlying || !DData.Zerg.Contains(enemy.type)))
                enemyNearbys.Add(enemy);
        }

        if (enemyNearbys.Count > 0 && random.Next(0, 5) > 0)
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

        if (TestStrategy != null)
            botStrategy = BotData.botStrategies.First(u => u.Name == TestStrategy);
        else
            botStrategy = BotData.botStrategies[0];
    }

    void EnemyFindInit()
    {
        enemyFindInit = true;
    }
}
