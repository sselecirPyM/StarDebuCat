using StarDebuCat.Algorithm;
using StarDebuCat.Data;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace MilkWangBase.Utility;

public static class TreeExt
{
    public static void BuildQuadTree(this QuadTree<ulong> quadTree, List<Unit> units)
    {
        if (units.Count == 0)
        {
            quadTree.Clear();
            return;
        }
        Span<(float, float, ulong)> units1 = stackalloc (float, float, ulong)[units.Count];
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            units1[i] = (unit.position.X, unit.position.Y, unit.Tag);
        }
        quadTree.Initialize(units1);
    }

    public static void BuildQuadTree(this QuadTree<int> quadTree, List<Vector2> units)
    {
        if (units.Count == 0)
        {
            quadTree.Clear();
            return;
        }
        Span<(float, float, int)> units1 = stackalloc (float, float, int)[units.Count];
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            units1[i] = (unit.X, unit.Y, i);
        }
        quadTree.Initialize(units1);
    }

    public static void BuildQuadTree(this QuadTree<Unit> quadTree, List<Unit> units)
    {
        if (units.Count == 0)
        {
            quadTree.Clear();
            return;
        }
        Span<(float, float, Unit)> units1 = new (float, float, Unit)[units.Count];
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            units1[i] = (unit.position.X, unit.position.Y, unit);
        }
        quadTree.Initialize(units1);
    }

    public static void ClearSearch<T>(this QuadTree<T> quadTree, List<T> unitIds, Vector2 position, float radius)
    {
        unitIds.Clear();
        quadTree.Search(unitIds, position, radius);
    }

    //public static void BuildQuadTree<T>(this QuadTree<T> quadTree, List<Vector2> units)
    //{
    //    if (units.Count == 0) return;
    //    Span<float> units1 = stackalloc float[units.Count];
    //    Span<float> units2 = stackalloc float[units.Count];
    //    Span<T> units3 = new T[units.Count];
    //    for (int i = 0; i < units.Count; i++)
    //    {
    //        var unit = units[i];
    //        units1[i] = (unit.X, unit.Y, i);
    //    }
    //    quadTree.Initialize(units1);
    //}

    public static void Group(this QuadTree<int> quadTree, List<Vector2> origin, List<Vector2> result, float maxDistance)
    {
        Dictionary<int, int> ids = new();
        Dictionary<int, int> alias = new();
        List<int> searchResult = new();

        for (int i = 0; i < quadTree.Count; i++)
        {
            var patioPoint = origin[i];
            int a = i;

            searchResult.Clear();
            quadTree.Search(searchResult, patioPoint, maxDistance);
            foreach (int j in searchResult)
            {
                if (ids.TryGetValue(j, out var b))
                {
                    if (a > b)
                    {
                        (a, b) = (b, a);
                    }
                    if (alias.TryGetValue(b, out var c) && c <= a)
                    {
                        if (alias.TryGetValue(a, out var d) && d <= c)
                        {

                        }
                        else if (c < a)
                        {
                            alias[a] = c;
                        }
                    }
                    else
                        alias[b] = a;
                }
            }
            ids[i] = a;
        }
        for (int i = 0; i < quadTree.Count; i++)
        {
            int t = i;
            while (alias.TryGetValue(t, out int t1))
            {
                if (t1 >= t)
                {
                    break;
                }
                t = t1;
            }
            if (alias.ContainsKey(i))
                alias[i] = t;
        }
        List<(int, Vector2)> reorder = new();
        foreach (var pair in ids)
        {
            var item = origin[pair.Key];
            int t = pair.Value;
            if (alias.TryGetValue(t, out var t1))
            {
                t = t1;
            }
            reorder.Add((t, item));
        }
        reorder.Sort((x, y) => x.Item1.CompareTo(y.Item1));
        int prev = 0;
        int pointCount = 0;
        Vector2 avg = Vector2.Zero;
        for (int i = 0; i < reorder.Count; i++)
        {
            if (reorder[i].Item1 != prev)
            {
                var p1 = avg / pointCount;
                result.Add(p1);
                avg = Vector2.Zero;
                pointCount = 0;
                prev = reorder[i].Item1;
            }
            avg += reorder[i].Item2;
            pointCount++;
        }
        if (pointCount > 0)
        {
            var p1 = avg / pointCount;
            result.Add(p1);
        }
    }

    public static void Group(this QuadTree<Unit> quadTree, List<Vector2> result, float maxDistance, int minCull = 0)
    {
        Dictionary<Unit, int> ids = new();
        Dictionary<int, int> alias = new();
        List<Unit> searchResult = new();

        for (int i = 0; i < quadTree.Count; i++)
        {
            var patioPoint = new Vector2(quadTree.points[i].Item1, quadTree.points[i].Item2);
            int a = i;

            searchResult.Clear();
            quadTree.Search(searchResult, patioPoint, maxDistance);
            foreach (var j in searchResult)
            {
                if (ids.TryGetValue(j, out var b))
                {
                    if (a > b)
                    {
                        (a, b) = (b, a);
                    }
                    if (alias.TryGetValue(b, out var c) && c <= a)
                    {
                        if (alias.TryGetValue(a, out var d) && d <= c)
                        {

                        }
                        else if (c < a)
                        {
                            alias[a] = c;
                        }
                    }
                    else
                        alias[b] = a;
                }
            }
            ids[quadTree.points[i].Item3] = a;
        }
        for (int i = 0; i < quadTree.Count; i++)
        {
            int t = i;
            while (alias.TryGetValue(t, out int t1))
            {
                if (t1 >= t)
                {
                    break;
                }
                t = t1;
            }
            if (alias.ContainsKey(i))
                alias[i] = t;
        }
        List<(int, Vector2)> reorder = new();
        foreach (var pair in ids)
        {
            var item = pair.Key;
            int t = pair.Value;
            if (alias.TryGetValue(t, out var t1))
            {
                t = t1;
            }
            reorder.Add((t, item.position));
        }
        reorder.Sort((x, y) => x.Item1.CompareTo(y.Item1));
        int prev = 0;
        int pointCount = 0;
        Vector2 avg = Vector2.Zero;
        for (int i = 0; i < reorder.Count; i++)
        {
            if (reorder[i].Item1 != prev)
            {
                var p1 = avg / pointCount;
                if (pointCount > minCull)
                    result.Add(p1);
                avg = Vector2.Zero;
                pointCount = 0;
                prev = reorder[i].Item1;
            }
            avg += reorder[i].Item2;
            pointCount++;
        }
        if (pointCount > minCull)
        {
            var p1 = avg / pointCount;
            result.Add(p1);
        }
    }
}
