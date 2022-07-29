using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarDebuCat.Attributes
{
    public class XFindAttribute : Attribute
    {
        public string MemberName { get; }

        public object[] Objects { get; }

        public XFindAttribute(string memberName,params object[] objects)
        {
            MemberName = memberName;
            Objects = objects;
        }
    }
}
