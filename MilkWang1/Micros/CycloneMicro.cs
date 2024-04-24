using MilkWangBase.Utility;
using StarDebuCat.Data;
using System.Collections.Generic;
using System.Composition;
using System.Numerics;

namespace MilkWang1.Micros;

//[Export(typeof(IMicro))]
//[ExportMetadata("unit", UnitType.TERRAN_CYCLONE)]
public class CycloneMicro : IMicro
{
    [Import]
    public AnalysisSystem1 analysisSystem { get; set; }
    [Import]
    public BattleSystem1 battleSystem { get; set; }

    Dictionary<BattleUnit, ulong> lockonPair = new();

    Dictionary<BattleUnit, ulong> lock1 = new();
    Dictionary<BattleUnit, ulong> lock2 = new();


    public void Micro(BattleUnit battleUnit)
    {
        switch (battleUnit.stateCode)
        {
            case 0:
                Micro0(battleUnit);
                break;
            case 1:
                Micro1(battleUnit);
                break;
            case 2:
                Micro2(battleUnit);
                break;
            case 3:
                Micro3(battleUnit);
                break;
        }
    }

    List<BattleUnit> removeUnits = new();
    public void Update()
    {
        (lock1, lock2) = (lock2, lock1);
        lock1.Clear();
        var unitMap = analysisSystem.unitDictionary;
        foreach (var pair in lock2)
        {
            if (unitMap.TryGetValue(pair.Value, out var target) && target.HasBuff(BuffType.LockOn))
            {
                lockonPair[pair.Key] = pair.Value;
                pair.Key.stateCode = 3;
            }
        }

        foreach (var pair in lockonPair)
        {
            if (unitMap.TryGetValue(pair.Value, out var target) &&
                unitMap.TryGetValue(pair.Key.unit.Tag, out var source) &&
                target.HasBuff(BuffType.LockOn))
            {

            }
            else
            {
                removeUnits.Add(pair.Key);
            }
        }
        foreach (var removeUnit in removeUnits)
        {
            lockonPair.Remove(removeUnit);
        }
        removeUnits.Clear();
    }

    void Micro0(BattleUnit battleUnit)
    {
        battleUnit.unit.ToggleAutoCast(Abilities.EFFECT_LOCKON);
        battleUnit.stateCode = 1;
    }
    void Micro1(BattleUnit battleUnit)
    {
        if (battleUnit.nearestEnemy != null)
        {
            var enemy = battleUnit.nearestEnemy;
            if (Vector2.Distance(enemy.position, battleUnit.unit.position) < 7)
            {
                Lock(battleUnit, battleUnit.nearestEnemy);
                battleUnit.stateCode = 2;
            }
        }
    }

    void Micro2(BattleUnit battleUnit)
    {
        if (battleUnit.unit.TryGetOrder(out var order))
        {
            Abilities abil = (Abilities)order.AbilityId;
            if (abil == Abilities.EFFECT_LOCKON)
            {
                Lock(battleUnit, battleUnit.nearestEnemy);
            }
            else
            {
                battleUnit.stateCode = 1;
            }
        }
        else
        {
            battleUnit.stateCode = 1;
        }
    }

    void Micro3(BattleUnit battleUnit)
    {
        var unitPosition = battleUnit.unit.position;
        var enemy = battleUnit.nearestEnemy;
        if (enemy != null)
        {
            battleUnit.unit.Command(Abilities.MOVE, unitPosition.Closer(enemy.position, -0.3f, battleUnit.enemyMaxRange + 2.0f));
        }
        if (!lockonPair.ContainsKey(battleUnit))
        {
            battleUnit.stateCode = 1;
        }
        battleUnit.commanding = true;

    }

    void Lock(BattleUnit source, Unit target)
    {
        if (target != null)
            source.unit.Command(Abilities.EFFECT_LOCKON, target);
        source.commanding = true;
        lock1[source] = target.Tag;
    }
}
