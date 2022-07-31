using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarDebuCat.Data
{
    public class UpgradeRequirement
    {
        public UnitType Researcher;
        public UpgradeType Upgrade;
        public HashSet<UnitType> Requirements;
    }
}
