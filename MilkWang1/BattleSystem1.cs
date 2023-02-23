using MilkWangBase.Attributes;
using MilkWangBase.Utility;
using StarDebuCat.Algorithm;
using StarDebuCat.Data;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace MilkWang1;

public enum MicroStrategy
{
    None,
    Push,
    Forward,
    Kite
}

public struct MicroState
{
    public float inEnemyRangeFood;
    public float enemyInRangeFood;
    public float friendlyNearByFood;
    public float enemyMaxRange;
    public Unit nearestEnemy;
    public Unit minLifeEnemy;
    public float nearestDistance;
}
public class BattleSystem1
{
    AnalysisSystem1 analysisSystem;
    CommandSystem1 commandSystem;


    [XFind("CollectUnits", Alliance.Self, "Army")]
    public List<Unit> armies;

    public BotData BotData;
    public GameData GameData;

    public Dictionary<Unit, BattleUnit> units = new();

    [XFind("QuadTree", Alliance.Self)]
    public QuadTree<Unit> myUnits1;
    [XFind("QuadTree", Alliance.Self, "Army")]
    public QuadTree<Unit> armies1;
    [XFind("QuadTree", Alliance.Enemy)]
    public QuadTree<Unit> enemyUnits1;
    [XFind("QuadTree", Alliance.Enemy, "Army")]
    public QuadTree<Unit> enemyArmies1;

    [XFind("CollectUnits", Alliance.Enemy, "Army", "OutOfSight")]
    public List<Unit> outOfSightEnemyArmy;

    public Vector2 mainTarget;
    public Vector2 protectPosition;

    HashSet<Unit> esc = new();

    Random random = new();

    void Update()
    {
        foreach (var deadUnit in analysisSystem.deadUnits)
            units.Remove(deadUnit);

        MicroOperate();

        foreach (var unit in units)
        {
            if (esc.Contains(unit.Key))
                continue;
            switch (unit.Value.battleType)
            {
                case UnitBattleType.AttackMain:
                    commandSystem.OptimiseCommand(unit.Key, Abilities.ATTACK, mainTarget);
                    break;
                case UnitBattleType.ProtectArea:
                    if (Vector2.Distance(unit.Value.protectPosition, unit.Value.unit.position) > 3)
                        commandSystem.OptimiseCommand(unit.Key, Abilities.ATTACK, unit.Value.protectPosition);
                    break;
            }
        }
        esc.Clear();
    }

    List<Unit> enemyNearby6 = new();

