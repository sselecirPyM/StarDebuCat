using MilkWangBase.Utility;
using StarDebuCat.Data;
using System.Collections.Generic;
using System.Numerics;

namespace MilkWang1.Micros;

public class CycloneMicro : IMicro
{
    public CommandSystem1 commandSystem;
    public AnalysisSystem1 analysisSystem;
    public BattleSystem1 battleSystem;

    Dictionary<BattleUnit, ulong> lockonPair = new();

    Dictionary<BattleUnit, ulong> lock1 = new();
    Dictionary<BattleUnit, ulong> lock2 = new();

    public CycloneMicro(CommandSystem1 commandSystem, AnalysisSystem1 analysisSystem, BattleSystem1 battleSystem)
    {
        this.commandSystem = commandSystem;
        this.analysisSystem = analysisSystem;
        this.battleSystem = battleSystem;
    }

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
    public void NewFrame()
    {
        (lock1, lock2) = (lock2, lock1);
        lock1.Clear();
        foreach (var pair in lock2)
        {
            if (analysisSystem.unitDictionary.TryGetValue(pair.Value, out var target) && target.HasBuff(BuffType.LockOn))
            {
                lockonPair[pair.Key] = pair.Value;
                pair.Key.stateCode = 3;
            }
        }

        foreach (var pair in lockonPair)
        {
            if (analysisSystem.unitDictionary.TryGetValue(pair.Value, out var target) &&
                analysisSystem.unitDictionary.TryGetValue(pair.Key.unit.Tag, out var source) &&
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
        commandSystem.ToggleAutocastAbility(battleUnit.unit, Abilities.EFFECT_LOCKON);
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
            commandSystem.OptimiseCommand(battleUnit.unit, Abilities.MOVE, unitPosition.Closer(enemy.position, -0.3f, battleUnit.enemyMaxRange + 2.0f));
        }
        if (!lockonPair.ContainsKey(battleUnit))
        {
            battleUnit.stateCode = 1;
        }
        battleUnit.command = true;

    }

    void Lock(BattleUnit source, Unit target)
    {
        if (target != null)
            commandSystem.OptimiseCommand(source.unit, Abilities.EFFECT_LOCKON, target);
        source.command = true;
        lock1[source] = target.Tag;
    }
}
