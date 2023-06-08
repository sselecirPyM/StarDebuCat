using StarDebuCat.Data;
using System.Numerics;

namespace MilkWang1;

public enum UnitBattleType
{
    Undefined = 0,
    AttackMain,
    ProtectArea,
}
public enum MicroStrategy
{
    None,
    Push,
    Forward,
    Kite
}
public class BattleUnit
{
    public float inEnemyRangeFood;
    public float enemyInRangeFood;
    public float friendlyNearByFood;
    public float dummyEnemyMaxRange;
    public float enemyMaxRange;

    public int enemyAnyInRange;
    public Unit nearestEnemy;
    public Unit minLifeEnemy;
    public float nearestDistance;

    public MicroStrategy microStrategy;
    public int stateCode;

    public bool commanding;


    public Unit unit;

    public Vector2 protectPosition;
    public UnitBattleType battleType;

    public BattleUnit (Unit unit)
    {
        this.unit = unit;
    }
}
