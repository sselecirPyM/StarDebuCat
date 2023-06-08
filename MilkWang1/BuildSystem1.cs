using MilkWangBase;
using MilkWangBase.Attributes;
using MilkWangBase.Utility;
using StarDebuCat.Algorithm;
using StarDebuCat.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MilkWang1;

public class BuildSystem1
{
    AnalysisSystem1 analysisSystem;
    CommandSystem1 commandSystem;
    PredicationSystem1 predicationSystem;
    Random random = new Random();

    [XFind("CollectUnits", Alliance.Self)]
    public List<Unit> myUnits;

    [XFind("CollectUnits", Alliance.Self, "Worker")]
    public List<Unit> workers;

    [XFind("CollectUnits", Alliance.Self, "Worker", "Idle")]
    public List<Unit> idleWorkers;

    [XFind("CollectUnits", Alliance.Self, "CommandCenter")]
    public List<Unit> commandCenters;

    [XFind("CollectUnits", Alliance.Self, "Building")]
    public List<Unit> buildings;


    [XFind("CollectUnits", Alliance.Self, "Factory")]
    public List<Unit> factories;

    [XFind("CollectUnits", Alliance.Self, "Army")]
    public List<Unit> armies;

    [XFind("CollectUnits", Alliance.Neutral, "MineralField")]
    public List<Unit> minerals;

    [XFind("CollectUnits", Alliance.Neutral, "VespeneGeyser")]
    public List<Unit> geysers;

    [XFind("CollectUnits", Alliance.Self, "Building", "Refinery")]
    public List<Unit> _refinery;

    [XFind("QuadTree", Alliance.Enemy, "Army")]
    public QuadTree<Unit> enemyArmies1;

    [XFind("QuadTree", Alliance.Neutral, "MineralField")]
    public QuadTree<Unit> minerals1;

    [XFind("QuadTree", Alliance.Neutral, "VespeneGeyser")]
    public QuadTree<Unit> geysers1;

    [XFind("QuadTree", Alliance.Self, "Refinery")]
    public QuadTree<Unit> refinery1;

    public List<Vector2> resourcePoints;

    public Dictionary<UnitType, int> requireUnitCount = new();
    public HashSet<UpgradeType> requireUpgrade = new();

    public BotData BotData;

    public GameData GameData;

    public bool randomlyUpgrade = true;

    SC2Resource resourceRemain = new SC2Resource();

    Image placementImage;
    Image nonResourcePlacementImage;

    List<Unit> nearByMineral = new();
    List<Unit> nearByMineral1 = new();
    List<Unit> workersAvailable = new();
    List<Unit> vespeneCanBuild = new();
    HashSet<Unit> workerBuildTargets = new();

    UnitType[] idleUnitTypes;
    List<Unit> idleUnits1 = new();
    void Initialize()
    {
        idleUnitTypes = BotData.onIdle.Keys.ToArray();
    }

