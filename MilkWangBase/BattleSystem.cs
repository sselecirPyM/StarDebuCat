using MilkWangBase.Utility;
using StarDebuCat.Algorithm;
using StarDebuCat.Attributes;
using StarDebuCat.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MilkWangBase
{
    public enum UnitBattleType
    {
        Undefined = 0,
        AttackMain,
        ProtectArea,
    }
    public class BattleSystem
    {
        AnalysisSystem analysisSystem;
        MarkerSystem markerSystem;
        CommandSystem commandSystem;

        [Find("ReadyToPlay")]
        bool readyToPlay;


        [XFind("CollectUnits", Alliance.Self, "Army")]
        public List<Unit> armies;

        public Dictionary<Unit, UnitBattleType> units = new();


        [XFind("QuadTree", Alliance.Self)]
        public QuadTree<Unit> myUnits1;
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

        List<Unit> enemyNearbys = new();
        List<Unit> enemyNearby6 = new();
        List<Unit> enemyInRange = new();
        List<Unit> enemyNearbyAll = new();
        List<Unit> friendNearbys = new();
        void MicroOperate()
        {
            outOfSightEnemyArmy1.Clear();
            foreach (var unit in outOfSightEnemyArmy)
                outOfSightEnemyArmy1.Add(unit);
            foreach (var unit in armies)
            {
                var unitPosition = unit.position;
                float fireRange = analysisSystem.fireRanges[(int)unit.type];
                Unit enemy = null;
                enemyArmies1.ClearSearch(enemyNearby6, unitPosition, 7.5f);
                enemyArmies1.ClearSearch(enemyInRange, unitPosition, fireRange + 0.15f);
                enemyUnits1.ClearSearch(enemyNearbyAll, unitPosition, 7.5f);
                int invisibleArmyCount = 0;
                foreach (var unit1 in enemyNearby6)
                    if (outOfSightEnemyArmy1.Contains(unit1) && Vector2.Distance(unitPosition, unit1.position) < 6.5)
                        invisibleArmyCount++;

                bool visEnemy = enemyNearby6.Count > 0;
                bool visEnemyAll = enemyNearbyAll.Count > 0;

                myUnits1.ClearSearch(friendNearbys, unitPosition, 4.5f);
                var unitTypeData = analysisSystem.unitTypeDatas[(int)unit.type];
                if (unit.weaponCooldown > 0.1f * 22.4f && fireRange > 2)
                {
                    if (visEnemy)
                    {
                        enemy = enemyNearby6.Nearest(unitPosition);
                        bool forward = enemyNearby6.Count * 2.0f < friendNearbys.Count - 1;

                        float distance = forward ? 0.2f : -0.2f;
                        var targetPosition = unitPosition.Closer(enemy.position, unitTypeData.MovementSpeed * distance, 2.5f);
                        commandSystem.OptimiseMove(unit, targetPosition);
                    }
                    else if (visEnemyAll)
                    {
                        enemy = enemyNearbyAll.Nearest(unitPosition);

                        var targetPosition = unitPosition.Closer(enemy.position, 0.1f * unitTypeData.MovementSpeed, outOfSightEnemyArmy1.Contains(enemy) ? 0.1f : 4);
                        commandSystem.OptimiseMove(unit, targetPosition);
                    }
                    esc.Add(unit);
                }
                else if (visEnemy && unit.weaponCooldown <= 0.1f * 22.4f)
                {
                    enemy = enemyNearby6.Nearest(unitPosition);
                    enemyArmies1.ClearSearch(enemyNearbys, enemy.position, 2.0f);
                    float enemyFood = 0;
                    float enemyLife = 0;
                    foreach (var enemyNearby in enemyNearbys)
                    {
                        var enemy1 = enemyNearby;
                        enemyFood += analysisSystem.unitTypeDatas[(int)enemy1.type].FoodRequired;
                        enemyLife += enemy1.health;
                    }

                    if (enemyInRange.Count > 0 && unit.weaponCooldown == 0)
                    {
                        commandSystem.EnqueueAbility(unit, Abilities.ATTACK, enemyInRange.MinLife());
                    }
                    else if (friendNearbys.Count + 2 < enemyFood * 0.6f && friendNearbys.Count * 20 < enemyLife)
                    {
                        float enemyrange = analysisSystem.fireRanges[(int)enemy.type];
                        var targetPosition = unitPosition.Closer(enemy.position, -0.5f, outOfSightEnemyArmy1.Contains(enemy) ? 0.1f : enemyrange + 1.1f);
                        commandSystem.OptimiseMove(unit, targetPosition);
                        esc.Add(unit);
                    }
                    else if (invisibleArmyCount + 2 > friendNearbys.Count)
                    {
                        var targetPosition = unitPosition.Closer(enemy.position, -0.1f, 6);
                        commandSystem.EnqueueAbility(unit, Abilities.ATTACK, targetPosition);
                        esc.Add(unit);
                    }
                }
                else if (friendNearbys.Count < 3 && analysisSystem.GameLoop > 5376 && !esc.Contains(unit))
                {
                    myUnits1.ClearSearch(friendNearbys, unitPosition, 9.5f);
                    if (friendNearbys.Count > 1)
                    {
                        commandSystem.EnqueueAbility(unit, Abilities.ATTACK, (friendNearbys.GetRandom(random).position + unitPosition) / 2);
                    }
                    else
                    {
                        commandSystem.EnqueueAbility(unit, Abilities.STOP);
                    }
                    esc.Add(unit);
                }
            }
        }
    }
}