    List<Unit> enemyNearbyAll = new();
    List<Unit> friendNearbys = new();
    Dictionary<Unit, MicroState> microStates = new();
    Dictionary<Unit, int> enemyChaseCount = new();
    void MicroOperate()
    {
        microStates.Clear();
        enemyChaseCount.Clear();
        foreach (var unit in armies)
        {
            var unitPosition = unit.position;
            enemyArmies1.ClearSearch(enemyNearby6, unitPosition, 15f);
            enemyUnits1.Search(enemyNearby6, unitPosition, 3.5f);
            armies1.ClearSearch(friendNearbys, unitPosition, 5.5f);

            float inEnemyRange = 0;
            float enemyInRange = 0;
            float friendlyNearByFood = 0;
            float enemyMaxRange = 0;
            Unit nearestEnemy = null;
            Unit minLifeEnemy = null;
            float nearestDistance = 20.0f;
            float minLife = 150.0f;
            foreach (var enemy in enemyNearby6)
            {
                var enemyTypeData = analysisSystem.GetUnitTypeData(enemy);
                float enemyRange = Math.Max(analysisSystem.fireRanges[(int)enemy.type], 3);
                float fireRange = analysisSystem.fireRanges[(int)unit.type];
                float fireRange1 = Math.Max(fireRange, 4);
                float distance = Math.Max(Vector2.Distance(unitPosition, enemy.position) - unit.radius - enemy.radius, 0);
                if (distance < enemyRange + 2.5f)
                    inEnemyRange += enemyTypeData.FoodRequired;
                if (distance < fireRange1 + 0.5f)
                    enemyInRange += enemyTypeData.FoodRequired;

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

            //    enemyChaseCount.TryGetValue(enemy, out var ec1);
            //if (nearestEnemy != null&&ec1 < 6)
            //    enemyChaseCount.Increment(nearestEnemy);
            //if (minLifeEnemy != null && ec1 < 6)
            //    enemyChaseCount.Increment(minLifeEnemy);

            foreach (var friendly in friendNearbys)
            {
                var friendlyTypeData = analysisSystem.GetUnitTypeData(friendly);
                friendlyNearByFood += friendlyTypeData.FoodRequired;
            }
            var microstate1 = new MicroState()
            {
                enemyInRangeFood = enemyInRange,
                inEnemyRangeFood = inEnemyRange,
                friendlyNearByFood = friendlyNearByFood,
                enemyMaxRange = enemyMaxRange,
                nearestDistance = nearestDistance,
                nearestEnemy = nearestEnemy,
                minLifeEnemy = minLifeEnemy,
            };
            microStates[unit] = microstate1;
        }

        foreach (var unit in armies)
        {
            float fireRange = analysisSystem.fireRanges[(int)unit.type];
            var microState1 = microStates[unit];
            MicroStrategy microStrategy;

            if (microState1.inEnemyRangeFood < microState1.friendlyNearByFood * 0.7f)
            {
                microStrategy = MicroStrategy.Push;
            }
            else if (microState1.inEnemyRangeFood < microState1.friendlyNearByFood * 1.0f)
            {
                microStrategy = MicroStrategy.Forward;
            }
            else if (microState1.inEnemyRangeFood < microState1.friendlyNearByFood * 1.5f + 1 && fireRange < 2.0f)
            {
                microStrategy = MicroStrategy.Forward;
            }
            else
            {
                microStrategy = MicroStrategy.None;
            }
            if (!CastAbil(unit, microState1, microStrategy))
                MicroAttack(unit, microState1, microStrategy);
        }
    }

    bool CastAbil(Unit unit, MicroState microState1, MicroStrategy microStrategy)
    {
        bool cast = false;
        if (GameData.autoCast.TryGetValue(unit.type, out var autoCast) &&
            unit.TryGetOrder(out var order) && (Abilities)order.AbilityId != autoCast.ability)
        {
            bool hasEnemy = microState1.nearestEnemy != null;

            cast |= !autoCast.noEnemy && hasEnemy && microState1.nearestDistance < autoCast.range;
            cast |= autoCast.noEnemy && (!hasEnemy || microState1.nearestDistance > autoCast.range);

            if (cast && analysisSystem.abilitiesData[(int)autoCast.ability].target == SC2APIProtocol.AbilityData.Target.None)
            {
                commandSystem.EnqueueAbility(unit, autoCast.ability);
                esc.Add(unit);
            }
        }

        return cast;
    }

    void MicroAttack(Unit unit, MicroState microState1, MicroStrategy microStrategy)
    {
        var unitPosition = unit.position;
        float fireRange = analysisSystem.fireRanges[(int)unit.type];

        var enemy = microState1.nearestEnemy;
        var enemyMaxRange = microState1.enemyMaxRange;
        if (enemy != null)
        {
            if (enemyMaxRange < fireRange && microState1.nearestDistance > fireRange)
            {
                microStrategy = MicroStrategy.Push;
            }

            if (unit.weaponCooldown > 0.1f * 22.4f && fireRange > 2)
            {
                switch (microStrategy)
                {
                    case MicroStrategy.Forward:
                        if (enemyMaxRange + 0.5f >= fireRange)
                            commandSystem.OptimiseCommand(unit, Abilities.ATTACK, enemy.position);
                        else
                            commandSystem.OptimiseCommand(unit, Abilities.MOVE, unitPosition.Closer(enemy.position, -0.3f, Math.Min(fireRange, enemyMaxRange + 1.5f)));
                        break;
                    case MicroStrategy.Push:
                        commandSystem.OptimiseCommand(unit, Abilities.MOVE, unitPosition.Closer(enemy.position, 0.2f, Math.Min(2.0f, fireRange)));
                        break;
                    case MicroStrategy.None:
                        commandSystem.OptimiseCommand(unit, Abilities.MOVE, unitPosition.Closer(enemy.position, -0.3f, Math.Min(fireRange, enemyMaxRange + 1.5f)));
                        break;
                }
                esc.Add(unit);
            }
            else if (unit.weaponCooldown <= 1 && microState1.minLifeEnemy != null)
            {
                commandSystem.EnqueueAbility(unit, Abilities.ATTACK, microState1.minLifeEnemy);
                esc.Add(unit);
            }
            else if (microStrategy == MicroStrategy.Forward || microStrategy == MicroStrategy.Push)
            {
                commandSystem.OptimiseCommand(unit, Abilities.ATTACK, enemy.position);
                esc.Add(unit);
            }
            else
            {
                commandSystem.OptimiseCommand(unit, Abilities.MOVE, unitPosition.Closer(enemy.position, -0.3f, Math.Max(fireRange, enemyMaxRange + 1.0f)));
                esc.Add(unit);
            }
        }
    }
}
