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
    public class BuildSystem
    {
        AnalysisSystem analysisSystem;
        CommandSystem commandSystem;
        MarkerSystem markerSystem;
        BattleSystem battleSystem;
        PredicationSystem predicationSystem;
        Random random = new Random();
        [Find("ReadyToPlay")]
        bool readyToPlay;

        [XFind("CollectUnits", Alliance.Self)]
        public List<Unit> myUnits;

        [XFind("CollectUnits", Alliance.Self, "Worker")]
        public List<Unit> workers;

        [XFind("CollectUnits", Alliance.Self, "Worker", "Idle")]
        public List<Unit> idleWorkers;

        [XFind("CollectUnits", Alliance.Self, "CommandCenter")]
        public List<Unit> commandCenters;

        [XFind("CollectUnits", Alliance.Self, UnitType.TERRAN_BARRACKS)]
        public List<Unit> barracks;

        [XFind("CollectUnits", Alliance.Self, UnitType.TERRAN_BARRACKSFLYING)]
        public List<Unit> barracksFlying;

        [XFind("CollectUnits", Alliance.Self, "Factory")]
        public List<Unit> factories;

        [XFind("CollectUnits", Alliance.Self, "Army")]
        public List<Unit> armies;

        [XFind("CollectUnits", Alliance.Neutral, "MineralField")]
        public List<Unit> minerals;

        [XFind("CollectUnits", Alliance.Neutral, "VespeneGeyser")]
        public List<Unit> geysers;

        [XFind("CollectUnits", Alliance.Self, "Refinery")]
        public List<Unit> refinery;

        [XFind("QuadTree", Alliance.Neutral, "MineralField")]
        public QuadTree<Unit> minerals1;

        [XFind("QuadTree", Alliance.Neutral, "VespeneGeyser")]
        public QuadTree<Unit> geysers1;

        [XFind("QuadTree", Alliance.Self, "Refinery")]
        public QuadTree<Unit> refinery1;

        public List<Vector2> resourcePoints;

        public List<Vector2> debugPositions;

        public Dictionary<UnitType, int> requireUnitCount = new();

        int mineralRemain;
        int vespeneRemain;
        int foodRemain;

        Image placementImage;
        Image nonResourcePlacementImage;

        List<Unit> nearByMineral = new();
        List<Unit> workersAvailable = new();
        List<Unit> vespineCanBuild = new();
        HashSet<Unit> workerBuildTargets = new();

        void Update()
        {
            if (!readyToPlay)
                return;

            if (placementImage == null)
            {
                PostInitialize();
            }

            mineralRemain = analysisSystem.Minerals;
            vespeneRemain = analysisSystem.Vespene;
            foodRemain = analysisSystem.FoodCap - analysisSystem.FoodUsed;

            workersAvailable.Clear();
            workerBuildTargets.Clear();
            foreach (var worker in workers)
            {
                if (worker.orders.Count > 0)
                {
                    if (!analysisSystem.abilitiesData[(int)worker.orders[0].AbilityId].IsBuilding)
                        workersAvailable.Add(worker);
                    if (analysisSystem.unitDictionary.TryGetValue(worker.orders[0].TargetUnitTag, out var targetUnit))
                    {
                        workerBuildTargets.Add(targetUnit);
                    }
                }
            }

            foreach (var worker in idleWorkers)
            {
                minerals1.ClearSearch(nearByMineral, worker.position, 20);
                if (nearByMineral.Count == 0)
                    minerals1.Search(nearByMineral, worker.position, 40);
                if (nearByMineral.Count > 0)
                {
                    var unit = nearByMineral.GetRandom(random);
                    commandSystem.EnqueueAbility(worker, Abilities.SMART, unit);
                }
            }

            vespineCanBuild.Clear();
            foreach (var commandCenter in commandCenters)
            {
                var position = commandCenter.position;
                minerals1.ClearSearch(nearByMineral, position, 10);
                geysers1.Search(vespineCanBuild, position, 12);

                var unitTypeData = analysisSystem.unitTypeDatas[(int)commandCenter.type];
                var race = unitTypeData.Race;
                UnitType workerType = GetWorkerType((Race)race);



                if (commandCenter.orders.Count == 0)
                {
                    if (commandCenter.type == UnitType.TERRAN_ORBITALCOMMAND && commandCenter.energy >= 50 && nearByMineral.Count > 0 && NeedBuildUnit(UnitType.TERRAN_MULE))
                    {
                        commandSystem.EnqueueAbility(commandCenter, Abilities.EFFECT_CALLDOWNMULE, nearByMineral.GetRandom(random));
                    }
                    else if (commandCenter.type == UnitType.TERRAN_COMMANDCENTER && NeedBuildUnit(UnitType.TERRAN_ORBITALCOMMAND))
                    {
                        commandSystem.EnqueueAbility(commandCenter, Abilities.MORPH_ORBITALCOMMAND);
                        mineralRemain -= 150;
                    }
                    else if (NeedBuildUnit(workerType))
                    {
                        Train(commandCenter, workerType);
                    }
                }
            }

            if (predicationSystem.foodPrediction20s + foodRemain < 4)
            {
                if (analysisSystem.race != Race.Zerg)
                {
                    if (workersAvailable.Count > 0)
                    {
                        var worker = workersAvailable.GetRandom(random);
                        if (!Build(worker, GetSupplyType(analysisSystem.race), worker.position, 10))
                            mineralRemain -= 100;
                    }
                }
            }

            vespineCanBuild.RemoveAll(u => workerBuildTargets.Contains(u) || refinery1.HitTest(u.position, 1.0f));

            if (vespineCanBuild.Count > 0 && workersAvailable.Count > 0)
            {
                var vespine = vespineCanBuild.GetRandom(random);
                var worker = workersAvailable.GetRandom(random);
                var refineryType = GetRefineryType(worker.type);
                if (NeedBuildUnit(refineryType))
                {
                    workersAvailable.Remove(worker);
                    commandSystem.EnqueueBuild(worker, refineryType, vespine);
                }
            }
            if (workersAvailable.Count > 0 && refinery.Count > 0)
            {
                var worker = workersAvailable.GetRandom(random);
                if (worker.orders.Count > 0 && worker.orders[0].TargetUnitTag != 0)
                {
                    if (analysisSystem.unitDictionary.TryGetValue(worker.orders[0].TargetUnitTag, out var target) &&
                        DData.Refinery.Contains(target.type) && target.assignedHarvesters > 3)
                    {
                        workersAvailable.Remove(worker);
                        commandSystem.EnqueueAbility(worker, Abilities.STOP);
                    }
                }
                var _refinery = refinery.GetRandom(random);
                if (_refinery.assignedHarvesters < 3 && _refinery.buildProgress == 1)
                {
                    workersAvailable.Remove(worker);
                    commandSystem.EnqueueAbility(worker, Abilities.HARVEST_GATHER, _refinery);
                }
            }

            foreach (var barracks1 in barracksFlying)
            {
                if (barracks1.orders.Count == 0)
                {
                    commandSystem.EnqueueAbility(barracks1, Abilities.LAND_BARRACKS, barracks1.position + new Vector2(random.NextSingle() * 10 - 5, random.NextSingle() * 10 - 5));
                }
            }
            foreach (var barracks1 in barracks)
            {
                var labType = UnitType.TERRAN_BARRACKSTECHLAB;
                if (barracks1.addOnTag == 0 && barracks1.orders.Count == 0 && barracks1.buildProgress == 1 && NeedBuildUnit(labType))
                {
                    Build(barracks1, labType, barracks1.position, 5);
                }
            }
            foreach (var factory in factories)
            {
                if (factory.buildProgress < 0.01)
                {
                    commandSystem.EnqueueAbility(factory, Abilities.SMART, battleSystem.protectPosition);
                }

                UnitType trainUnitType = UnitType.INVALID;
                if (factory.type == UnitType.TERRAN_BARRACKS)
                {
                    trainUnitType = UnitType.TERRAN_MARINE;
                    if (factory.addOnTag != 0 && ReadyToBuild(UnitType.TERRAN_MARAUDER))
                        trainUnitType = UnitType.TERRAN_MARAUDER;
                }
                else if (factory.type == UnitType.PROTOSS_GATEWAY)
                {
                    trainUnitType = UnitType.PROTOSS_ZEALOT;
                    if (ReadyToBuild(UnitType.PROTOSS_SENTRY))
                        trainUnitType = UnitType.PROTOSS_SENTRY;
                    if (ReadyToBuild(UnitType.PROTOSS_ADEPT))
                        trainUnitType = UnitType.PROTOSS_ADEPT;
                    if (ReadyToBuild(UnitType.PROTOSS_STALKER))
                        trainUnitType = UnitType.PROTOSS_STALKER;
                }
                else if (factory.type == UnitType.PROTOSS_STARGATE)
                {
                    trainUnitType = UnitType.PROTOSS_PHOENIX;
                    if (ReadyToBuild(UnitType.PROTOSS_VOIDRAY))
                        trainUnitType = UnitType.PROTOSS_VOIDRAY;
                    if (ReadyToBuild(UnitType.PROTOSS_CARRIER))
                        trainUnitType = UnitType.PROTOSS_CARRIER;
                }

                if (factory.orders.Count == 0 && factory.buildProgress == 1)
                {
                    if (NeedBuildUnit(trainUnitType))
                        Train(factory, trainUnitType);
                }
            }

            foreach (var buildingType in BuildBuildings)
            {
                if (workersAvailable.Count > 0 && ReadyToBuild(buildingType))
                {
                    var worker = workersAvailable.GetRandom(random);
                    workersAvailable.Remove(worker);
                    Build(worker, buildingType, worker.position, 10);
                }
            }
        }

        bool Build(Unit unit, UnitType unitType, Vector2 position, float randomSize)
        {
            var unitTypeData = analysisSystem.unitTypeDatas[(int)unitType];
            int mineralCost = (int)unitTypeData.MineralCost;
            int vespeneCost = (int)unitTypeData.VespeneCost;
            int foodCost = (int)unitTypeData.FoodRequired;
            if (mineralRemain < mineralCost || vespeneRemain < vespeneCost || foodRemain < foodCost && foodCost != 0)
                return false;

            position += new Vector2(random.NextFloat(-randomSize, randomSize), random.NextFloat(-randomSize, randomSize));
            for (int i = 0; i < 7; i++)
                if (placementImage.Query(position) == 0)
                {
                    position += new Vector2(random.NextFloat(-randomSize, randomSize), random.NextFloat(-randomSize, randomSize));
                }
                else
                {
                    break;
                }
            predicationSystem.predicatedUnitTypes.Increment(unitType);
            commandSystem.EnqueueBuild(unit, unitType, position);
            mineralRemain -= mineralCost;
            vespeneRemain -= vespeneCost;
            foodRemain -= foodCost;
            return true;
        }

        int GetRequireUnitCount(UnitType unitType)
        {
            requireUnitCount.TryGetValue(unitType, out int wantBuildCount);
            return wantBuildCount;
        }

        bool ReadyToBuild(UnitType unitType)
        {
            if (!predicationSystem.canBuildUnits.Contains(unitType) && ResourceEnough(unitType))
                return false;
            return predicationSystem.GetPredictTotal(unitType) < GetRequireUnitCount(unitType);
        }

        bool NeedBuildUnit(UnitType unitType)
        {
            if (!predicationSystem.canBuildUnits.Contains(unitType))
                return false;
            return predicationSystem.GetPredictTotal(unitType) < GetRequireUnitCount(unitType);
        }

        bool ResourceEnough(UnitType unitType)
        {
            var unitTypeData = analysisSystem.unitTypeDatas[(int)unitType];
            int mineralCost = (int)unitTypeData.MineralCost;
            int vespeneCost = (int)unitTypeData.VespeneCost;
            int foodCost = (int)unitTypeData.FoodRequired;
            if (mineralRemain < mineralCost || vespeneRemain < vespeneCost || foodRemain < foodCost && foodCost != 0)
                return false;
            return true;
        }

        bool Train(Unit unit, UnitType unitType)
        {
            var unitTypeData = analysisSystem.unitTypeDatas[(int)unitType];
            int mineralCost = (int)unitTypeData.MineralCost;
            int vespeneCost = (int)unitTypeData.VespeneCost;
            int foodCost = (int)unitTypeData.FoodRequired;
            if (mineralRemain < mineralCost || vespeneRemain < vespeneCost || foodRemain < foodCost && foodCost != 0)
                return false;
            predicationSystem.predicatedUnitTypes.Increment(unitType);
            commandSystem.EnqueueTrain(unit, unitType);
            mineralRemain -= mineralCost;
            vespeneRemain -= vespeneCost;
            foodRemain -= foodCost;
            return true;
        }

        void PostInitialize()
        {
            placementImage = new Image(analysisSystem.build);
            nonResourcePlacementImage = new Image(analysisSystem.build);

            debugPositions = new();
            foreach (var mineral in minerals)
            {
                var positionf = mineral.position;
                var position = ((int)(positionf.X + 0.5f), (int)positionf.Y);
                for (int i = -4; i < 4; i++)
                    for (int j = -3; j < 4; j++)
                        if (!((i == -4 || i == 3) && (j == -3 || j == 3)))
                        {
                            //if (placementImage.Query(position.Item1 + i, position.Item2 + j) != 0)
                            //{
                            //    debugPositions.Add(new(position.Item1 + i + 0.5f, position.Item2 + j + 0.5f));
                            //}
                            placementImage.Write(position.Item1 + i, position.Item2 + j, false);
                        }
            }

            foreach (var geyser in geysers)
            {
                var positionf = geyser.position;
                var position = ((int)positionf.X, (int)positionf.Y);
                for (int i = -4; i < 5; i++)
                    for (int j = -4; j < 5; j++)
                        if (!((i == -4 || i == 4) && (j == -4 || j == 4)))
                        {
                            placementImage.Write(position.Item1 + i, position.Item2 + j, false);
                        }
            }
            resourcePoints = new List<Vector2>();
            minerals1.Group(resourcePoints, 5);
            List<Unit> m1 = new();
            for (int i = 0; i < resourcePoints.Count; i++)
                resourcePoints[i] = AdjustResourcePoint(resourcePoints[i], m1);
            foreach (var resourcePoint in resourcePoints)
            {
                for (int i = -2; i < 3; i++)
                    for (int j = -2; j < 3; j++)
                    {
                        placementImage.Write((int)resourcePoint.X + i, (int)resourcePoint.Y + j, false);
                        //debugPositions.Add(new(resourcePoint.X + i, resourcePoint.Y + j));
                    }
            }

            //foreach (var resourcePoint in resourcePoints)
            //    markerSystem.AddMark(resourcePoint, "ResourcePoint", 1000);
        }

        Vector2 AdjustResourcePoint(Vector2 point, List<Unit> minerals)
        {
            point = new Vector2(MathF.Round(point.X) + 0.5f, MathF.Round(point.Y) + 0.5f);
            minerals1.ClearSearch(minerals, point, 10);
            Vector2 result = point;
            float current = float.MaxValue;
            for (int i = -8; i < 8; i++)
                for (int j = -8; j < 8; j++)
                {
                    Vector2 testPoint = new Vector2(point.X + i, point.Y + j);
                    if (CanPlaceCommandCenter(testPoint))
                    {
                        float value = minerals.Sum(u => Vector2.Distance(u.position, testPoint));
                        if (value < current)
                        {
                            result = new Vector2(point.X + i, point.Y + j);
                            current = value;
                        }
                    }
                }

            return result;
        }

        bool CanPlaceCommandCenter(Vector2 position)
        {
            int x = (int)position.X;
            int y = (int)position.Y;
            for (int i = -2; i < 3; i++)
                for (int j = -2; j < 3; j++)
                {
                    if (placementImage.Query(x + i, y + j) == 0)
                    {
                        return false;
                    }
                }
            return true;
        }

        UnitType GetSupplyType(Race race)
        {
            return race switch
            {
                Race.Terran => UnitType.TERRAN_SUPPLYDEPOT,
                Race.Protoss => UnitType.PROTOSS_PYLON,
                Race.Zerg => UnitType.ZERG_OVERLORD,
                _ => UnitType.TERRAN_SUPPLYDEPOT,
            };
        }

        UnitType GetWorkerType(Race race)
        {
            return race switch
            {
                Race.Terran => UnitType.TERRAN_SCV,
                Race.Protoss => UnitType.PROTOSS_PROBE,
                Race.Zerg => UnitType.ZERG_DRONE,
                _ => UnitType.TERRAN_SCV,
            };
        }

        UnitType GetRefineryType(UnitType unitType)
        {
            return unitType switch
            {
                UnitType.TERRAN_SCV => UnitType.TERRAN_REFINERY,
                UnitType.PROTOSS_PROBE => UnitType.PROTOSS_ASSIMILATOR,
                UnitType.ZERG_DRONE => UnitType.ZERG_EXTRACTOR,
                _ => UnitType.ZERG_EXTRACTOR,
            };
        }

        static List<UnitType> BuildBuildings = new()
        {
            UnitType.TERRAN_BARRACKS,
            UnitType.TERRAN_ENGINEERINGBAY,
            UnitType.TERRAN_GHOSTACADEMY,
            UnitType.TERRAN_FACTORY,
            UnitType.TERRAN_ARMORY,
            UnitType.TERRAN_STARPORT,
            UnitType.TERRAN_FUSIONCORE,
            UnitType.PROTOSS_GATEWAY,
            UnitType.PROTOSS_CYBERNETICSCORE,
            UnitType.PROTOSS_FORGE,
            UnitType.PROTOSS_TWILIGHTCOUNCIL,
            UnitType.PROTOSS_TEMPLARARCHIVE,
            UnitType.PROTOSS_DARKSHRINE,
            UnitType.PROTOSS_ROBOTICSBAY,
            UnitType.PROTOSS_ROBOTICSFACILITY,
            UnitType.PROTOSS_STARGATE,
            UnitType.PROTOSS_FLEETBEACON,
            UnitType.ZERG_SPAWNINGPOOL,
            UnitType.ZERG_EVOLUTIONCHAMBER,
        };
    }
}
