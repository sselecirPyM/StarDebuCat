using System.Collections.Generic;

namespace StarDebuCat.Data;

public class UpgradeRequirement
{
    public UnitType Researcher;
    public UpgradeType Upgrade;
    public HashSet<UnitType> Requirements;
}
