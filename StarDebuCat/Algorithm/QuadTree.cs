using System;
using System.Collections.Generic;
using System.Numerics;

namespace StarDebuCat.Algorithm;

public enum SplitType
{
    BranchX,
    BranchY,
    Leaf,
}

public struct Point
{
    public float x;
    public float y;

    public Point(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    public static implicit operator (float, float)(Point point)
    {
        return (point.x, point.y);
    }
    public static implicit operator Point((float, float) point)
    {
        return new(point.Item1, point.Item2);
    }
}

public struct QuadTreeNode
{
    public int left;
    public int right;
    public float splitCoord;
    public SplitType splitType;
    public int children1;
    public int children2;
    public int childCount;
}

public class QuadTree<T>
{
    static float Pow2(float x)
    {
        return x * x;
    }

    static bool InRange(Point point, Point point1, float radius)
    {
        if (Pow2(radius) >= (Pow2(point.x - point1.x) + Pow2(point.y - point1.y)))
        {
            return true;
        }
        return false;
    }

    public List<QuadTreeNode> nodes = new List<QuadTreeNode>();

    //x,y,id
    public (float, float, T)[] points;

    public int Count;

    Point minX;
    Point maxX;

    int PartitionX(int left, int right, float split)
    {
        if (left > right) throw new Exception();
        if (left == right) return left;
        for (int i = left; i < right; i++)
        {
            if (points[i].Item1 < split)
            {
                (points[i], points[left]) = (points[left], points[i]);
                left++;
            }
        }
        return left;
    }

    int PartitionY(int left, int right, float split)
    {
        if (left > right) throw new Exception();
        if (left == right) return left;
        for (int i = left; i < right; i++)
        {
            if (points[i].Item2 < split)
            {
                (points[i], points[left]) = (points[left], points[i]);
                left++;
            }
        }
        return left;
    }

    int Build(int l, int r, Point min, Point max, int depth)
    {
        if (l == r) return -1;
        QuadTreeNode node = new QuadTreeNode();
        node.left = l;
        node.right = r;
        node.children1 = -1;
        node.children2 = -1;

        node.childCount = node.right - node.left;

        if (r - l < 16 || depth > 8)
        {
            node.splitType = SplitType.Leaf;
            nodes.Add(node);
            return nodes.Count - 1;
        }
        if (max.x - min.x > max.y - min.y)
            node.splitType = SplitType.BranchX;
        else
            node.splitType = SplitType.BranchY;

        depth++;

        if (node.splitType == SplitType.BranchX)
        {
            float midX = (min.x + max.x) / 2;

            int d = PartitionX(l, r, midX);
            node.splitCoord = midX;

            Point min1 = new(min.x, min.y);
            Point max1 = new(midX - 1, max.y);
            node.children1 = Build(l, d, min1, max1, depth);
            Point min2 = new(midX, min.y);
            Point max2 = new(max.x, max.y);
            node.children2 = Build(d, r, min2, max2, depth);
        }
        else
        {
            float midY = (min.y + max.y) / 2;

            int d = PartitionY(l, r, midY);
            node.splitCoord = midY;

            Point min3 = new(min.x, min.y);
            Point max3 = new(max.x, midY - 1);
            node.children1 = Build(l, d, min3, max3, depth);
            Point min4 = new(min.x, midY);
            Point max4 = new(max.x, max.y);
            node.children2 = Build(d, r, min4, max4, depth);
        }

        nodes.Add(node);

        return nodes.Count - 1;
    }

    public void Initialize(Span<float> Xs, Span<float> Ys, Span<T> Ids)
    {
        int n = Xs.Length;
        Count = n;
        //this.points = new (float, float, T)[n];
        if (this.points == null || this.points.Length < n)
        {
            this.points = new (float, float, T)[n + 16];
        }
        for (int i = 0; i < n; i++)
        {
            this.points[i] = (Xs[i], Ys[i], Ids[i]);
        }

        minX = (Xs[0], Ys[0]);
        maxX = (Xs[0], Ys[0]);

        for (int i = 1; i < n; i++)
        {
            minX = (Math.Min(minX.x, Xs[i]), Math.Min(minX.y, Ys[i]));
            maxX = (Math.Max(maxX.x, Xs[i]), Math.Max(maxX.y, Ys[i]));
        }


        nodes.EnsureCapacity(256);
        Build(0, n, minX, maxX, 0);
    }