    void Update()
    {
        if (placementImage == null)
        {
            PostInitialize();
        }
        var frameResource = analysisSystem.currentFrameResource;

        resourceRemain.mineral = frameResource.Minerals;
        resourceRemain.vespene = frameResource.Vespene;
        resourceRemain.food = frameResource.FoodCap - frameResource.FoodUsed;

        workersAvailable.Clear();
        workerBuildTargets.Clear();
        foreach (var worker1 in workers)
        {
            if (worker1.TryGetOrder(out var order))
            {
                if (!analysisSystem.abilitiesData[(int)order.AbilityId].IsBuilding)
                    workersAvailable.Add(worker1);
                if (analysisSystem.unitDictionary.TryGetValue(order.TargetUnitTag, out var targetUnit))
                    workerBuildTargets.Add(targetUnit);
            }
        }

        nearByMineral.Clear();
        foreach (var commandCenter in commandCenters)
        {
            minerals1.Search(nearByMineral, commandCenter.position, 10);
        }

        foreach (var worker1 in idleWorkers)
        {
            if (nearByMineral.TryGetRandom(random, out var mineral))
            {
                commandSystem.OptimiseCommand(worker1, Abilities.HARVEST_GATHER, mineral);
            }
        }

        vespeneCanBuild.Clear();
        foreach (var commandCenter in commandCenters)
        {
            if (commandCenter.buildProgress > 0.6f)
                geysers1.Search(vespeneCanBuild, commandCenter.position, 12);
        }
        vespeneCanBuild.RemoveAll(u => workerBuildTargets.Contains(u) || refinery1.HitTest(u.position, 1.0f));

        foreach (var commandCenter in commandCenters)
        {
            var position = commandCenter.position;
            minerals1.ClearSearch(nearByMineral1, position, 10);
            UnitType workerType = BotData.workerType;

            if (commandCenter.orders.Count == 0)
            {
                if (commandCenter.type == UnitType.TERRAN_ORBITALCOMMAND && commandCenter.energy >= 50 &&
                    nearByMineral1.TryGetRandom(random, out var mineral1) && NeedBuildUnit(UnitType.TERRAN_MULE))
                {
                    commandSystem.OptimiseCommand(commandCenter, Abilities.EFFECT_CALLDOWNMULE, mineral1);
                }
                else if (commandCenter.type == UnitType.TERRAN_COMMANDCENTER && NeedBuildUnit(UnitType.TERRAN_ORBITALCOMMAND))
                {
                    commandSystem.OptimiseCommand(commandCenter, Abilities.MORPH_ORBITALCOMMAND);
                    resourceRemain.mineral -= 150;
                }
                else if (commandCenter.type == UnitType.TERRAN_COMMANDCENTER && NeedBuildUnit(UnitType.TERRAN_PLANETARYFORTRESS))
                {
                    commandSystem.OptimiseCommand(commandCenter, Abilities.MORPH_PLANETARYFORTRESS);
                    resourceRemain.mineral -= 150;
                    resourceRemain.vespene -= 150;
                }
                else if (NeedBuildUnit(workerType))
                {
                    Train(commandCenter, workerType);
                }
            }
        }
        Unit worker;

        foreach (var commandCenter in DData.CommandCenters1)
        {
            if (NeedBuildUnit(commandCenter) && TryGetAvailableWorker(out worker))
            {
                var point1 = Vector2.Zero;
                var point = point1;

                bool canBuild = false;
                for (int i = 0; i < 3; i++)
                {
                    point1 = resourcePoints.GetRandom(random);
                    if (commandCenters.All(u => Vector2.Distance(u.position, point1) > 7))
                    {
                        canBuild = true;
                        point = point1;
                    }
                    if (canBuild && commandCenters.Any(u => Vector2.Distance(u.position, point1) < 60))
                    {
                        break;
                    }
                }
                if (canBuild && Build(worker, commandCenter, point))
                {

                }
                else
                {
                    resourceRemain.mineral -= (int)analysisSystem.GetUnitTypeData(commandCenter).MineralCost;
                }
            }
        }

        foreach (var needWorker in predicationSystem.needWorkers)
        {
            if (!enemyArmies1.HitTest(needWorker.position, 6) && TryGetAvailableWorker(out worker))
            {
                commandSystem.EnqueueAbility(worker, Abilities.SMART, needWorker);
            }
        }

        if (predicationSystem.foodPrediction20s + resourceRemain.food < 4 && BotData.supplyBuilding != UnitType.INVALID)
        {
            if (TryGetAvailableWorker(out worker))
            {
                var unitType = BotData.supplyBuilding;
                var unitData = analysisSystem.GetUnitTypeData(unitType);
                if (!Build(worker, unitType, worker.position, 10))
                    resourceRemain.mineral -= (int)unitData.MineralCost;
                else
                    workersAvailable.Remove(worker);
            }
        }


        if (vespeneCanBuild.TryGetRandom(random, out var vespene1) && TryGetAvailableWorker(out worker))
        {
            var refineryType = GetRefineryType(worker.type);
            if (ReadyToBuild(refineryType))
            {
                workersAvailable.Remove(worker);
                Build(worker, refineryType, vespene1);
            }
        }
        if (workersAvailable.Count > 12 && _refinery.TryGetRandom(random, out var refinery2) && TryGetAvailableWorker(out worker))
        {
            if (worker.TryGetOrder(out var order) && order.TargetUnitTag != 0)
            {
                if (analysisSystem.unitDictionary.TryGetValue(order.TargetUnitTag, out var target) && GameData.refineries.Contains(target.type) &&
                    (target.assignedHarvesters > 3 || target.buildProgress != 1))
                {
                    workersAvailable.Remove(worker);
                    commandSystem.EnqueueAbility(worker, Abilities.STOP);
                }
            }
            if (refinery2.assignedHarvesters < 3 && refinery2.vespeneContents > 0 && refinery2.buildProgress == 1 && Vector2.Distance(worker.position, refinery2.position) < 10)
            {
                workersAvailable.Remove(worker);
                commandSystem.OptimiseCommand(worker, Abilities.HARVEST_GATHER, refinery2);
            }
        }

        if (TryGetAvailableWorker(out worker))
        {
            if (worker.TryGetOrder(out var order) && order.TargetUnitTag != 0 &&
                analysisSystem.unitDictionary.TryGetValue(order.TargetUnitTag, out var target) &&
                GameData.commandCenters.Contains(target.type) &&
                target.assignedHarvesters > target.idealHarvesters * 1.5)
            {
                workersAvailable.Remove(worker);
                if (nearByMineral.TryGetRandom(random, out var unit))
                {
                    commandSystem.OptimiseCommand(worker, Abilities.HARVEST_GATHER, unit);
                }
            }
        }

        analysisSystem._CollectUnits(idleUnits1, new object[] { Alliance.Self, idleUnitTypes });
        foreach (var unit in idleUnits1)
        {
            if (unit.orders.Count == 0)
            {
                commandSystem.OptimiseCommand(unit, BotData.onIdle[unit.type], unit.position + random.NextVector2(-5, 5));
            }
        }

        foreach (var factory in factories)
        {
            UnitType labType = GetLabType(factory.type);
            if (labType == UnitType.INVALID)
                continue;

            if (factory.addOnTag == 0 && factory.orders.Count == 0 && factory.buildProgress == 1 && ReadyToBuild(labType))
            {
                Build(factory, labType, factory.position, 5);
            }
        }
        foreach (var factory in factories)
        {
            if (factory.orders.Count != 0 || factory.buildProgress != 1)
                continue;

            UnitType trainUnitType = UnitType.INVALID;
            if (analysisSystem.Spawners.TryGetValue(factory.type, out var unitTypes))
            {
                var _trainType = unitTypes.GetRandom(random);
                if (ReadyToBuild(_trainType))
                {
                    if (analysisSystem.GetUnitTypeData(_trainType).RequireAttached && factory.addOnTag == 0)
                    {
                        continue;
                    }
                    trainUnitType = _trainType;
                }
            }

            if (NeedBuildUnit(trainUnitType))
            {
                if (factory.type == UnitType.PROTOSS_WARPGATE)
                    Warp(factory, trainUnitType, factory.position, 10);
                else
                    Train(factory, trainUnitType);
            }
        }
        foreach (var myUnit in myUnits)
        {
            if (myUnit.orders.Count != 0 || myUnit.buildProgress != 1)
                continue;
            if (analysisSystem.UpgradesResearcher.TryGetValue(myUnit.type, out var upgrades))
            {
                var upgrade = upgrades.GetRandom(random);
                if (ReadyToUpgrade(upgrade))
                {
                    Upgrade(myUnit, upgrade);
                }
            }
        }

        var BuildBuildings = BotData.unitLists["BuildBuildings"];
        foreach (var buildingType in BuildBuildings)
        {
            if (TryGetAvailableWorker(out worker) && ReadyToBuild(buildingType))
            {
                workersAvailable.Remove(worker);
                Build(worker, buildingType, worker.position, 10);
            }
        }

        foreach (var notCompleted in predicationSystem.buildNotCompletedUnits)
        {
            if (notCompleted.health < notCompleted.healthMax * notCompleted.buildProgress * 0.3f)
            {
                commandSystem.EnqueueAbility(notCompleted, Abilities.CANCEL);
            }
        }
    }

