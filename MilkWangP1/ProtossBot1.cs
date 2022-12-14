using StarDebuCat.Attributes;
using StarDebuCat.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MilkWangBase;
using MilkWangBase.Utility;
using StarDebuCat.Algorithm;

namespace MilkWangP1
{
    public class ProtossBot1
    {
        AnalysisSystem analysisSystem;
        CommandSystem commandSystem;
        MarkerSystem markerSystem;
        BattleSystem battleSystem;
        BuildSystem buildSystem;
        PredicationSystem predicationSystem;

        Random random = new Random();

        [Find("ReadyToPlay")]
        bool readyToPlay;


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

        void Update()
        {
            if (!readyToPlay)
                return;

            if (!initialized)
                PostInitialize();

            if (!enemyFindInit && buildSystem.resourcePoints != null)
                EnemyFindInit();

            //buildSystem.requireUnitCount[UnitType.PROTOSS_ZEALOT] = 0;
            //buildSystem.requireUnitCount[UnitType.PROTOSS_ADEPT] = 6;
            //buildSystem.requireUnitCount[UnitType.PROTOSS_STALKER] = 6;
            //buildSystem.requireUnitCount[UnitType.PROTOSS_SENTRY] = 0;
            buildSystem.requireUnitCount[UnitType.PROTOSS_CYBERNETICSCORE] = 1;
            buildSystem.requireUnitCount[UnitType.PROTOSS_TWILIGHTCOUNCIL] = 1;
            //buildSystem.requireUnitCount[UnitType.PROTOSS_DARKSHRINE] = 1;
            //buildSystem.requireUnitCount[UnitType.PROTOSS_DARKTEMPLAR] = 10;
            //buildSystem.requireUnitCount[UnitType.PROTOSS_TEMPLARARCHIVE] = 1;
            //buildSystem.requireUnitCount[UnitType.PROTOSS_HIGHTEMPLAR] = 2;
            //buildSystem.requireUnitCount[UnitType.PROTOSS_STARGATE] = 2;
            //buildSystem.requireUnitCount[UnitType.PROTOSS_FLEETBEACON] = 1;
            //buildSystem.requireUnitCount[UnitType.PROTOSS_VOIDRAY] = 1;
            //buildSystem.requireUnitCount[UnitType.PROTOSS_ORACLE] = 1;
            //buildSystem.requireUnitCount[UnitType.PROTOSS_PHOENIX] = 1;
            //buildSystem.requireUnitCount[UnitType.PROTOSS_TEMPEST] = 1;
            //buildSystem.requireUnitCount[UnitType.PROTOSS_CARRIER] = 1;
            //buildSystem.requireUnitCount[UnitType.PROTOSS_ROBOTICSFACILITY] = 2;
            //buildSystem.requireUnitCount[UnitType.PROTOSS_ROBOTICSBAY] = 1;
            //buildSystem.requireUnitCount[UnitType.PROTOSS_OBSERVER] = 1;
            //buildSystem.requireUnitCount[UnitType.PROTOSS_IMMORTAL] = 3;
            //buildSystem.requireUnitCount[UnitType.PROTOSS_COLOSSUS] = 1;
            //buildSystem.requireUnitCount[UnitType.PROTOSS_WARPPRISM] = 1;
            //buildSystem.requireUnitCount[UnitType.PROTOSS_DISRUPTOR] = 1;

            int nexusCount = predicationSystem.GetPredictTotal(UnitType.PROTOSS_NEXUS);
            if (buildSystem.workers.Count > 15)
                buildSystem.requireUnitCount[UnitType.PROTOSS_ASSIMILATOR] = 1;
            if (buildSystem.workers.Count > 20)
                buildSystem.requireUnitCount[UnitType.PROTOSS_ASSIMILATOR] = nexusCount * 2;

            buildSystem.requireUnitCount[UnitType.PROTOSS_GATEWAY] = 1;
            buildSystem.requireUnitCount[UnitType.PROTOSS_NEXUS] = (int)analysisSystem.GameLoop / 4032;


            if (predicationSystem.GetPredictTotal(UnitType.PROTOSS_CYBERNETICSCORE) > 0)
            {
                buildSystem.requireUnitCount[UnitType.PROTOSS_GATEWAY] = 3;
                buildSystem.requireUnitCount[UnitType.PROTOSS_ZEALOT] = 2;
                buildSystem.requireUnitCount[UnitType.PROTOSS_ADEPT] = 6;
                buildSystem.requireUnitCount[UnitType.PROTOSS_STALKER] = 6;
            }
            if (predicationSystem.GetPredictTotal(UnitType.PROTOSS_WARPGATE) > 0)
            {
                buildSystem.requireUnitCount[UnitType.PROTOSS_GATEWAY] = 5 - predicationSystem.GetPredictTotal(UnitType.PROTOSS_WARPGATE) + analysisSystem.Minerals / 600;
                buildSystem.requireUnitCount[UnitType.PROTOSS_ADEPT] = 12;
                buildSystem.requireUnitCount[UnitType.PROTOSS_STALKER] = 12;
                //buildSystem.requireUnitCount[UnitType.PROTOSS_SENTRY] = 0;
            }
            if ((int)analysisSystem.GameLoop > 5400)
            {
                buildSystem.requireUnitCount[UnitType.PROTOSS_ZEALOT] = 0;
            }
            int workerCount = Math.Min(nexusCount * 16 + 9, 60);
            buildSystem.requireUnitCount[UnitType.PROTOSS_PROBE] = workerCount;

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

            int attackCount = Math.Min(11 + ((int)analysisSystem.GameLoop / 4032), 20);
            foreach (var army in armies)
            {
                if ((int)analysisSystem.GameLoop > 5400)
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
            Vector2 p1 = analysisSystem.StartLocations[0];
            //List<Vector2> v1 = new List<Vector2>(buildSystem.resourcePoints);
            //v1.Sort((u, v) => Vector2.Distance(u, p1).CompareTo(Vector2.Distance(v, p1)));
            //for (int i = 0; i < Math.Min(3, v1.Count); i++)
            //    enemyBases.Push(v1[i]);
            //battleSystem.mainTarget = enemyBases.Pop();
        }
    }
}
