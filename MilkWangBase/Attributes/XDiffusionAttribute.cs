using System;

namespace MilkWangBase.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class XDiffusionAttribute : Attribute
{
    public string MemberName { get; }

    public XDiffusionAttribute(string memberName)
    {
        MemberName = memberName;
    }
}
