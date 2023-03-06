using StarDebuCat.Data;
using System.Collections.Generic;
using System.Numerics;

namespace MilkWang1;

public class PredicationUnit
{
    public Unit unit;

    public Vector2 predictPosition;
}

public class PredicationFrame
{
    public List<PredicationUnit> predicationUnits;

}
