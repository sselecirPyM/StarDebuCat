using MilkWangBase.Attributes;
using MilkWangBase.Utility;
using StarDebuCat.Algorithm;
using StarDebuCat.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using static MilkWang1.Util;
using MilkWang1.Learning;

namespace MilkWang1;

public class TerranBot1
{
    AnalysisSystem1 analysisSystem;
    PredicationSystem1 predicationSystem;
    MarkerSystem1 markerSystem;
    BattleSystem1 battleSystem;
    BuildSystem1 buildSystem;
    InputSystem1 inputSystem;
    CommandSystem1 commandSystem;

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

    Dictionary<string, BotStrategy> botStrateydict = new();

    BotStrategy currentStrategy;

    BotStrategyList strategyList;
    Statistics statistics;


    List<Vector2> protectPositions;
    List<Vector2> enemyProtectPositions;
    public Vector2 protectPosition;

    Vector2 commandCenterPosition;

    int changeTargetCount = 0;

    int attackCount;

    int attackTimer = 0;

    bool scvAttack;
    bool scvAttack1;

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

        buildSystem.randomlyUpgrade = gameLoop > 300 * 22.4;
        battleSystem.defaultMicro.AllowPush = gameLoop > 240 * 22.4;

        if (gameLoop < 22.4 * 125 && enemyArmy.Count > 1 && !scvAttack
            && Vector2.Distance(commandCenterPosition, enemyArmy[0].position) < 30)
        {
            scvAttack = true;
            for (int i = 0; i < 5; i++)
            {
                if (buildSystem.TryGetAvailableWorker(out var scv))
                {
                    commandSystem.EnqueueAbility(scv, Abilities.ATTACK_ATTACK, enemyArmy[0].position);
                    commandSystem.ToggleAutocastAbility(scv, Abilities.EFFECT_REPAIR_SCV);
                }
            }
        }
        if (gameLoop < 22.4 * 125 && enemyUnits.Count > 1 && !scvAttack1
            && Vector2.Distance(commandCenterPosition, enemyUnits[0].position) < 30)
        {
            scvAttack1 = true;
            for (int i = 0; i < 1; i++)
            {
                if (buildSystem.TryGetAvailableWorker(out var scv))
                {
                    commandSystem.EnqueueAbility(scv, Abilities.ATTACK, enemyUnits[0]);
                    commandSystem.ToggleAutocastAbility(scv, Abilities.EFFECT_REPAIR_SCV);
                }
            }
        }

        foreach (var rail in currentStrategy.buildRails)
        {
            int[] sequenceStart = rail.buildSequenceStart;
            int findIndex = -1;
            for (int i = sequenceStart.Length - 1; i >= 0; i--)
            {
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

        var unitDictionary = analysisSystem.unitDictionary;

        if (battleSystem.mainTarget == Vector2.Zero)
        {
            battleSystem.mainTarget = analysisSystem.StartLocations[0];
        }
        myUnits1.ClearSearch(friendNearbys, battleSystem.mainTarget, 3);


        bool changeTarget = friendNearbys.Count > 2 && gameLoop > 6272;

        attackCount = currentStrategy.attackCount + Math.Clamp(frame.TotalLost - frame.TotalKill, -1200, 1000) / 150;

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
            switch (changeTargetCount)
            {
                case 0:
                    ChangeTarget0();
                    break;
                default:
                    ChangeTarget1();
                    break;
            }

            changeTargetCount++;
            //battleSystem.mainTarget = FindEnemy();
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
            else if (Vector2.Distance(protectPosition, army.position) > 30)
            {
                unit.battleType = UnitBattleType.Undefined;
            }
            else
            {
                unit.battleType = UnitBattleType.ProtectArea;
                unit.protectPosition = protectPosition;
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

    void ChangeTarget0()
    {
        battleSystem.mainTarget = enemyProtectPositions[0];
    }

    void ChangeTarget1()
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

    void PostInitialize()
    {
        initialized = true;
        commandCenterPosition = commandCenters[0].position;
        var enemyStartPosition = analysisSystem.StartLocations[0];

        protectPositions = new List<Vector2>(analysisSystem.patioPointsMerged);
        protectPositions.Sort((a, b) => Vector2.Distance(a, commandCenterPosition).CompareTo(Vector2.Distance(b, commandCenterPosition)));

        if (protectPositions.Count > 0)
            protectPosition = protectPositions[0];
        else
            protectPosition = commandCenterPosition;

        enemyProtectPositions = new List<Vector2>(analysisSystem.patioPointsMerged);
        enemyProtectPositions.Sort((a, b) => Vector2.Distance(a, enemyStartPosition).CompareTo(Vector2.Distance(b, enemyStartPosition)));
        battleSystem.mainTarget = enemyProtectPositions[1];

        //if (TestStrategy != null)
        //    botStrategy = BotData.botStrategies.First(u => u.Name == TestStrategy);
        //else
        //    botStrategy = BotData.botStrategies[0];

        statistics = Statistics.Load();

        DirectoryInfo directoryInfo = new DirectoryInfo("Strategy");
        foreach (var file in directoryInfo.GetFiles())
        {
            switch (file.Name)
            {
                case "strategy_list.json":
                    strategyList = GetData<BotStrategyList>(file.FullName);
                    break;
                default:
                    var strategy = GetData<BotStrategy>(file.FullName);
                    botStrateydict.Add(strategy.Name, strategy);
                    break;
            }
        }
        string strategyName = strategyList.strategies[random.Next(0, strategyList.strategies.Count)];
        currentStrategy = botStrateydict[strategyName];
        Console.WriteLine(currentStrategy.Name);
    }

    void EnemyFindInit()
    {
        enemyFindInit = true;
    }

    public void OnExit()
    {
        if (analysisSystem.enemyRace != 0)
        {
            statistics.LogResult((SC2APIProtocol.Race)analysisSystem.enemyRace, inputSystem.Result, inputSystem.enemyId, currentStrategy.Name);

            statistics.Save();
        }
    }
}
