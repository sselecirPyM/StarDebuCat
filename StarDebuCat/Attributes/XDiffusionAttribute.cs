using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarDebuCat.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class XDiffusionAttribute : Attribute
    {
        public string MemberName { get; }

        public XDiffusionAttribute(string memberName)
        {
            MemberName = memberName;
        }
    }
}
