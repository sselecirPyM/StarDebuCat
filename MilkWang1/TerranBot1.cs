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

namespace MilkWang1
{
    public class TerranBot1
    {
        AnalysisSystem analysisSystem;
        PredicationSystem predicationSystem;
        CommandSystem commandSystem;
        MarkerSystem markerSystem;
        BattleSystem battleSystem;
        BuildSystem buildSystem;

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
            buildSystem.requireUnitCount[UnitType.TERRAN_BARRACKS] = 6;
            buildSystem.requireUnitCount[UnitType.TERRAN_BARRACKSTECHLAB] = 0;
            buildSystem.requireUnitCount[UnitType.TERRAN_MARINE] = 100;
            //buildSystem.requireUnitCount[UnitType.TERRAN_MARAUDER] = 10;
            buildSystem.requireUnitCount[UnitType.TERRAN_SCV] = 24;
            buildSystem.requireUnitCount[UnitType.TERRAN_MULE] = 10;
            buildSystem.requireUnitCount[UnitType.TERRAN_ORBITALCOMMAND] = 10;
            //buildSystem.requireUnitCount[UnitType.TERRAN_GHOSTACADEMY] = 1;
            //buildSystem.requireUnitCount[UnitType.TERRAN_GHOST] = 1;
            //buildSystem.requireUnitCount[UnitType.TERRAN_FACTORY] = 1;
            //buildSystem.requireUnitCount[UnitType.TERRAN_FACTORYTECHLAB] = 1;
            //buildSystem.requireUnitCount[UnitType.TERRAN_STARPORT] = 1;
            //buildSystem.requireUnitCount[UnitType.TERRAN_STARPORTTECHLAB] = 1;
            //buildSystem.requireUnitCount[UnitType.TERRAN_ENGINEERINGBAY] = 1;
            //buildSystem.requireUnitCount[UnitType.TERRAN_ARMORY] = 1;
            //buildSystem.requireUnitCount[UnitType.TERRAN_FUSIONCORE] = 1;
            //buildSystem.requireUnitCount[UnitType.TERRAN_REAPER] = 1;
            //buildSystem.requireUnitCount[UnitType.TERRAN_HELLION] = 1;
            //buildSystem.requireUnitCount[UnitType.TERRAN_SIEGETANK] = 1;
            //buildSystem.requireUnitCount[UnitType.TERRAN_WIDOWMINE] = 1;
            //buildSystem.requireUnitCount[UnitType.TERRAN_CYCLONE] = 1;
            //buildSystem.requireUnitCount[UnitType.TERRAN_THOR] = 1;
            //buildSystem.requireUnitCount[UnitType.TERRAN_VIKINGFIGHTER] = 2;
            //buildSystem.requireUnitCount[UnitType.TERRAN_MEDIVAC] = 2;
            //buildSystem.requireUnitCount[UnitType.TERRAN_LIBERATOR] = 2;
            //buildSystem.requireUnitCount[UnitType.TERRAN_BANSHEE] = 1;
            //buildSystem.requireUnitCount[UnitType.TERRAN_RAVEN] = 1;
            //buildSystem.requireUnitCount[UnitType.TERRAN_BATTLECRUISER] = 1;

            //if (buildSystem.workers.Count > 16)
            //    buildSystem.requireUnitCount[UnitType.TERRAN_REFINERY] = 2;

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
            Vector2 p1 = analysisSystem.StartLocations[0];
            //List<Vector2> v1 = new List<Vector2>(buildSystem.resourcePoints);
            //v1.Sort((u, v) => Vector2.Distance(u, p1).CompareTo(Vector2.Distance(v, p1)));
            //for (int i = 0; i < Math.Min(3, v1.Count); i++)
            //    enemyBases.Push(v1[i]);
            //battleSystem.mainTarget = enemyBases.Pop();
        }
    }
}
