﻿using StarDebuCat.Attributes;
using StarDebuCat.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MilkWangBase.Utility;

namespace MilkWangBase
{
    public class PredicationSystem
    {
        public AnalysisSystem analysisSystem;

        [Find("ReadyToPlay")]
        bool readyToPlay;

        [XFind("CollectUnits", Alliance.Self)]
        public List<Unit> myUnits;

        public Dictionary<UnitType, int> buildCompletedUnitTypes = new();
        public Dictionary<UnitType, int> buildNotCompletedUnitTypes = new();
        public Dictionary<UnitType, int> predicatedUnitTypes = new();
        public Dictionary<UnitType, bool> unitCanBuild = new();

        public List<Unit> buildNotCompletedUnits = new();

        public HashSet<UnitType> canBuildUnits = new();

        public float foodPrediction20s;

        public void Update()
        {
            if (!readyToPlay)
                return;
            foodPrediction20s = 0;
            buildCompletedUnitTypes.Clear();
            buildNotCompletedUnitTypes.Clear();
            buildNotCompletedUnits.Clear();
            predicatedUnitTypes.Clear();
            foreach (var unit in myUnits)
            {
                UnitType unitType = GetAlias(unit.type);
                var unitTypeData = analysisSystem.unitTypeDatas[(int)unit.type];
                if (unit.buildProgress == 1.0f)
                {
                    buildCompletedUnitTypes.Increment(unitType);
                }
                else
                {
                    buildNotCompletedUnitTypes.Increment(unitType);
                    buildNotCompletedUnits.Add(unit);
                    if (unitTypeData.Race != SC2APIProtocol.Race.Terran)
                    {
                        foodPrediction20s += unitTypeData.FoodProvided;
                        predicatedUnitTypes.Increment(unitType);
                    }
                }
            }
            float mineralPredict = (analysisSystem.CollectedMinerals.SampleFuture(20) - analysisSystem.SpentMinerals) * 1.25f + 50;
            float vespinePredict = (analysisSystem.CollectedVespene.SampleFuture(20) - analysisSystem.SpentVespene) * 1.25f;

            foreach (var unit in buildNotCompletedUnits)
            {
                var typeData = analysisSystem.unitTypeDatas[(int)unit.type];
                float timeRemain = typeData.BuildTime * (1 - unit.buildProgress);
                if (timeRemain < 20 * 22.4f)
                {
                    switch (unit.type)
                    {
                        case UnitType.TERRAN_BARRACKS:
                            foodPrediction20s -= 1;
                            break;
                        case UnitType.TERRAN_STARPORT:
                        case UnitType.TERRAN_FACTORY:
                        case UnitType.PROTOSS_GATEWAY:
                            foodPrediction20s -= 2;
                            break;
                        case UnitType.PROTOSS_STARGATE:
                        case UnitType.PROTOSS_ROBOTICSBAY:
                            foodPrediction20s -= 3;
                            break;
                        case UnitType.ZERG_SPAWNINGPOOL:
                            foodPrediction20s -= 6;
                            break;
                    }
                }
            }

            canBuildUnits.Clear();
            foreach (var item in DData.UnitRequirements)
            {
                bool canBuild = true;
                //var requirement = item;
                if (GetBuildCompletedCount(item.Builder) == 0)
                    continue;
                if (item.Requirements != null)
                {
                    foreach (var req1 in item.Requirements)
                    {
                        if (GetBuildCompletedCount(req1) == 0)
                        {
                            canBuild = false;
                        }
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

            foreach (var builder in myUnits)
            {
                if (builder.buildProgress == 1 && builder.orders.Count > 0)
                {
                    var order = builder.orders[0];
                    if (analysisSystem.abilToUnitTypeData.TryGetValue((Abilities)order.AbilityId, out var buildUnit))
                    {
                        var predicatedUnitType = GetAlias((UnitType)buildUnit.UnitId);
                        predicatedUnitTypes.Increment(predicatedUnitType);
                        if (order.TargetCase == SC2APIProtocol.UnitOrder.TargetOneofCase.None &&
                            buildUnit.FoodRequired > 0)
                        {
                            float timeRemain = buildUnit.BuildTime * (1 - order.Progress);

                            if (timeRemain < 20 * 22.4f)
                                foodPrediction20s -= buildUnit.FoodRequired;
                            timeRemain += buildUnit.BuildTime;
                            if (timeRemain < 20 * 22.4f && mineralPredict > 0)
                            {
                                foodPrediction20s -= buildUnit.FoodRequired;
                                mineralPredict -= buildUnit.MineralCost;
                            }
                        }
                        if (buildUnit.FoodProvided > 0)
                        {
                            foodPrediction20s += buildUnit.FoodProvided;
                        }
                    }
                    else if ((Abilities)order.AbilityId == Abilities.MORPH_ORBITALCOMMAND)
                    {
                        predicatedUnitTypes.Increment(UnitType.TERRAN_ORBITALCOMMAND);
                    }
                }
            }
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
            var unitType1 = (UnitType)analysisSystem.unitTypeDatas[(int)unitType].UnitAlias;
            if (unitType1 != UnitType.INVALID)
                return unitType1;
            return unitType;
        }
    }
}