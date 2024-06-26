﻿using MilkWangBase;
using MilkWangBase.Attributes;
using MilkWangBase.Utility;
using StarDebuCat.Algorithm;
using StarDebuCat.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Reflection;

namespace MilkWang1;

public class AnalysisSystem1
{
    InputSystem1 inputSystem;

    public (int, int) MapSize;
    public Rectangle PlayableArea;
    public List<Vector2> StartLocations = new();

    public int playerId;
    public Race race;
    public Race enemyRace;


    public HashSet<UpgradeType> hasUpgrade = new();

    public FrameResource currentFrameResource = new() { KillUnitCount = new(), LostUnitCount = new() };

    public List<FrameResource> historyFrameResource = new();
    public readonly int sampleRate = 32;
    public int currentSample = 0;

    public uint GameLoop;

    public List<Unit> units = new();
    public Dictionary<ulong, Unit> unitDictionary = new();
    public Dictionary<ulong, Unit> exitSightUnit = new();
    public Dictionary<ulong, Unit> enterSightUnit = new();
    public HashSet<Unit> deadUnits = new();
    public HashSet<ulong> deadUnits1 = new();
    List<(object[], List<Unit>)> unitFindCache = new();

    public List<PowerSource> powerSources = new();
    public List<SC2APIProtocol.Effect> rawEffect = new();
    public List<PointEffect> effects = new();
    public List<Vector2> buildPoints = new();

    public Image visable;
    public Image pathing;
    public Image build;
    public Image patio;
    public Image terrainHeight;

    public List<Vector2> patioPoints = new();
    public List<Vector2> patioPointsMerged = new();

    public GameData GameData;

    bool initialized = false;

    public void Update()
    {
        CollectData();
        CollectScores();
        CollectUnits();

    }
    #region Initial Collect
    void CollectData()
    {
        playerId = inputSystem.playerId;
        if (initialized)
            return;
        initialized = true;

        GameData.Initialize(inputSystem.gameData);

        AnalizeTerrain();
        race = (Race)inputSystem.Race;
    }

    void AnalizeTerrain()
    {
        var startRaw = inputSystem.gameInfo.StartRaw;
        var size = startRaw.PathingGrid.Size;
        MapSize = (size.X, size.Y);
        PlayableArea = RectExt.GetRectangle(startRaw.PlayableArea);

        StartLocations.Clear();
        foreach (var startLocation in startRaw.StartLocations)
        {
            StartLocations.Add(new Vector2(startLocation.X, startLocation.Y));
        }
        buildPoints.Clear();


        pathing = new(startRaw.PathingGrid);
        build = new(startRaw.PlacementGrid);
        terrainHeight = new(startRaw.TerrainHeight);

        Patio(pathing);
    }

