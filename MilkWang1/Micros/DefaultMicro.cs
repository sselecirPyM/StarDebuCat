using MilkWangBase.Utility;
using StarDebuCat.Data;
using System;
using System.Composition;
using System.Numerics;

namespace MilkWang1.Micros;

public class DefaultMicro : IMicro
{
    [Import]
    public BattleSystem1 battleSystem { get; set; }
    [Import]
    public GameData GameData { get; set; }


    public bool AllowPush = false;

    public void Micro(BattleUnit battleUnit)
    {
        if (battleUnit.commanding)
            return;

        if (!CastAbil(battleUnit))
            MicroAttack(battleUnit);
    }

    public void Update()
    {

    }


    bool CastAbil(BattleUnit battleUnit)
    {
        Unit unit = battleUnit.unit;
        bool cast = false;
        if (GameData.autoCast.TryGetValue(unit.type, out var autoCast) &&
            unit.energy >= autoCast.energyRequired)
        {
            bool hasEnemy = battleUnit.nearestEnemy != null;
            bool enemyInRange = hasEnemy && battleUnit.nearestDistance < autoCast.range;

            cast |= !autoCast.noEnemy && enemyInRange;
            cast |= autoCast.noEnemy && !enemyInRange && !battleSystem.enemyUnits1.HitTest(unit.position, autoCast.range);
            var targetType = GameData.abilitiesData[(int)autoCast.ability].target;

            if (cast && targetType == SC2APIProtocol.AbilityData.Target.None)
            {
                unit.Command(autoCast.ability);
                battleUnit.commanding = true;
            }
            if (cast && targetType == SC2APIProtocol.AbilityData.Target.Point && battleUnit.nearestEnemy != null)
            {
                unit.Command(autoCast.ability, battleUnit.nearestEnemy.position);
                battleUnit.commanding = true;
            }
        }

        return cast;
    }

    void MicroAttack(BattleUnit battleUnit)
    {
        if (AllowPush)
        {
            Micro1(battleUnit);
        }
        else
        {
            Micro2(battleUnit);
        }
    }

    void Micro1(BattleUnit battleUnit)
    {
        Unit unit = battleUnit.unit;
        var unitPosition = unit.position;
        float fireRange = GameData.GetFireRange(unit.type);

        var enemy = battleUnit.nearestEnemy;
        var dummyEnemyMaxRange = battleUnit.dummyEnemyMaxRange;
        if (enemy != null)
        {
            bool longFirePreparing = unit.weaponCooldown > 2 && fireRange > 2;
            if (dummyEnemyMaxRange < fireRange && battleUnit.nearestDistance > fireRange)
            {
                battleUnit.microStrategy = MicroStrategy.Push;
            }

            if (longFirePreparing)
            {
                switch (battleUnit.microStrategy)
                {
                    case MicroStrategy.Forward:
                        if (dummyEnemyMaxRange + 0.5f >= fireRange)
                            unit.Command(Abilities.ATTACK, enemy.position);
                        else
                            unit.Command(Abilities.MOVE, unitPosition.Closer(enemy.position, -0.3f, Math.Min(fireRange, dummyEnemyMaxRange + 1.5f)));
                        break;
                    case MicroStrategy.Push:
                        unit.Command(Abilities.MOVE, unitPosition.Closer(enemy.position, 0.2f, Math.Min(fireRange, 2.0f)));
                        break;
                    case MicroStrategy.None:
                        unit.Command(Abilities.MOVE, unitPosition.Closer(enemy.position, -0.3f, Math.Min(fireRange, dummyEnemyMaxRange + 1.5f)));
                        break;
                }
                battleUnit.commanding = true;
            }
            else if (unit.weaponCooldown <= 1 && battleUnit.minLifeEnemy != null)
            {
                unit.Command(Abilities.ATTACK, battleUnit.minLifeEnemy);
                battleUnit.commanding = true;
            }
            //else if (unit.positionZ > enemy.positionZ + 0.5f && Vector2.Distance(unitPosition, enemy.position) < 9)
            //{
            //    unit.Command(Abilities.STOP);
            //    battleUnit.commanding = true;
            //}
            else if (battleUnit.microStrategy == MicroStrategy.Forward || battleUnit.microStrategy == MicroStrategy.Push)
            {
                unit.Command(Abilities.ATTACK, enemy.position);
                battleUnit.commanding = true;
            }
            else
            {
                unit.Command(Abilities.MOVE, unitPosition.Closer(enemy.position, -0.3f, Math.Max(fireRange, dummyEnemyMaxRange + 1.0f)));
                battleUnit.commanding = true;
            }
        }
    }

    void Micro2(BattleUnit battleUnit)
    {
        Unit unit = battleUnit.unit;
        var unitPosition = unit.position;
        float fireRange = GameData.GetFireRange(unit.type);

        var enemy = battleUnit.nearestEnemy;
        var dummyEnemyMaxRange = battleUnit.dummyEnemyMaxRange;
        if (enemy != null)
        {
            bool longFirePreparing = unit.weaponCooldown > 2 && fireRange > 2;

            if (longFirePreparing)
            {
                unit.Command(Abilities.MOVE, unitPosition.Closer(enemy.position, -0.3f, Math.Min(fireRange, dummyEnemyMaxRange + 1.0f)));

                battleUnit.commanding = true;
            }
            else if (unit.weaponCooldown <= 1 && battleUnit.minLifeEnemy != null)
            {
                unit.Command(Abilities.ATTACK, battleUnit.minLifeEnemy);
                battleUnit.commanding = true;
            }
            else if (unit.positionZ <= enemy.positionZ + 0.5f)
            {
                unit.Command(Abilities.ATTACK_ATTACK, enemy.position);
                battleUnit.commanding = true;
            }
            else if (unit.positionZ > enemy.positionZ + 0.5f && Vector2.Distance(unitPosition, enemy.position) < 12)
            {
                unit.Command(Abilities.STOP);
                battleUnit.commanding = true;
            }
            else
            {
                unit.Command(Abilities.MOVE, unitPosition.Closer(enemy.position, -0.1f, Math.Max(fireRange, dummyEnemyMaxRange + 1.0f)));
                battleUnit.commanding = true;
            }
        }
    }
}
