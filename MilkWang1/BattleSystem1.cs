using MilkWang1.Micros;
using MilkWangBase;
using MilkWangBase.Attributes;
using MilkWangBase.Core;
using MilkWangBase.Utility;
using StarDebuCat.Algorithm;
using StarDebuCat.Data;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace MilkWang1;

public class BattleSystem1
{
    AnalysisSystem1 analysisSystem;
    CommandSystem1 commandSystem;
    Fusion fusion;

    [XFind("CollectUnits", Alliance.Self, "Army")]
    public List<Unit> armies;

    public BotData BotData;
    public GameData GameData;

    public Dictionary<Unit, BattleUnit> battleUnits = new();

    [XFind("QuadTree", Alliance.Self)]
    public QuadTree<Unit> myUnits1;
    [XFind("QuadTree", Alliance.Self, "Army")]
    public QuadTree<Unit> armies1;
    [XFind("QuadTree", Alliance.Enemy)]
    public QuadTree<Unit> enemyUnits1;
    [XFind("QuadTree", Alliance.Enemy, "Ground")]
    public QuadTree<Unit> enemyGround;
    [XFind("QuadTree", Alliance.Enemy, "Army")]
    public QuadTree<Unit> enemyArmies1;

    [XFind("CollectUnits", Alliance.Enemy, "Army", "OutOfSight")]
    public List<Unit> outOfSightEnemyArmy;

    public Vector2 mainTarget;

    public Dictionary<UnitType, IMicro> micros;
    public List<IMicro> micros1;

    Random random = new();
    public DefaultMicro defaultMicro;

    void Initialize()
    {
        defaultMicro = fusion.Instantiate<DefaultMicro>();
        IMicro siegeTankMicro = fusion.Instantiate<SiegeTankMicro>();
        IMicro ghostMicro = fusion.Instantiate<GhostMicro>();

        micros = new Dictionary<UnitType, IMicro>();
        micros[UnitType.TERRAN_CYCLONE] = fusion.Instantiate<CycloneMicro>();
        micros[UnitType.TERRAN_SIEGETANK] = siegeTankMicro;
        micros[UnitType.TERRAN_SIEGETANKSIEGED] = siegeTankMicro;
        micros[UnitType.TERRAN_GHOST] = ghostMicro;

        micros1 = new List<IMicro>();
        micros1.Add(defaultMicro);
        foreach (var value in micros.Values)
        {
            if (!micros1.Contains(value))
            {
                micros1.Add(value);
            }
        }
    }

    void Update()
    {
        foreach (var army in armies)
        {
            if (!battleUnits.TryGetValue(army, out var unit))
            {
                battleUnits[army] = unit = new BattleUnit(army);
            }
        }
        foreach (var deadUnit in analysisSystem.deadUnits)
            battleUnits.Remove(deadUnit);

        MicroOperate();

        foreach (var pair in battleUnits)
        {
            if (pair.Value.commanding)
            {
                pair.Value.commanding = false;
                continue;
            }
            switch (pair.Value.battleType)
            {
                case UnitBattleType.AttackMain:
                    commandSystem.OptimiseCommand(pair.Key, Abilities.ATTACK, mainTarget);
                    break;
                case UnitBattleType.ProtectArea:
                    if (Vector2.Distance(pair.Value.protectPosition, pair.Value.unit.position) > 3)
                        commandSystem.OptimiseCommand(pair.Key, Abilities.ATTACK, pair.Value.protectPosition);
                    break;
            }
        }
    }

    List<Unit> enemyNearbyMix = new();
    List<Unit> enemyNearbyWorker = new();

    List<Unit> friendNearbys = new();
    List<Unit> enemyAny = new();