    void Patio(Image pathingGrid)
    {
        patio = new(pathingGrid);
        for (int i = 0; i < pathing.Data.Length; i++)
            patio.Data[i] = (byte)(pathing.Data[i] & ~build.Data[i]);
        for (int i = 0; i < patio.Data.Length; i++)
            for (int j = 0; j < 8; j++)
                if ((patio.Data[i] & 1 << 7 - j) != 0)
                {
                    int d = i * 8 + j;
                    patioPoints.Add(new(d % patio.Width + 0.5f, d / patio.Width + 0.5f));
                }
        patio.Group(patioPointsMerged, 4);

        for (int i = 0; i < patioPointsMerged.Count; i++)
        {
            patioPointsMerged[i] = MovePositionToHigher(patioPointsMerged[i]);
            patioPointsMerged[i] = MovePositionToHigher(patioPointsMerged[i]);
        }

    }
    #endregion
    void CollectScores()
    {
        var observation = inputSystem.observation.Observation;
        var scoreDetails = observation.Score.ScoreDetails;

        var playerCommon = observation.PlayerCommon;
        var previousFrameResource = currentFrameResource;

        currentFrameResource = new FrameResource()
        {
            CollectionRateMinerals = scoreDetails.CollectionRateMinerals,
            CollectionRateVespene = scoreDetails.CollectionRateVespene,
            CollectedMinerals = scoreDetails.CollectedMinerals,
            CollectedVespene = scoreDetails.CollectedVespene,
            KilledMineralsArmy = scoreDetails.KilledMinerals.Army,
            KilledMineralsTechnology = scoreDetails.KilledMinerals.Technology,
            KilledVespeneArmy = scoreDetails.KilledVespene.Army,
            KilledVespeneTechnology = scoreDetails.KilledVespene.Technology,
            TotalUsedMineralsArmy = scoreDetails.TotalUsedMinerals.Army,
            TotalUsedMineralsTechnology = scoreDetails.TotalUsedMinerals.Technology,
            TotalUsedVespeneArmy = scoreDetails.TotalUsedVespene.Army,
            TotalUsedVespeneTechnology = scoreDetails.TotalUsedVespene.Technology,
            LostMineralsArmy = scoreDetails.LostMinerals.Army,
            LostVespeneArmy = scoreDetails.LostVespene.Army,
            SpentMinerals = scoreDetails.SpentMinerals,
            SpentVespene = scoreDetails.SpentVespene,
            UsedMineralsArmy = scoreDetails.UsedMinerals.Army,
            UsedVespeneArmy = scoreDetails.UsedVespene.Army,
            FoodUsedArmy = scoreDetails.FoodUsed.Army,


            Minerals = (int)playerCommon.Minerals,
            Vespene = (int)playerCommon.Vespene,
            ArmyCount = (int)playerCommon.ArmyCount,
            FoodArmy = (int)playerCommon.FoodArmy,
            FoodCap = (int)playerCommon.FoodCap,
            FoodWorkers = (int)playerCommon.FoodWorkers,
            FoodUsed = (int)playerCommon.FoodUsed,
            WarpGateCount = (int)playerCommon.WarpGateCount,
            IdleWorkerCount = (int)playerCommon.IdleWorkerCount,

            MineralKill = previousFrameResource.MineralKill,
            VespeneKill = previousFrameResource.VespeneKill,
            MineralLost = previousFrameResource.MineralLost,
            VespeneLost = previousFrameResource.VespeneLost,

            KillUnitCount = new(previousFrameResource.KillUnitCount),
            LostUnitCount = new(previousFrameResource.LostUnitCount),

            GameLoop = (int)observation.GameLoop,
        };
        if (currentSample == 0)
        {
            historyFrameResource.Add(currentFrameResource);
        }
        currentSample++;
        if (currentSample >= sampleRate)
        {
            currentSample = 0;
        }

        GameLoop = observation.GameLoop;

        hasUpgrade.Clear();
        if (observation.RawData.Player.UpgradeIds != null)
            foreach (var up in observation.RawData.Player.UpgradeIds)
            {
                hasUpgrade.Add((UpgradeType)up);
            }
    }

    HashSet<ulong> previousUnit = new();
    HashSet<ulong> existUnit = new();
    void CollectUnits()
    {
        (previousUnit, existUnit) = (existUnit, previousUnit);
        existUnit.Clear();
        enterSightUnit.Clear();
        unitFindCache.Clear();
        var rawData = inputSystem.observation.Observation.RawData;
        foreach (var unit in rawData.Units)
        {
            if (unitDictionary.TryGetValue(unit.Tag, out var unit1))
            {
                unit1.UpdateBy(unit);
            }
            else
            {
                unit1 = new Unit();
                unit1.UpdateBy(unit);
                unitDictionary.Add(unit.Tag, unit1);
                enterSightUnit.Add(unit.Tag, unit1);
            }
            existUnit.Add(unit.Tag);
        }
        exitSightUnit.Clear();
        previousUnit.ExceptWith(existUnit);
        foreach (var key in previousUnit)
        {
            unitDictionary.Remove(key, out var unit1);
            exitSightUnit.Add(key, unit1);
        }

        units.Clear();
        units.AddRange(unitDictionary.Values);
        CountingDeadUnits();


        powerSources.Clear();
        foreach (var powerSource in rawData.Player.PowerSources)
        {
            var powerSource1 = new PowerSource();
            powerSource1.Update(powerSource);
            powerSources.Add(powerSource1);
        }
        effects.Clear();
        foreach (var effect in rawData.Effects)
        {
            foreach (var point in effect.Pos)
            {
                var effect1 = new PointEffect();
                effect1.Update(effect.EffectId, new Vector2(point.X, point.Y));
                effects.Add(effect1);
            }
        }
        rawEffect.Clear();
        rawEffect.AddRange(rawData.Effects);

        visable = new(rawData.MapState.Visibility);

        if (enemyRace == Race.NoRace)
        {
            foreach (var unit in units)
            {
                if (unit.alliance == Alliance.Enemy)
                {
                    if (DData.Terran.Contains(unit.type))
                    {
                        enemyRace = Race.Terran;
                    }
                    if (DData.Protoss.Contains(unit.type))
                    {
                        enemyRace = Race.Protoss;
                    }
                    if (DData.Zerg.Contains(unit.type))
                    {
                        enemyRace = Race.Zerg;
                    }
                }
            }
        }
    }

