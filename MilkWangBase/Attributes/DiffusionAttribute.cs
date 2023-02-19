using System;

namespace MilkWangBase.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class DiffusionAttribute : Attribute
{
    public string MemberName { get; }

    public DiffusionAttribute(string memberName)
    {
        MemberName = memberName;
    }
}
