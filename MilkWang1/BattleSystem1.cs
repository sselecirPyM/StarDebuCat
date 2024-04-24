using MilkWang1.Attributes;
using MilkWang1.Micros;
using MilkWangBase.Utility;
using StarDebuCat.Algorithm;
using StarDebuCat.Data;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.Numerics;
using System.Reflection;

namespace MilkWang1;

public class BattleSystem1
{
    AnalysisSystem1 analysisSystem;

    [Units(Alliance.Self, "Army")]
    public List<Unit> armies;

    public BotData BotData;
    public GameData GameData;

    public Dictionary<Unit, BattleUnit> battleUnits = new();

    [QuadTree(Alliance.Self)]
    public QuadTree<Unit> myUnits1;
    [QuadTree(Alliance.Self, "Army")]
    public QuadTree<Unit> armies1;
    [QuadTree(Alliance.Enemy)]
    public QuadTree<Unit> enemyUnits1;
    [QuadTree(Alliance.Enemy, "Worker")]
    public QuadTree<Unit> enemyUnitsWorkers1;
    [QuadTree(Alliance.Enemy, "Ground")]
    public QuadTree<Unit> enemyGround;
    [QuadTree(Alliance.Enemy, "Army")]
    public QuadTree<Unit> enemyArmies1;

    [Units(Alliance.Enemy, "Army", "OutOfSight")]
    public List<Unit> outOfSightEnemyArmy;

    public Vector2 mainTarget;

    public Dictionary<UnitType, IMicro> micros = new Dictionary<UnitType, IMicro>();
    public List<IMicro> micros1 = new List<IMicro>();

    public DefaultMicro defaultMicro;

    void Initialize()
    {
        ContainerConfiguration containerConfiguration = new ContainerConfiguration();
        containerConfiguration.WithAssembly(Assembly.GetExecutingAssembly());
        containerConfiguration.WithExport(this);
        containerConfiguration.WithExport(analysisSystem);
        containerConfiguration.WithExport(GameData);
        var container = containerConfiguration.CreateContainer();
        var exports = container.GetExports<Lazy<IMicro, IDictionary<string, object>>>();
        foreach (var export in exports)
        {
            foreach (var val in export.Metadata.Values)
            {
                if (val is UnitType unitType)
                {
                    micros[unitType] = export.Value;
                }
            }
        }

        defaultMicro = new DefaultMicro();
        container.SatisfyImports(defaultMicro);
        micros1.Add(defaultMicro);
        foreach (var value in micros.Values)
        {
            if (!micros1.Contains(value))
            {
                micros1.Add(value);
            }
        }
    }

    public void Update()
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
                    pair.Key.Command(Abilities.ATTACK, mainTarget);
                    break;
                case UnitBattleType.ProtectArea:
                    if (Vector2.Distance(pair.Value.protectPosition, pair.Value.unit.position) > 3)
                        pair.Key.Command(Abilities.ATTACK, pair.Value.protectPosition);
                    break;
            }
        }
    }

    List<Unit> enemyNearbyMix = new();

    List<Unit> friendNearbys = new();
    List<Unit> enemyAny = new();

    Dictionary<Unit, int> enemyChaseCount = new();
    void MicroOperate()
    {
        enemyChaseCount.Clear();
        foreach (var unit in armies)
        {
            float fireRange = GameData.GetFireRange(unit.type);

            var unitPosition = unit.position;
            enemyArmies1.ClearSearch(enemyNearbyMix, unitPosition, 15f);

            enemyUnits1.Search(enemyNearbyMix, unitPosition, 3.0f);

            enemyUnitsWorkers1.Search(enemyNearbyMix, unitPosition, 6.5f);

            armies1.ClearSearch(friendNearbys, unitPosition, 5.5f);

            enemyUnits1.ClearSearch(enemyAny, unitPosition, fireRange + 1.5f);

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
                var enemyTypeData = GameData.GetUnitTypeData(enemy.type);
                float enemyRange = GameData.GetFireRange(enemy.type);
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
                float enemyHealth = enemy.health + enemy.shield * 0.2f - enemy.energy * 0.2f;
                if (enemy.type == UnitType.TERRAN_SIEGETANK || enemy.type == UnitType.TERRAN_SIEGETANKSIEGED ||
                    enemy.type == UnitType.ZERG_BANELING || enemy.type == UnitType.ZERG_BANELINGBURROWED)
                {
                    enemyHealth *= 0.35f;
                }

                if (enemyHealth < minLife && distance < fireRange + 0.2f)
                {
                    minLife = enemyHealth;
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
                var friendlyTypeData = GameData.GetUnitTypeData(friendly.type);
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
                try
                {
                    micro.Micro(pair.Value);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine(e.StackTrace);
                }
            }
        }
        foreach (var unit in armies)
        {
            var battleUnit = battleUnits[unit];
            if (battleUnit.commanding)
                continue;
            float fireRange = GameData.GetFireRange(unit.type);

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
