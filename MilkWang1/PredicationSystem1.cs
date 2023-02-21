using MilkWangBase;
using MilkWangBase.Attributes;
using MilkWangBase.Utility;
using StarDebuCat.Data;
using System;
using System.Collections.Generic;

namespace MilkWang1;

public class PredicationSystem1
{
    public AnalysisSystem1 analysisSystem;

    [XFind("CollectUnits", Alliance.Self)]
    public List<Unit> myUnits;


    public Dictionary<UnitType, int> buildCompletedUnitTypes = new();
    public Dictionary<UnitType, int> buildNotCompletedUnitTypes = new();
    public Dictionary<UnitType, int> predicatedUnitTypes = new();

    public List<Unit> buildNotCompletedUnits = new();

    public HashSet<UnitType> canBuildUnits = new();
    public HashSet<UpgradeType> canUpgrades = new();
    public HashSet<UpgradeType> predicatedUpgrades = new();

    public List<(UnitType, float)> vBuildNotCompleted = new();

    public float foodPrediction20s;

    SC2Resource predictResource = new();

    public GameData GameData;

    Dictionary<UnitType, int> buildCompleteFood = new();

    bool initialized = false;

    public void Update()
    {
        if (!initialized)
            Initialize1();

        foodPrediction20s = 0;
        buildCompletedUnitTypes.Clear();
        buildNotCompletedUnitTypes.Clear();
        buildNotCompletedUnits.Clear();
        predicatedUnitTypes.Clear();
        vBuildNotCompleted.Clear();
        foreach (var unit in myUnits)
        {
            UnitType unitType = GetAlias(unit.type);
            var unitTypeData = analysisSystem.GetUnitTypeData(unit.type);
            float timeRemain = unitTypeData.BuildTime * (1 - unit.buildProgress);
            if (unit.buildProgress == 1.0f)
            {
                buildCompletedUnitTypes.Increment(unitType);
            }
            else
            {
                buildNotCompletedUnitTypes.Increment(unitType);
                buildNotCompletedUnits.Add(unit);
            }
        }
        var frameResource = analysisSystem.currentFrameResource;
        var history = analysisSystem.historyFrameResource;
        var predictFrame = FrameResource.Interpolate(history[Math.Max(history.Count - 3, 0)], history[^1], frameResource.GameLoop + 448);

        predictResource.mineral = (int)((predictFrame.CollectedMinerals - frameResource.SpentMinerals) * 1.25f + 50);
        predictResource.vespene = (int)((predictFrame.CollectedVespene - frameResource.SpentVespene) * 1.25f);

        foreach (var unit in buildNotCompletedUnits)
        {
            var typeData = analysisSystem.GetUnitTypeData(unit.type);
            float timeRemain = typeData.BuildTime * (1 - unit.buildProgress);
            if (GameData.selfBuild.Contains(unit.type))
            {
                vBuildNotCompleted.Add((unit.type, timeRemain));
            }
        }

        foreach (var builder in myUnits)
        {
            if (builder.buildProgress < 1)
                continue;
            if (!builder.TryGetOrder(out var order))
                continue;
            Abilities abilities = (Abilities)order.AbilityId;
            if (analysisSystem.abilToUnitTypeData.TryGetValue(abilities, out var buildUnit))
            {
                var predicatedUnitType = GetAlias((UnitType)buildUnit.UnitId);
                predicatedUnitTypes.Increment(predicatedUnitType);
                float timeRemain = buildUnit.BuildTime * (1 - order.Progress);
                vBuildNotCompleted.Add((predicatedUnitType, timeRemain));
            }
            else if (GameData.morphToUnit.TryGetValue(abilities, out var unitType))
            {
                predicatedUnitTypes.Increment(unitType);
            }
            if (analysisSystem.abilToUpgrade.TryGetValue(abilities, out var upgrade))
            {
                var up1 = (UpgradeType)upgrade.UpgradeId;
                predicatedUpgrades.Add(up1);
                canUpgrades.Remove(up1);
            }
        }

        foreach (var pair in vBuildNotCompleted)
        {
            if (buildCompleteFood.TryGetValue(pair.Item1, out var foodCost))
            {
                if (pair.Item2 < 20 * 22.4f && foodCost < 0)
                    foodPrediction20s += foodCost;
                if (pair.Item2 < 30 * 22.4f && foodCost > 0)
                    foodPrediction20s += foodCost;
            }
        }

        canBuildUnits.Clear();
        foreach (var item in DData.UnitRequirements)
        {
            bool canBuild = true;
            if (GetBuildCompletedCount(item.Builder) == 0)
                continue;
            if (item.Requirements != null)
                foreach (var req1 in item.Requirements)
                {
                    if (GetBuildCompletedCount(req1) == 0)
                    {
                        canBuild = false;
                        break;
                    }
                }
            if (item.needLab)
            {
                switch (item.Builder)
                {
                    case UnitType.TERRAN_BARRACKS when GetBuildCompletedCount(UnitType.TERRAN_BARRACKSTECHLAB) == 0:
                    case UnitType.TERRAN_FACTORY when GetBuildCompletedCount(UnitType.TERRAN_FACTORYTECHLAB) == 0:
                    case UnitType.TERRAN_STARPORT when GetBuildCompletedCount(UnitType.TERRAN_STARPORTTECHLAB) == 0:
                        continue;
                }
            }
            if (canBuild)
            {
                canBuildUnits.Add(item.UnitType);
            }
        }
        foreach (var item in DData.UpgradeRequirements)
        {
            bool canResearching = true;
            if (GetBuildCompletedCount(item.Researcher) == 0)
                continue;
            if (item.Requirements != null)
                foreach (var req1 in item.Requirements)
                {
                    if (GetBuildCompletedCount(req1) == 0)
                    {
                        canResearching = false;
                    }
                }
            if (canResearching)
            {
                canUpgrades.Add(item.Upgrade);
            }
        }

        canUpgrades.ExceptWith(analysisSystem.hasUpgrade);
    }

    void Initialize1()
    {
        initialized = true;
        foreach (var pair in analysisSystem.abilToUnitTypeData)
        {
            int foodCost = (int)pair.Value.FoodRequired;
            int foodPrivede = (int)pair.Value.FoodProvided;
            if (foodCost > 0)
                buildCompleteFood[(UnitType)pair.Value.UnitId] = -foodCost;
            if (foodPrivede > 0)
                buildCompleteFood[(UnitType)pair.Value.UnitId] = foodPrivede;
        }
        foreach (var pair in GameData.buildCompleteFoodCost)
            buildCompleteFood[pair.Key] = -pair.Value;
    }

    public int GetBuildCompletedCount(UnitType unitType)
    {
        buildCompletedUnitTypes.TryGetValue(unitType, out var completed);
        return completed;
    }

    public int GetPredictTotal(UnitType unitType)
    {
        buildCompletedUnitTypes.TryGetValue(unitType, out var completed);
        predicatedUnitTypes.TryGetValue(unitType, out var predicated);
        return completed + predicated;
    }

    UnitType GetAlias(UnitType unitType)
    {
        var unitType1 = (UnitType)analysisSystem.GetUnitTypeData(unitType).UnitAlias;
        if (unitType1 != UnitType.INVALID)
            return unitType1;
        else
            return unitType;
    }
}
