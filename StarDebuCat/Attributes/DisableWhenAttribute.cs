using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarDebuCat.Attributes
{
    public class DisableWhenAttribute : Attribute
    {
        public DisableWhenAttribute(string condition)
        {
            this.condition = condition;
        }
        public string condition { get; }
    }
}
