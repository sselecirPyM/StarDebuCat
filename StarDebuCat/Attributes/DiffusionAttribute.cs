using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarDebuCat.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class DiffusionAttribute : Attribute
    {
        public string MemberName { get; }

        public DiffusionAttribute(string memberName)
        {
            MemberName = memberName;
        }
    }
}
