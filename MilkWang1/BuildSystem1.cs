﻿using MilkWangBase.Attributes;
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
    public List<Unit> refinery;

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

    public bool randomlyUpgrade = true;

    SC2Resource resourceRemain = new SC2Resource();

    Image placementImage;
    Image nonResourcePlacementImage;

    List<Unit> nearByMineral = new();
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

        foreach (var worker1 in idleWorkers)
        {
            if (commandCenters.Count > 0)
            {
                var commanderCenter = commandCenters.GetRandom(random);
                for (int i = 0; i < 3; i++)
                    if (commanderCenter.assignedHarvesters > commanderCenter.idealHarvesters * 1.5f)
                        commanderCenter = commandCenters.GetRandom(random);

                float range = 10;
                minerals1.ClearSearch(nearByMineral, commanderCenter.position, range);
                if (nearByMineral.Count == 0)
                    minerals1.Search(nearByMineral, commanderCenter.position, 40);
                if (nearByMineral.Count == 0)
                    minerals1.Search(nearByMineral, commanderCenter.position, 80);
                if (nearByMineral.Count > 0)
                {
                    var unit = nearByMineral.GetRandom(random);
                    commandSystem.EnqueueAbility(worker1, Abilities.SMART, unit);
                }
            }
        }

        vespeneCanBuild.Clear();
        foreach (var commandCenter in commandCenters)
            geysers1.Search(vespeneCanBuild, commandCenter.position, 12);

        foreach (var commandCenter in commandCenters)
        {
            var position = commandCenter.position;
            minerals1.ClearSearch(nearByMineral, position, 10);
            UnitType workerType = BotData.workerType;

            if (commandCenter.orders.Count == 0)
            {
                if (commandCenter.type == UnitType.TERRAN_ORBITALCOMMAND && commandCenter.energy >= 50 &&
                    nearByMineral.TryGetRandom(random, out var mineral1) && NeedBuildUnit(UnitType.TERRAN_MULE))
                {
                    commandSystem.EnqueueAbility(commandCenter, Abilities.EFFECT_CALLDOWNMULE, mineral1);
                }
                else if (commandCenter.type == UnitType.TERRAN_COMMANDCENTER && NeedBuildUnit(UnitType.TERRAN_ORBITALCOMMAND))
                {
                    commandSystem.EnqueueAbility(commandCenter, Abilities.MORPH_ORBITALCOMMAND);
                    resourceRemain.mineral -= 150;
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

        if (predicationSystem.foodPrediction20s + resourceRemain.food < 4 && BotData.supplyBuilding != UnitType.INVALID)
        {
            if (TryGetAvailableWorker(out worker))
            {
                var unitType = BotData.supplyBuilding;
                var unitData = analysisSystem.GetUnitTypeData(unitType);
                if (!Build(worker, unitType, worker.position, 10))
                    resourceRemain.mineral -= (int)unitData.MineralCost;
            }
        }

        vespeneCanBuild.RemoveAll(u => workerBuildTargets.Contains(u) || refinery1.HitTest(u.position, 1.0f));

        if (vespeneCanBuild.Count > 0 && TryGetAvailableWorker(out worker))
        {
            var vespene = vespeneCanBuild.GetRandom(random);
            var refineryType = GetRefineryType(worker.type);
            if (NeedBuildUnit(refineryType))
            {
                if (ResourceEnough(refineryType))
                {
                    workersAvailable.Remove(worker);
                    commandSystem.EnqueueBuild(worker, refineryType, vespene);
                }
                else
                {

                }
            }
        }
        if (workersAvailable.Count > 0 && refinery.Count > 0 && TryGetAvailableWorker(out worker))
        {
            if (worker.TryGetOrder(out var order) && order.TargetUnitTag != 0)
            {
                if (analysisSystem.unitDictionary.TryGetValue(order.TargetUnitTag, out var target) && DData.Refinery.Contains(target.type) &&
                    (target.assignedHarvesters > 3 || target.buildProgress != 1))
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

        if (TryGetAvailableWorker(out worker))
        {
            if (worker.TryGetOrder(out var order) && order.TargetUnitTag != 0 &&
                analysisSystem.unitDictionary.TryGetValue(order.TargetUnitTag, out var target) &&
                DData.CommandCenters.Contains(target.type) &&
                target.assignedHarvesters > target.idealHarvesters * 1.5)
            {
                workersAvailable.Remove(worker);
                minerals1.Search(nearByMineral, worker.position, 40);
                if (nearByMineral.Count > 0)
                {
                    var unit = nearByMineral.GetRandom(random);
                    commandSystem.EnqueueAbility(worker, Abilities.SMART, unit);
                }

            }
        }

        analysisSystem._CollectUnits(idleUnits1, new object[] { Alliance.Self, idleUnitTypes });
        foreach (var unit in idleUnits1)
        {
            if (unit.orders.Count == 0)
            {
                commandSystem.EnqueueAbility(unit, BotData.onIdle[unit.type], unit.position + random.NextVector2(-5, 5));
            }
        }

        foreach (var barracks1 in factories)
        {
            UnitType labType;
            switch (barracks1.type)
            {
                case UnitType.TERRAN_BARRACKS:
                    labType = UnitType.TERRAN_BARRACKSTECHLAB;
                    break;
                case UnitType.TERRAN_FACTORY:
                    labType = UnitType.TERRAN_FACTORYTECHLAB;
                    break;
                case UnitType.TERRAN_STARPORT:
                    labType = UnitType.TERRAN_STARPORTTECHLAB;
                    break;
                default:
                    continue;
            }
            if (barracks1.addOnTag == 0 && barracks1.orders.Count == 0 && barracks1.buildProgress == 1 && ReadyToBuild(labType))
            {
                Build(barracks1, labType, barracks1.position, 5);
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
        predicationSystem.predicatedUnitTypes.Increment(unitType);
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
        commandSystem.EnqueueAbility(unit, ability, position);
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

        predicationSystem.predicatedUnitTypes.Increment(unitType);
        commandSystem.EnqueueBuild(unit, unitType, position);
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
        predicationSystem.predicatedUnitTypes.Increment(unitType);
        commandSystem.EnqueueTrain(unit, unitType);
        return true;
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

    bool TryGetAvailableWorker(out Unit worker)
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
