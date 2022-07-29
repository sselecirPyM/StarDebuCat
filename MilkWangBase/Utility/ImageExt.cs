using StarDebuCat.Algorithm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MilkWangBase.Utility
{
    public static class ImageExt
    {
        public static void Group(this Image image, List<Vector2> result, int minCount = 1)
        {
            if (image.BitsPerPixel != 1)
            {
                throw new NotImplementedException();
            }
            List<int> ids = new(image.Width * image.Height);

            Dictionary<int, int> alias = new();

            int idCount = 0;
            for (int j = 0; j < image.Height; j++)
                for (int i = 0; i < image.Width; i++)
                {
                    int pixelIndex = i + j * image.Width;
                    if (image.Query(i, j) == 0)
                    {
                        ids.Add(-1);
                        continue;
                    }
                    int current = idCount;
                    int left = -1;
                    int up = -1;
                    if (i > 0)
                        left = ids[pixelIndex - 1];
                    if (j > 0)
                        up = ids[pixelIndex - image.Width];

                    if (left != -1 && left < current)
                    {
                        current = left;
                    }
                    if (up != -1 && up < current)
                    {
                        current = up;
                    }
                    if (left != -1 && up != -1 && left != up)
                    {
                        int a = left;
                        int b = up;
                        if (a > b)
                            (a, b) = (b, a);
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

                    if (current == idCount)
                        idCount++;

                    ids.Add(current);
                }

            for (int i = 0; i < idCount; i++)
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
            for (int i = 0; i < ids.Count; i++)
            {
                Vector2 item = new(i % image.Width + 0.5f, i / image.Width + 0.5f);
                int t = ids[i];
                if (t == -1)
                    continue;
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
                    if (pointCount >= minCount)
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
                if (pointCount >= minCount)
                    result.Add(p1);
            }
        }
    }
}
