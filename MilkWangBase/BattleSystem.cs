using MilkWangBase.Attributes;
using MilkWangBase.Utility;
using StarDebuCat.Algorithm;
using StarDebuCat.Data;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace MilkWangBase;

public enum UnitBattleType
{
    Undefined = 0,
    AttackMain,
    ProtectArea,
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
public class BattleSystem
{
    AnalysisSystem analysisSystem;
    CommandSystem commandSystem;

    [Find("ReadyToPlay")]
    bool readyToPlay;


    [XFind("CollectUnits", Alliance.Self, "Army")]
    public List<Unit> armies;

    public Dictionary<Unit, UnitBattleType> units = new();


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
    HashSet<Unit> outOfSightEnemyArmy1 = new();

    public Vector2 mainTarget;

    public Vector2 protectPosition;

    public HashSet<Unit> esc = new();

    Random random = new();

    List<Unit> attackArmy = new();
    List<Unit> protectorArmy = new();
    void Update()
    {
        if (!readyToPlay)
            return;

        foreach (var deadUnit in analysisSystem.deadUnits)
            units.Remove(deadUnit);

        MicroOperate();

        attackArmy.Clear();
        protectorArmy.Clear();
        foreach (var unit in units)
        {
            if (esc.Contains(unit.Key))
                continue;
            switch (unit.Value)
            {
                case UnitBattleType.AttackMain:
                    attackArmy.Add(unit.Key);
                    break;
                case UnitBattleType.ProtectArea:
                    protectorArmy.Add(unit.Key);
                    break;
            }
        }

        commandSystem.EnqueueAbility(attackArmy, Abilities.ATTACK, mainTarget);
        commandSystem.EnqueueAbility(protectorArmy, Abilities.ATTACK, protectPosition);
    }

    List<Unit> enemyNearby6 = new();

    List<Unit> enemyNearbyAll = new();
    List<Unit> friendNearbys = new();
    Dictionary<Unit, MicroState> microState = new();
    Dictionary<Unit, int> enemyChaseCount = new();
    void MicroOperate()
    {
        outOfSightEnemyArmy1.Clear();
        foreach (var unit in outOfSightEnemyArmy)
            outOfSightEnemyArmy1.Add(unit);
        microState.Clear();
        enemyChaseCount.Clear();
        foreach (var unit in armies)
        {
            var unitPosition = unit.position;
            enemyArmies1.ClearSearch(enemyNearby6, unitPosition, 10.5f);
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
                float distance = Vector2.Distance(unitPosition, enemy.position);
                if (distance < enemyRange + 2.5f)
                    inEnemyRange += enemyTypeData.FoodRequired;
                if (distance < fireRange1 + 0.5f)
                    enemyInRange += enemyTypeData.FoodRequired;

                enemyMaxRange = Math.Max(enemyMaxRange, enemyRange);
                enemyChaseCount.TryGetValue(enemy, out var ec1);

                if (enemy.health < minLife && distance < fireRange + 0.2f && ec1 < 6)
                {
                    minLife = enemy.health;
                    minLifeEnemy = enemy;
                }

                if (nearestDistance > distance && ec1 < 6)
                {
                    nearestEnemy = enemy;
                    nearestDistance = distance;
                }
            }

            if (nearestEnemy != null)
                enemyChaseCount.Increment(nearestEnemy);
            if (minLifeEnemy != null)
                enemyChaseCount.Increment(minLifeEnemy);

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
            microState[unit] = microstate1;
        }

        foreach (var unit in armies)
        {
            var unitPosition = unit.position;
            float fireRange = analysisSystem.fireRanges[(int)unit.type];
            var microState1 = microState[unit];

            enemyArmies1.ClearSearch(enemyNearby6, unitPosition, 7.5f);
            //enemyArmies1.ClearSearch(enemyInRange, unitPosition, fireRange + 0.15f);
            enemyUnits1.ClearSearch(enemyNearbyAll, unitPosition, 7.5f);
            //int invisibleArmyCount = 0;
            //foreach (var unit1 in enemyNearby6)
            //    if (outOfSightEnemyArmy1.Contains(unit1) && Vector2.Distance(unitPosition, unit1.position) < 6.5)
            //        invisibleArmyCount++;

            //bool visEnemy = enemyNearby6.Count > 0;
            //bool visEnemyAll = enemyNearbyAll.Count > 0;

            myUnits1.ClearSearch(friendNearbys, unitPosition, 4.5f);
            var unitTypeData = analysisSystem.GetUnitTypeData(unit.type);
            bool forward = false;
            bool push = false;
            if (microState1.inEnemyRangeFood < microState1.friendlyNearByFood * 0.7f)
            {
                push = true;
                forward = true;
            }
            else if (microState1.inEnemyRangeFood < microState1.friendlyNearByFood * 1.0f)
            {
                forward = true;
            }
            else if (microState1.inEnemyRangeFood < microState1.friendlyNearByFood * 1.5f + 1 && fireRange < 2.0f)
            {
                forward = true;
            }
            var enemy = microState1.nearestEnemy;
            var enemyMaxRange = microState1.enemyMaxRange;
            if (enemy != null)
            {
                if (enemyMaxRange < fireRange && microState1.nearestDistance > fireRange)
                {
                    forward = true;
                    push = true;
                }

                if (unit.weaponCooldown > 0.1f * 22.4f && fireRange > 2)
                {
                    if (forward)
                    {
                        if (push)
                            commandSystem.OptimiseCommand(unit, Abilities.MOVE, unitPosition.Closer(enemy.position, 0.2f, Math.Min(2.0f, fireRange)));
                        else if (enemyMaxRange + 0.5f >= fireRange)
                            commandSystem.OptimiseCommand(unit, Abilities.ATTACK, enemy.position);
                        else
                            commandSystem.OptimiseCommand(unit, Abilities.MOVE, unitPosition.Closer(enemy.position, -0.3f, Math.Min(fireRange, enemyMaxRange + 1.5f)));
                    }
                    else
                    {
                        commandSystem.OptimiseCommand(unit, Abilities.MOVE, unitPosition.Closer(enemy.position, -0.3f, Math.Min(fireRange, enemyMaxRange + 1.5f)));
                    }
                    esc.Add(unit);
                }
                else if (unit.weaponCooldown <= 1 && microState1.minLifeEnemy != null)
                {
                    commandSystem.EnqueueAbility(unit, Abilities.ATTACK, microState1.minLifeEnemy);
                    esc.Add(unit);
                }
                else if (forward)
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
}
