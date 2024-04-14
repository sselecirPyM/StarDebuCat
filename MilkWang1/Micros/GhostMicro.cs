using StarDebuCat.Data;
using System.Composition;

namespace MilkWang1.Micros;

[Export(typeof(IMicro))]
[ExportMetadata("unit", UnitType.TERRAN_GHOST)]
public class GhostMicro : IMicro
{
    [Import]
    public BattleSystem1 battleSystem { get;set; }
    [Import]
    public CommandSystem1 commandSystem { get; set; }


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
                battleUnit.commanding = true;
            }
        }

        return cast;
    }
}
