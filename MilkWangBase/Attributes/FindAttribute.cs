using System;

namespace MilkWangBase.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class FindAttribute : Attribute
{
    public string MemberName { get; }

    public FindAttribute(string memberName)
    {
        MemberName = memberName;
    }
}