    public void Initialize(Span<(float, float, T)> Xs)
    {
        int n = Xs.Length;
        Count = n;
        if (this.points == null || this.points.Length < n)
        {
            this.points = new (float, float, T)[n + 16];
        }

        for (int i = 0; i < n; i++)
        {
            this.points[i] = Xs[i];
        }

        minX = (Xs[0].Item1, Xs[0].Item2);
        maxX = (Xs[0].Item1, Xs[0].Item2);

        for (int i = 1; i < n; i++)
        {
            minX = (Math.Min(minX.x, Xs[i].Item1), Math.Min(minX.y, Xs[i].Item2));
            maxX = (Math.Max(maxX.x, Xs[i].Item1), Math.Max(maxX.y, Xs[i].Item2));
        }
        nodes.EnsureCapacity(256);
        Build(0, n, minX, maxX, 0);
    }

    bool _HitTest(int nodeIndex, Point point, float radius)
    {
        if (nodeIndex < 0) return false;
        var node = nodes[nodeIndex];
        if (node.childCount == 0) return false;

        if (node.splitType == SplitType.Leaf)
        {
            for (int i = node.left; i < node.right; i++)
            {
                if (Math.Abs(points[i].Item1 - point.x) > radius) continue;
                if (Math.Abs(points[i].Item2 - point.y) > radius) continue;
                if (InRange((points[i].Item1, points[i].Item2), point, radius))
                {
                    return true;
                }
            }
        }
        else if (node.splitType == SplitType.BranchX)
        {
            if (point.x - radius < node.splitCoord)
            {
                if (_HitTest(node.children1, point, radius))
                    return true;
            }
            if (point.x + radius >= node.splitCoord)
            {
                if (_HitTest(node.children2, point, radius))
                    return true;
            }
        }
        else
        {
            if (point.y - radius < node.splitCoord)
            {
                if (_HitTest(node.children1, point, radius))
                    return true;
            }
            if (point.y + radius >= node.splitCoord)
            {
                if (_HitTest(node.children2, point, radius))
                    return true;
            }
        }

        return false;
    }

    public bool HitTest(float x, float y, float radius)
    {
        return _HitTest(nodes.Count - 1, (x, y), radius);
    }
    public bool HitTest(Vector2 position, float radius)
    {
        return _HitTest(nodes.Count - 1, (position.X, position.Y), radius);
    }

    void _Search(List<T> unitIds, int nodeIndex, Point point, float radius)
    {
        if (nodeIndex < 0) return;
        var node = nodes[nodeIndex];
        if (node.childCount == 0) return;

        if (node.splitType == SplitType.Leaf)
        {
            for (int i = node.left; i < node.right; i++)
            {
                if (Math.Abs(points[i].Item1 - point.x) > radius) continue;
                if (Math.Abs(points[i].Item2 - point.y) > radius) continue;
                if (InRange((points[i].Item1, points[i].Item2), point, radius))
                {
                    unitIds.Add(points[i].Item3);
                }
            }
        }
        else if (node.splitType == SplitType.BranchX)
        {
            if (point.x - radius < node.splitCoord)
            {
                _Search(unitIds, node.children1, point, radius);
            }
            if (point.x + radius >= node.splitCoord)
            {
                _Search(unitIds, node.children2, point, radius);
            }
        }
        else
        {
            if (point.y - radius < node.splitCoord)
            {
                _Search(unitIds, node.children1, point, radius);
            }
            if (point.y + radius >= node.splitCoord)
            {
                _Search(unitIds, node.children2, point, radius);
            }
        }
    }
    public void Search(List<T> unitIds, float x, float y, float radius)
    {
        _Search(unitIds, nodes.Count - 1, (x, y), radius);
    }
    public void Search(List<T> unitIds, Vector2 position, float radius)
    {
        _Search(unitIds, nodes.Count - 1, (position.X, position.Y), radius);
    }

    public void Clear()
    {
        nodes.Clear();
        //points = null;
        maxX = new Point();
        minX = new Point();
    }
}
