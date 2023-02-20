using StarDebuCat.Data;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace MilkWangBase.Utility;

public static class RandomExt
{
    public static float NextFloat(this Random random)
    {
        return random.NextSingle();
    }
    public static float NextFloat(this Random random, float min, float max)
    {
        return random.NextSingle() * (max - min) + min;
    }
    public static Vector2 NextVector2(this Random random, float min, float max)
    {
        return new Vector2(random.NextSingle() * (max - min) + min, random.NextSingle() * (max - min) + min);
    }


    public static T GetRandom<T>(this List<T> list, Random random)
    {
        if (list.Count == 1)
            return list[0];
        return list[random.Next(0, list.Count)];
    }

    public static bool TryGetRandom<T>(this List<T> list, Random random, out T result)
    {
        if (list.Count == 0)
        {
            result = default(T);
            return false;
        }
        if (list.Count == 1)
        {
            result = list[0];
            return true;
        }
        result = list[random.Next(0, list.Count)];
        return true;
    }

    public static Vector2 Nearest(this List<Vector2> list, Vector2 position)
    {
        Vector2 result = list[0];
        float distanceSquared = Vector2.DistanceSquared(position, result);
        foreach (var point in list)
        {
            float newDistance = Vector2.DistanceSquared(position, point);
            if (newDistance < distanceSquared)
            {
                result = point;
                distanceSquared = newDistance;
            }
        }
        return result;
    }

    public static Unit Nearest(this List<Unit> list, Vector2 position)
    {
        Unit result = list[0];
        float distanceSquared = Vector2.DistanceSquared(position, result.position);
        foreach (var unit in list)
        {
            float newDistance = Vector2.DistanceSquared(position, unit.position);
            if (newDistance < distanceSquared)
            {
                result = unit;
                distanceSquared = newDistance;
            }
        }
        return result;
    }

    public static Unit MinLife(this List<Unit> list)
    {
        Unit result = list[0];
        float minLife = result.health < 0.01f ? 500 : (result.health + result.shield);
        foreach (var unit in list)
        {
            float unitLife = unit.health + unit.shield;
            if (unitLife < minLife && unitLife > 0.01f)
            {
                result = unit;
                minLife = unitLife;
            }
        }
        return result;
    }
}
