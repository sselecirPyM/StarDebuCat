using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarDebuCat.Data
{
    public class UnitRequirement
    {
        public UnitType UnitType;

        public UnitType Builder;

        public HashSet<UnitType> Requirements;

        public bool needLab;
    }
}