    void CountingDeadUnits()
    {
        var rawData = inputSystem.observation.Observation.RawData;
        deadUnits1.Clear();
        if (rawData.Event != null)
            foreach (var unit in rawData.Event.DeadUnits)
                deadUnits1.Add(unit);
        deadUnits.Clear();
        foreach (var deadUnit in deadUnits1)
            if (exitSightUnit.TryGetValue(deadUnit, out var unit))
                deadUnits.Add(unit);
        foreach (var unit in deadUnits)
        {
            var unitTypeData = GameData.GetUnitTypeData(unit.type);
            if (unit.owner == playerId)
            {
                currentFrameResource.MineralLost += (int)unitTypeData.MineralCost;
                currentFrameResource.VespeneLost += (int)unitTypeData.VespeneCost;
                currentFrameResource.LostUnitCount.Increment(unit.type);
            }
            else if (unit.alliance == Alliance.Enemy)
            {
                currentFrameResource.MineralKill += (int)unitTypeData.MineralCost;
                currentFrameResource.VespeneKill += (int)unitTypeData.VespeneCost;
                currentFrameResource.KillUnitCount.Increment(unit.type);
            }
        }
    }

    #region CollectUnits
    [XDiffusion("CollectUnits")]
    void _CollectUnits(object owner, FieldInfo fieldInfo)
    {
        var attr = fieldInfo.GetCustomAttribute<XFindAttribute>();
        object[] conditions = attr.Objects;
        List<Unit> units1 = (List<Unit>)fieldInfo.GetValue(owner);
        if (units1 == null)
        {
            units1 = new List<Unit>();
            fieldInfo.SetValue(owner, units1);
        }
        _CollectUnits(units1, conditions);
    }
    public void _CollectUnits(List<Unit> units1, object[] conditions)
    {
        units1.Clear();
        var findUnits = Optimise(conditions, out var equal);
        if (equal)
            units1.AddRange(findUnits);
        else
        {
            foreach (var unit in findUnits)
            {
                if (UnitConditions(unit, conditions))
                    units1.Add(unit);
            }
            unitFindCache.Add((conditions, units1));
        }
    }
    List<Unit> Optimise(object[] conditions, out bool equal)
    {
        List<Unit> lessUnits = units;
        equal = false;
        foreach (var findUnit in unitFindCache)
        {
            if (conditions.Length < findUnit.Item1.Length)
                continue;
            bool passAll = true;
            for (int i = 0; i < findUnit.Item1.Length; i++)
                if (!findUnit.Item1[i].Equals(conditions[i]))
                {
                    passAll = false;
                    break;
                }
            if (passAll)
            {
                lessUnits = findUnit.Item2;
                if (findUnit.Item1.Length == conditions.Length)
                {
                    equal = true;
                    return lessUnits;
                }
            }
        }
        return lessUnits;
    }
    bool UnitConditions(Unit unit, object[] conditions)
    {
        const int neutralResourceId = 16;
        foreach (var condition in conditions)
        {
            if (condition is Alliance alliance)
            {
                if (unit.owner != playerId && alliance == Alliance.Self)
                {
                    return false;
                }
                else if (unit.owner != neutralResourceId && alliance == Alliance.Neutral)
                {
                    return false;
                }
                else if ((unit.owner == neutralResourceId || unit.owner == playerId) && alliance == Alliance.Enemy)
                {
                    return false;
                }
            }
            else if (condition is UnitType unitType)
            {
                if (unit.type != unitType)
                {
                    return false;
                }
            }
            else if (condition is UnitType[] unitTypes)
            {
                foreach (var unitType1 in unitTypes)
                    if (unitType1 == unit.type)
                        return true;
                return false;
            }
            else if (condition is HashSet<UnitType> unitTypes1)
            {
                return unitTypes1.Contains(unit.type);
            }
            else if (condition is string s)
            {
                switch (s)
                {
                    case "IsCloaked" when unit.isCloaked:
                    case "IsBurrowed" when unit.isBurrowed:
                    case "IsFlying" when unit.isFlying:
                    case "IsPowered" when unit.isPowered:
                    case "Idle" when unit.orders.Count == 0:
                    case "Enter" when enterSightUnit.ContainsKey(unit.Tag):
                    case "MineralField" when GameData.mineralFields.Contains(unit.type):
                    case "VespeneGeyser" when GameData.vespeneGeysers.Contains(unit.type):
                    case "Army" when GameData.armies.Contains(unit.type):
                    case "CommandCenter" when GameData.commandCenters.Contains(unit.type):
                    case "Building" when GameData.building.Contains(unit.type):
                    case "Worker" when GameData.workers.Contains(unit.type):
                    case "Refinery" when GameData.refineries.Contains(unit.type):
                    case "Ground" when !GameData.flying.Contains(unit.type):
                    case "Flying" when !GameData.flying.Contains(unit.type):
                    case "OutOfSight" when visable.Query(unit.position) != 2:
                    case "BuildComplete" when unit.buildProgress == 1:
                    case "Factory" when Factories.Contains(unit.type):
                        continue;
                    default:
                        return false;
                }
            }
        }
        return true;
    }
    #endregion

