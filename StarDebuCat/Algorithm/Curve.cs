using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace StarDebuCat.Algorithm
{
    public class Curve
    {
        List<Vector2> points = new();

        public float Sample(float time, int smooth = 1)
        {
            if (points.Count == 0)
                return 0;
            if (points.Count == 1)
                return points[0].Y;
            int index = binarySearch(time);
            if (index == points.Count - 1 && index > smooth)
            {
                var l = points[index - smooth];
                var r = points[index];

                return r.Y + (time - r.X) / (r.X - l.X) * (r.Y - l.Y);
            }
            else if (index >= 0 && points.Count > smooth + index)
            {
                var l = points[index];
                var r = points[index + smooth];

                return l.Y + (time - l.X) / (r.X - l.X) * (r.Y - l.Y);
            }
            return points[0].Y;
        }

        public void AddPoint(float position, float value)
        {
            points.Add(new(position, value));
            points.Sort((u, v) => u.X.CompareTo(v.X));
        }

        int binarySearch(float time)
        {
            int left = 0;
            int right = points.Count;
            int mid = (left + right - 1) / 2;

            while (left + 1 < right)
            {
                if (points[mid].X < time)
                {
                    left = mid + 1;
                }
                else if (points[mid].X > time)
                {
                    right = mid;
                }
                else
                {
                    return mid;
                }
                mid = (left + right - 1) / 2;
            }
            if (left == 0)
            {
                if (time < points[left].X)
                    return -1;
            }
            return left;
        }
    }
}