    bool Warp(Unit unit, UnitType unitType, Vector2 position, float randomSize)
    {
        var unitTypeData = analysisSystem.GetUnitTypeData(unitType);
        int mineralCost = (int)unitTypeData.MineralCost;
        int vespeneCost = (int)unitTypeData.VespeneCost;
        int foodCost = (int)unitTypeData.FoodRequired;
        if (!resourceRemain.TryPay(mineralCost, vespeneCost, foodCost))
            return false;

        position += random.NextVector2(-randomSize, randomSize);
        for (int i = 0; i < 7; i++)
            if (placementImage.Query(position) == 0)
            {
                position += random.NextVector2(-randomSize, randomSize);
            }
            else
            {
                break;
            }
        //predicationSystem.predicatedUnitTypes.Increment(unitType);
        predicationSystem.predicatedEquivalentUnitTypes.Increment(unitType);
        Abilities ability = unitType switch
        {
            UnitType.PROTOSS_ZEALOT => Abilities.TRAINWARP_ZEALOT,
            UnitType.PROTOSS_STALKER => Abilities.TRAINWARP_STALKER,
            UnitType.PROTOSS_SENTRY => Abilities.TRAINWARP_SENTRY,
            UnitType.PROTOSS_ADEPT => Abilities.TRAINWARP_ADEPT,
            UnitType.PROTOSS_HIGHTEMPLAR => Abilities.TRAINWARP_HIGHTEMPLAR,
            UnitType.PROTOSS_DARKTEMPLAR => Abilities.TRAINWARP_DARKTEMPLAR,
            _ => Abilities.INVALID,
        };
        commandSystem.OptimiseCommand(unit, ability, position);
        return true;
    }