    Dictionary<FieldInfo, List<Unit>> buildTreeCache = new();
    [XDiffusion("QuadTree")]
    void _BuildQuadTree(object owner, FieldInfo fieldInfo)
    {
        var attr = fieldInfo.GetCustomAttribute<XFindAttribute>();
        object[] conditions = attr.Objects;
        QuadTree<Unit> units1 = (QuadTree<Unit>)fieldInfo.GetValue(owner);
        if (units1 == null)
        {
            units1 = new QuadTree<Unit>();
            fieldInfo.SetValue(owner, units1);
        }
        if (buildTreeCache.TryGetValue(fieldInfo, out var temp))
        {

        }
        else
        {
            temp = new();
            buildTreeCache.Add(fieldInfo, temp);
        }
        _CollectUnits(temp, conditions);
        units1.BuildQuadTree(temp);
    }

    static HashSet<UnitType> Factories = new()
    {
        UnitType.TERRAN_BARRACKS,
        UnitType.TERRAN_BARRACKSFLYING,
        UnitType.TERRAN_FACTORY,
        UnitType.TERRAN_FACTORYFLYING,
        UnitType.TERRAN_STARPORT,
        UnitType.TERRAN_STARPORTFLYING,
        UnitType.PROTOSS_GATEWAY,
        UnitType.PROTOSS_WARPGATE,
        UnitType.PROTOSS_ROBOTICSFACILITY,
        UnitType.PROTOSS_STARGATE,
        UnitType.ZERG_LARVA,
    };

    Vector2 MovePositionToHigher(Vector2 position)
    {
        byte heightBase = terrainHeight.Query(position);
        Vector2 adjustPosition = Vector2.Zero;
        for (int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                if (i == 0 && j == 0)
                    continue;
                byte height = terrainHeight.Query(position + new Vector2(i, j));
                if (heightBase > height)
                {
                    adjustPosition -= new Vector2(i, j);
                }
                else if (heightBase < height)
                {
                    adjustPosition += new Vector2(i, j);
                }
            }
        }
        position.X += Math.Sign(adjustPosition.X);
        position.Y += Math.Sign(adjustPosition.Y);
        return position;
    }
}
