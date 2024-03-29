﻿using System;

namespace MilkWangBase.Attributes;

public abstract class XFindAttribute : Attribute
{
    public string MemberName { get; }

    public object[] Objects { get; }

    public XFindAttribute(string memberName, params object[] objects)
    {
        MemberName = memberName;
        Objects = objects;
    }
}
