using MilkWangBase.Utility;
using StarDebuCat.Algorithm;
using MilkWangBase.Attributes;
using StarDebuCat.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Reflection;

namespace MilkWangBase;

public class AnalysisSystem
{
    InputSystem inputSystem;
    [Find("ReadyToPlay")]
    bool readyToPlay;


    public (int, int) MapSize;
    public Rectangle PlayableArea;
    public List<Vector2> StartLocations = new();

    public int playerId;
    public Race race;
    public Race enemyRace;

    public List<float> fireRanges;
    public List<SC2APIProtocol.UnitTypeData> unitTypeDatas;
    public List<SC2APIProtocol.AbilityData> abilitiesData;
    public List<SC2APIProtocol.UpgradeData> upgradeDatas;
    public List<SC2APIProtocol.BuffData> buffDatas;
    public Dictionary<Abilities, SC2APIProtocol.UnitTypeData> abilToUnitTypeData;
    public Dictionary<Abilities, SC2APIProtocol.UpgradeData> abilToUpgrade;

    public Dictionary<UnitType, List<UnitType>> Spawners = new();
    public Dictionary<UnitType, List<UpgradeType>> UpgradesResearcher = new();

    public HashSet<UpgradeType> hasUpgrade = new();

    public FrameResource currentFrameResource;

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
    public List<Effect> effects = new();
    public List<Vector2> buildPoints = new();

    public Image visable;
    public Image pathing;
    public Image build;
    public Image patio;
    public Image terrainHeight;

    public List<Vector2> patioPoints = new();
    public List<Vector2> patioPointsMerged = new();

    public bool Debugging;

    void Update()
    {
        if (!readyToPlay)
            return;
        Debugging = !inputSystem.ladderGame;
        CollectData();
        CollectScores();
        CollectUnits();

    }
    #region Initial Collect
    void CollectData()
    {
        playerId = inputSystem.playerId;
        if (unitTypeDatas != null)
        {
            return;
        }
        unitTypeDatas = new();
        unitTypeDatas.AddRange(inputSystem.gameData.Units);
        fireRanges = new(unitTypeDatas.Count);
        foreach (var unitData in unitTypeDatas)
        {
            if (unitData.Weapons.Count > 0)
            {
                float maxDistance = 0;
                foreach (var weapon in unitData.Weapons)
                {
                    maxDistance = Math.Max(maxDistance, weapon.Range);
                }
                fireRanges.Add(maxDistance);
            }
            else
            {
                fireRanges.Add(0);
            }
        }

        abilitiesData = new();
        abilitiesData.AddRange(inputSystem.gameData.Abilities);
        abilToUnitTypeData = new();
        foreach (var unitTypeData in unitTypeDatas)
        {
            if (unitTypeData.AbilityId != 0)
                abilToUnitTypeData[(Abilities)unitTypeData.AbilityId] = unitTypeData;
        }
        upgradeDatas = new();
        upgradeDatas.AddRange(inputSystem.gameData.Upgrades);
        abilToUpgrade = new();
        foreach (var upgrade in upgradeDatas)
        {
            if (upgrade.AbilityId != 0)
                abilToUpgrade[(Abilities)upgrade.AbilityId] = upgrade;
            if (((Abilities)upgrade.AbilityId) == Abilities.RESEARCH_CONCUSSIVESHELLS)
            {

            }
        }
        buffDatas = new();
        buffDatas.AddRange(inputSystem.gameData.Buffs);


        Spawners = new();
        foreach (var requirement in DData.UnitRequirements)
        {
            var builder = requirement.Builder;
            var unitType = requirement.UnitType;
            if (DData.Building.Contains(unitType))
            {
                continue;
            }
            if (!Spawners.TryGetValue(builder, out var buildUnits1))
            {
                buildUnits1 = new();
                Spawners[builder] = buildUnits1;
            }
            if (!buildUnits1.Contains(unitType))
                buildUnits1.Add(unitType);
        }
        UpgradesResearcher = new();
        foreach (var requirement in DData.UpgradeRequirements)
        {
            var researcher = requirement.Researcher;
            var upgrade = requirement.Upgrade;
            if (!UpgradesResearcher.TryGetValue(researcher, out var upgradeRequirements))
            {
                upgradeRequirements = new();
                UpgradesResearcher[researcher] = upgradeRequirements;
            }
            if (!upgradeRequirements.Contains(upgrade))
                upgradeRequirements.Add(upgrade);
        }

        var startRaw = inputSystem.gameInfo.StartRaw;
        var size = startRaw.PathingGrid.Size;
        MapSize = (size.X, size.Y);
        PlayableArea = RectExt.GetRectangle(startRaw.PlayableArea);

        StartLocations.Clear();
        foreach (var startLocation in startRaw.StartLocations)
        {
            StartLocations.Add(startLocation);
        }
        buildPoints.Clear();


        pathing = new(startRaw.PathingGrid);
        build = new(startRaw.PlacementGrid);
        terrainHeight = new(startRaw.TerrainHeight);

        #region patio
        patio = new(startRaw.PathingGrid);
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
        #endregion patio

        race = (Race)inputSystem.Race;
    }
    #endregion
    void CollectScores()
    {
        var observation = inputSystem.observation.Observation;
        var scoreDetails = observation.Score.ScoreDetails;

        var playerCommon = observation.PlayerCommon;
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
        deadUnits1.Clear();
        if (rawData.Event != null)
            foreach (var unit in rawData.Event.DeadUnits)
                deadUnits1.Add(unit);
        deadUnits.Clear();
        foreach (var deadUnit in deadUnits1)
            if (exitSightUnit.TryGetValue(deadUnit, out var unit))
                deadUnits.Add(unit);


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
                var effect1 = new Effect();
                effect1.Update(effect.EffectId, point);
                effects.Add(effect1);
            }
        }

        visable = new(rawData.MapState.Visibility);

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
                else if ((unit.owner == neutralResourceId || unit.owner == playerId)
                    && alliance == Alliance.Enemy)
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
                    case "MineralField" when DData.MineralFields.Contains(unit.type):
                    case "VespeneGeyser" when DData.VespeneGeysers.Contains(unit.type):
                    case "Army" when DData.Army.Contains(unit.type):
                    case "CommandCenter" when DData.CommandCenters.Contains(unit.type):
                    case "Building" when DData.Building.Contains(unit.type):
                    case "OutOfSight" when visable.Query(unit.position) != 2:
                    case "BuildComplete" when unit.buildProgress == 1:
                    case "Worker" when DData.Workers.Contains(unit.type):
                    case "Refinery" when DData.Refinery.Contains(unit.type):
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
        UnitType.TERRAN_FACTORY,
        UnitType.TERRAN_STARPORT,
        UnitType.PROTOSS_GATEWAY,
        UnitType.PROTOSS_WARPGATE,
        UnitType.PROTOSS_ROBOTICSFACILITY,
        UnitType.PROTOSS_STARGATE,
        UnitType.ZERG_LARVA,
    };

    public SC2APIProtocol.UnitTypeData GetUnitTypeData(Unit unit)
    {
        return unitTypeDatas[(int)unit.type];
    }

    public SC2APIProtocol.UnitTypeData GetUnitTypeData(UnitType unitType)
    {
        return unitTypeDatas[(int)unitType];
    }
}
