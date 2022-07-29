using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarDebuCat.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class FindAttribute : Attribute
    {
        public string MemberName { get; }

        public FindAttribute(string memberName)
        {
            MemberName = memberName;
        }
    }
}
