using StarDebuCat.Data;
using System.Numerics;

namespace MilkWang1;

public enum UnitBattleType
{
    Undefined = 0,
    AttackMain,
    ProtectArea,
}
public class BattleUnit
{
    public Unit unit;

    public Vector2 protectPosition;
    public UnitBattleType battleType;

    public BattleUnit (Unit unit)
    {
        this.unit = unit;
    }
}
