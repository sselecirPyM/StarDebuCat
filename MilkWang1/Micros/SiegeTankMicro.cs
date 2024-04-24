using StarDebuCat.Data;
using System.Composition;

namespace MilkWang1.Micros;

[Export(typeof(IMicro))]
[ExportMetadata("unit", UnitType.TERRAN_SIEGETANK)]
[ExportMetadata("unit2", UnitType.TERRAN_SIEGETANKSIEGED)]
public class SiegeTankMicro : IMicro
{
    [Import]
    public BattleSystem1 battleSystem { get; set; }

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

    public void Update()
    {

    }

    bool CastAbil(BattleUnit battleUnit, Abilities ability, float range, bool noEnemy)
    {
        Unit unit = battleUnit.unit;
        bool cast = false;

        bool hasEnemy = battleUnit.nearestEnemy != null;
        bool enemyInRange = hasEnemy && battleUnit.nearestDistance < range;

        cast |= !noEnemy && enemyInRange;
        cast |= noEnemy && !enemyInRange && !battleSystem.enemyGround.HitTest(unit.position, range);

        if (cast)
        {
            unit.Command(ability);
            battleUnit.commanding = true;
        }

        return cast;
    }
}
