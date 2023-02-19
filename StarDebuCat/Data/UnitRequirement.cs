using System.Collections.Generic;

namespace StarDebuCat.Data;

public class UnitRequirement
{
    public UnitType UnitType;

    public UnitType Builder;

    public HashSet<UnitType> Requirements;

    public bool needLab;
}
