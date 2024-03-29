﻿using System;
using System.Numerics;

namespace MilkWangBase.Utility;

public static class VectorExt
{
    public static Vector2 Closer(this Vector2 source, Vector2 target, float distance, float min)
    {
        var unit2Enemy = (source - target).Length();
        var unit2Enemy2 = Math.Max(unit2Enemy - distance, min) / unit2Enemy;
        var targetPosition = target + (source - target) * unit2Enemy2;

        return targetPosition;
    }
}