    bool Build(Unit unit, UnitType unitType, Vector2 position, float randomSize)
    {
        position += random.NextVector2(-randomSize, randomSize);
        for (int i = 0; i < 7; i++)
            if (placementImage.Query(position) == 0)
            {
                position += random.NextVector2(-randomSize, randomSize);
            }
            else
            {
                break;
            }
        return Build(unit, unitType, position);
    }

    bool Build(Unit unit, UnitType unitType, Vector2 position)
    {
        var unitTypeData = analysisSystem.GetUnitTypeData(unitType);
        int mineralCost = (int)unitTypeData.MineralCost;
        int vespeneCost = (int)unitTypeData.VespeneCost;
        int foodCost = (int)unitTypeData.FoodRequired;
        if (!resourceRemain.TryPay(mineralCost, vespeneCost, foodCost))
            return false;

        //predicationSystem.predicatedUnitTypes.Increment(unitType);
        predicationSystem.predicatedEquivalentUnitTypes.Increment(unitType);
        commandSystem.EnqueueBuild(unit, unitType, position);
        return true;
    }

    bool Build(Unit unit, UnitType unitType, Unit target)
    {
        var unitTypeData = analysisSystem.GetUnitTypeData(unitType);
        int mineralCost = (int)unitTypeData.MineralCost;
        int vespeneCost = (int)unitTypeData.VespeneCost;
        int foodCost = (int)unitTypeData.FoodRequired;
        if (!resourceRemain.TryPay(mineralCost, vespeneCost, foodCost))
            return false;

        //predicationSystem.predicatedUnitTypes.Increment(unitType);
        predicationSystem.predicatedEquivalentUnitTypes.Increment(unitType);
        commandSystem.EnqueueBuild(unit, unitType, target);
        return true;
    }

    bool Upgrade(Unit unit, UpgradeType upgrade)
    {
        var upgradeData = analysisSystem.upgradeDatas[(int)upgrade];
        int mineralCost = (int)upgradeData.MineralCost;
        int vespeneCost = (int)upgradeData.VespeneCost;
        if (!predicationSystem.canUpgrades.Contains(upgrade))
            return false;
        if (!resourceRemain.TryPay(mineralCost, vespeneCost, 0))
            return false;

        commandSystem.EnqueueAbility(unit, (Abilities)upgradeData.AbilityId);
        return true;
    }

    int GetRequireUnitCount(UnitType unitType)
    {
        requireUnitCount.TryGetValue(unitType, out int wantBuildCount);
        return wantBuildCount;
    }

    bool ReadyToBuild(UnitType unitType)
    {
        return NeedBuildUnit(unitType) && ResourceEnough(unitType);
    }

