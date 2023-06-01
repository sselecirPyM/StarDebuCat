using StarDebuCat.Data;

namespace MilkWang1.Micros;

public class GhostMicro : IMicro
{
    public BattleSystem1 battleSystem;
    public CommandSystem1 commandSystem;

    public GhostMicro(BattleSystem1 battleSystem, CommandSystem1 commandSystem)
    {
        this.battleSystem = battleSystem;
        this.commandSystem = commandSystem;
    }

    public void Micro(BattleUnit battleUnit)
    {
        CastAbil(battleUnit);
    }

    public void Update()
    {

    }

    bool CastAbil(BattleUnit battleUnit)
    {
        float range = 10.25f;
        Unit unit = battleUnit.unit;
        bool cast = false;
        if (unit.energy >= 75)
        {
            Unit nearestEnemy = battleUnit.nearestEnemy;
            bool hasEnemy = nearestEnemy != null;
            bool enemyInRange = hasEnemy && battleUnit.nearestDistance < range;

            cast |= enemyInRange && (nearestEnemy.energy >= 50 || nearestEnemy.shield >= 25);

            if (cast)
            {
                commandSystem.OptimiseCommand(unit, Abilities.EFFECT_EMP, battleUnit.nearestEnemy.position);
                battleUnit.command = true;
            }
        }

        return cast;
    }
}
