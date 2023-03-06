using StarDebuCat.Data;

namespace MilkWang1.Micros;

public class SiegeTankMicro : IMicro
{
    public BattleSystem1 battleSystem;
    public CommandSystem1 commandSystem;

    public SiegeTankMicro(BattleSystem1 battleSystem, CommandSystem1 commandSystem)
    {
        this.battleSystem = battleSystem;
        this.commandSystem = commandSystem;
    }

    public void Micro(BattleUnit battleUnit)
    {
        switch (battleUnit.unit.type)
        {
            case UnitType.TERRAN_SIEGETANK:
                CastAbil(battleUnit, Abilities.MORPH_SIEGEMODE, 12.5f, false);
                break;
            case UnitType.TERRAN_SIEGETANKSIEGED:
                CastAbil(battleUnit, Abilities.MORPH_UNSIEGE, 14, true);
                break;
        }
    }

    public void NewFrame()
    {

    }

    bool CastAbil(BattleUnit battleUnit, Abilities ability, float range, bool noEnemy)
    {
        Unit unit = battleUnit.unit;
        bool cast = false;

        bool hasEnemy = battleUnit.nearestEnemy != null;
        bool enemyInRange = hasEnemy && battleUnit.nearestDistance < range;

        cast |= !noEnemy && enemyInRange;
        cast |= noEnemy && !enemyInRange && !battleSystem.enemyUnits1.HitTest(unit.position, range);

        if (cast)
        {
            commandSystem.OptimiseCommand(unit, ability);
            battleUnit.command = true;
        }

        return cast;
    }
}
