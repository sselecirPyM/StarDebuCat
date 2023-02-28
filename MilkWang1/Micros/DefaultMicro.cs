using MilkWangBase.Utility;
using StarDebuCat.Data;
using System;

namespace MilkWang1.Micros;

public class DefaultMicro : IMicro
{
    public CommandSystem1 commandSystem;
    public AnalysisSystem1 analysisSystem;
    public BattleSystem1 battleSystem;

    public DefaultMicro(CommandSystem1 commandSystem, AnalysisSystem1 analysisSystem, BattleSystem1 battleSystem)
    {
        this.commandSystem = commandSystem;
        this.analysisSystem = analysisSystem;
        this.battleSystem = battleSystem;
    }

    public void Micro(BattleUnit battleUnit)
    {
        if (battleUnit.command)
            return;

        if (!CastAbil(battleUnit))
            MicroAttack(battleUnit);
    }

    public void NewFrame()
    {

    }


    bool CastAbil(BattleUnit battleUnit)
    {
        Unit unit = battleUnit.unit;
        bool cast = false;
        if (battleSystem.GameData.autoCast.TryGetValue(unit.type, out var autoCast) &&
            unit.energy >= autoCast.energyRequired)
        {
            bool hasEnemy = battleUnit.nearestEnemy != null;
            bool enemyInRange = hasEnemy && battleUnit.nearestDistance < autoCast.range;

            cast |= !autoCast.noEnemy && enemyInRange;
            cast |= autoCast.noEnemy && !enemyInRange && !battleSystem.enemyUnits1.HitTest(unit.position, autoCast.range);
            var targetType = analysisSystem.abilitiesData[(int)autoCast.ability].target;

            if (cast && targetType == SC2APIProtocol.AbilityData.Target.None)
            {
                commandSystem.OptimiseCommand(unit, autoCast.ability);
                battleUnit.command = true;
            }
            if (cast && targetType == SC2APIProtocol.AbilityData.Target.Point && battleUnit.nearestEnemy != null)
            {
                commandSystem.OptimiseCommand(unit, autoCast.ability, battleUnit.nearestEnemy.position);
                battleUnit.command = true;
            }
        }

        return cast;
    }

    void MicroAttack(BattleUnit battleUnit)
    {
        Unit unit = battleUnit.unit;
        var unitPosition = unit.position;
        float fireRange = analysisSystem.fireRanges[(int)unit.type];

        var enemy = battleUnit.nearestEnemy;
        var dummyEnemyMaxRange = battleUnit.dummyEnemyMaxRange;
        if (enemy != null)
        {
            if (dummyEnemyMaxRange < fireRange && battleUnit.nearestDistance > fireRange)
            {
                battleUnit.microStrategy = MicroStrategy.Push;
            }

            if (unit.weaponCooldown > 0.1f * 22.4f && fireRange > 2)
            {
                switch (battleUnit.microStrategy)
                {
                    case MicroStrategy.Forward:
                        if (dummyEnemyMaxRange + 0.5f >= fireRange)
                            commandSystem.OptimiseCommand(unit, Abilities.ATTACK, enemy.position);
                        else
                            commandSystem.OptimiseCommand(unit, Abilities.MOVE, unitPosition.Closer(enemy.position, -0.3f, Math.Min(fireRange, dummyEnemyMaxRange + 1.5f)));
                        break;
                    case MicroStrategy.Push:
                        commandSystem.OptimiseCommand(unit, Abilities.MOVE, unitPosition.Closer(enemy.position, 0.2f, Math.Min(2.0f, fireRange)));
                        break;
                    case MicroStrategy.None:
                        commandSystem.OptimiseCommand(unit, Abilities.MOVE, unitPosition.Closer(enemy.position, -0.3f, Math.Min(fireRange, dummyEnemyMaxRange + 1.5f)));
                        break;
                }
                battleUnit.command = true;
            }
            else if (unit.weaponCooldown <= 1 && battleUnit.minLifeEnemy != null)
            {
                commandSystem.OptimiseCommand(unit, Abilities.ATTACK, battleUnit.minLifeEnemy);
                battleUnit.command = true;
            }
            else if (battleUnit.microStrategy == MicroStrategy.Forward || battleUnit.microStrategy == MicroStrategy.Push)
            {
                commandSystem.OptimiseCommand(unit, Abilities.ATTACK, enemy.position);
                battleUnit.command = true;
            }
            else
            {
                commandSystem.OptimiseCommand(unit, Abilities.MOVE, unitPosition.Closer(enemy.position, -0.3f, Math.Max(fireRange, dummyEnemyMaxRange + 1.0f)));
                battleUnit.command = true;
            }
        }
    }
}