    bool NeedBuildUnit(UnitType unitType)
    {
        if (!predicationSystem.canBuildUnits.Contains(unitType))
            return false;
        return predicationSystem.GetPredictEquivalentTotal(unitType) < GetRequireUnitCount(unitType);
    }

    bool ResourceEnough(UnitType unitType)
    {
        var unitTypeData = analysisSystem.GetUnitTypeData(unitType);
        int mineralCost = (int)unitTypeData.MineralCost;
        int vespeneCost = (int)unitTypeData.VespeneCost;
        int foodCost = (int)unitTypeData.FoodRequired;
        return resourceRemain.ResourceEnough(mineralCost, vespeneCost, foodCost);
    }

    bool ReadyToUpgrade(UpgradeType upgrade)
    {
        if (!predicationSystem.canUpgrades.Contains(upgrade))
            return false;
        if (!requireUpgrade.Contains(upgrade) && !randomlyUpgrade)
            return false;
        return true;
    }

    bool Train(Unit unit, UnitType unitType)
    {
        var unitTypeData = analysisSystem.GetUnitTypeData(unitType);
        int mineralCost = (int)unitTypeData.MineralCost;
        int vespeneCost = (int)unitTypeData.VespeneCost;
        int foodCost = (int)unitTypeData.FoodRequired;
        if (!resourceRemain.TryPay(mineralCost, vespeneCost, foodCost))
            return false;
        //predicationSystem.predicatedUnitTypes.Increment(unitType);
        predicationSystem.predicatedEquivalentUnitTypes.Increment(unitType);
        commandSystem.EnqueueTrain(unit, unitType);
        return true;
    }

    UnitType GetLabType(UnitType unitType)
    {
        return unitType switch
        {
            UnitType.TERRAN_BARRACKS => UnitType.TERRAN_BARRACKSTECHLAB,
            UnitType.TERRAN_BARRACKSFLYING => UnitType.TERRAN_BARRACKSTECHLAB,
            UnitType.TERRAN_FACTORY => UnitType.TERRAN_FACTORYTECHLAB,
            UnitType.TERRAN_FACTORYFLYING => UnitType.TERRAN_FACTORYTECHLAB,
            UnitType.TERRAN_STARPORT => UnitType.TERRAN_STARPORTTECHLAB,
            UnitType.TERRAN_STARPORTFLYING => UnitType.TERRAN_STARPORTTECHLAB,
            _ => UnitType.INVALID
        };
    }

    UnitType GetRefineryType(UnitType unitType)
    {
        return unitType switch
        {
            UnitType.TERRAN_SCV => UnitType.TERRAN_REFINERY,
            UnitType.PROTOSS_PROBE => UnitType.PROTOSS_ASSIMILATOR,
            UnitType.ZERG_DRONE => UnitType.ZERG_EXTRACTOR,
            _ => UnitType.INVALID,
        };
    }

    void PostInitialize()
    {
        placementImage = new Image(analysisSystem.build);
        nonResourcePlacementImage = new Image(analysisSystem.build);

        foreach (var mineral in minerals)
        {
            var positionf = mineral.position;
            var position = ((int)(positionf.X + 0.5f), (int)positionf.Y);
            for (int i = -4; i < 4; i++)
                for (int j = -3; j < 4; j++)
                    if (!((i == -4 || i == 3) && (j == -3 || j == 3)))
                    {
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
        minerals1.Group(resourcePoints, 5, 4);
        List<Unit> m1 = new();
        for (int i = 0; i < resourcePoints.Count; i++)
            resourcePoints[i] = AdjustResourcePoint(resourcePoints[i], m1);
        foreach (var resourcePoint in resourcePoints)
        {
            for (int i = -2; i < 3; i++)
                for (int j = -2; j < 3; j++)
                {
                    placementImage.Write((int)resourcePoint.X + i, (int)resourcePoint.Y + j, false);
                }
        }
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

    public bool TryGetAvailableWorker(out Unit worker)
    {
        if (workersAvailable.Count == 0)
        {
            worker = null;
            return false;
        }
        else
        {
            worker = workersAvailable.GetRandom(random);
            return true;
        }
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
}
