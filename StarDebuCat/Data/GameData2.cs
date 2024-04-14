using System;
using System.Collections.Generic;

namespace StarDebuCat.Data;

public partial class GameData
{
    public List<SC2APIProtocol.UnitTypeData> unitTypeDatas = new();
    public List<SC2APIProtocol.AbilityData> abilitiesData = new();
    public List<SC2APIProtocol.UpgradeData> upgradeDatas = new();
    public List<SC2APIProtocol.BuffData> buffDatas = new();
    public List<SC2APIProtocol.EffectData> effectDatas = new();
    public Dictionary<Abilities, SC2APIProtocol.UnitTypeData> abilToUnitTypeData;
    public Dictionary<Abilities, SC2APIProtocol.UpgradeData> abilToUpgrade;
    private List<float> fireRanges = new();

    public Dictionary<UnitType, List<UnitType>> Spawners = new();
    public Dictionary<UnitType, List<UpgradeType>> UpgradesResearcher = new();

    public bool initialized = false;

    public void Initialize(SC2APIProtocol.ResponseData responseData)
    {
        if (initialized)
            return;
        initialized = true;

        unitTypeDatas.AddRange(responseData.Units);

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



        abilitiesData = new(responseData.Abilities);
        abilToUnitTypeData = new();
        foreach (var unitTypeData in unitTypeDatas)
        {
            if (unitTypeData.AbilityId != 0)
                abilToUnitTypeData[(Abilities)unitTypeData.AbilityId] = unitTypeData;
        }
        upgradeDatas = new(responseData.Upgrades);
        abilToUpgrade = new();
        foreach (var upgrade in upgradeDatas)
        {
            if (upgrade.AbilityId != 0)
                abilToUpgrade[(Abilities)upgrade.AbilityId] = upgrade;
        }
        buffDatas = new(responseData.Buffs);
        effectDatas = new(responseData.Effects);

        Spawners = new();
        foreach (var requirement in DData.UnitRequirements)
        {
            var builder = requirement.Builder;
            var unitType = requirement.UnitType;
            if (building.Contains(unitType))
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
        foreach (var requirement in upgradeRequirements)
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
    }
    public SC2APIProtocol.UnitTypeData GetUnitTypeData(Unit unit)
    {
        return unitTypeDatas[(int)unit.type];
    }

    public SC2APIProtocol.UnitTypeData GetUnitTypeData(UnitType unitType)
    {
        return unitTypeDatas[(int)unitType];
    }

    public float GetFireRange(UnitType unitType)
    {
        return fireRanges[(int)unitType];
    }
}
