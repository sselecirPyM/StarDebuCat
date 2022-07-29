using StarDebuCat.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MilkWangBase.Utility
{
    public static class RandomExt
    {
        public static float NextFloat(this Random random)
        {
            return (float)random.NextDouble();
        }
        public static float NextFloat(this Random random, float min, float max)
        {
            return (float)random.NextDouble() * (max - min) + min;
        }
        public static T GetRandom<T>(this List<T> list, Random random)
        {
            if (list.Count == 1)
                return list[0];
            return list[random.Next(0, list.Count)];
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
}