    Dictionary<Unit, int> enemyChaseCount = new();
    void MicroOperate()
    {
        enemyChaseCount.Clear();
        foreach (var unit in armies)
        {
            float fireRange = analysisSystem.GetFireRange(unit.type);

            var unitPosition = unit.position;
            enemyArmies1.ClearSearch(enemyNearbyMix, unitPosition, 15f);
            enemyUnits1.Search(enemyNearbyMix, unitPosition, 3.5f);
            armies1.ClearSearch(friendNearbys, unitPosition, 5.5f);

            enemyUnits1.ClearSearch(enemyAny, unitPosition, fireRange + 1.5f);

            enemyUnits1.ClearSearch(enemyNearbyWorker, unitPosition, 6.5f);
            enemyNearbyWorker.RemoveAll(u => !analysisSystem.GameData.workers.Contains(u.type));
            enemyNearbyMix.AddRange(enemyNearbyWorker);

            float inEnemyRange = 0;
            float enemyInRange = 0;
            float friendlyNearByFood = 0;
            float dummyEnemyMaxRange = 0;
            float enemyMaxRange = 0;
            Unit nearestEnemy = null;
            Unit minLifeEnemy = null;
            float nearestDistance = 20.0f;
            float minLife = 150.0f;
            foreach (var enemy in enemyNearbyMix)
            {
                var enemyTypeData = analysisSystem.GetUnitTypeData(enemy);
                float enemyRange = analysisSystem.GetFireRange(enemy.type);
                float dummyEnemyRange = Math.Max(enemyRange, 3) + 2.5f;
                float fireRange1 = Math.Max(fireRange, 4);
                float distance = Math.Max(Vector2.Distance(unitPosition, enemy.position) - unit.radius - enemy.radius, 0);
                if (distance < dummyEnemyRange)
                    inEnemyRange += enemyTypeData.FoodRequired;
                if (distance < fireRange1 + 0.5f)
                    enemyInRange += enemyTypeData.FoodRequired;

                dummyEnemyMaxRange = Math.Max(dummyEnemyMaxRange, dummyEnemyRange);

                if (distance < enemyRange + 2.5f)
                    enemyMaxRange = Math.Max(enemyMaxRange, enemyRange);

                if (enemy.health < minLife && distance < fireRange + 0.2f)
                {
                    minLife = enemy.health;
                    minLifeEnemy = enemy;
                }

                if (nearestDistance > distance)
                {
                    nearestEnemy = enemy;
                    nearestDistance = distance;
                }
            }

            foreach (var friendly in friendNearbys)
            {
                var friendlyTypeData = analysisSystem.GetUnitTypeData(friendly);
                friendlyNearByFood += friendlyTypeData.FoodRequired;
            }
            var battleUnit = battleUnits[unit];

            battleUnit.enemyInRangeFood = enemyInRange;
            battleUnit.inEnemyRangeFood = inEnemyRange;
            battleUnit.friendlyNearByFood = friendlyNearByFood;
            battleUnit.dummyEnemyMaxRange = dummyEnemyMaxRange;
            battleUnit.enemyMaxRange = enemyMaxRange;
            battleUnit.nearestDistance = nearestDistance;
            battleUnit.nearestEnemy = nearestEnemy;
            battleUnit.minLifeEnemy = minLifeEnemy;
            battleUnit.enemyAnyInRange = enemyAny.Count;
        }
        foreach (var micro in micros1)
        {
            micro.Update();
        }
        foreach (var pair in battleUnits)
        {
            if (micros.TryGetValue(pair.Key.type, out var micro))
            {
                micro.Micro(pair.Value);
            }
        }
        foreach (var unit in armies)
        {
            var battleUnit = battleUnits[unit];
            if (battleUnit.commanding)
                continue;
            float fireRange = analysisSystem.GetFireRange(unit.type);

            if (battleUnit.inEnemyRangeFood < battleUnit.friendlyNearByFood * 0.7f)
            {
                battleUnit.microStrategy = MicroStrategy.Push;
            }
            else if (battleUnit.inEnemyRangeFood < battleUnit.friendlyNearByFood * 1.0f)
            {
                battleUnit.microStrategy = MicroStrategy.Forward;
            }
            else if (battleUnit.inEnemyRangeFood < battleUnit.friendlyNearByFood * 1.5f + 1 && fireRange < 2.0f)
            {
                battleUnit.microStrategy = MicroStrategy.Forward;
            }
            else
            {
                battleUnit.microStrategy = MicroStrategy.None;
            }

            defaultMicro.Micro(battleUnit);
        }
    }
}
