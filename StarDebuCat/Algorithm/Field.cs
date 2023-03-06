using System;
using System.Numerics;

namespace StarDebuCat.Algorithm;

public class Field
{
    int Width;
    int Height;
    Vector2[] vectors;

    public Vector2 Query(Vector2 position)
    {
        position = position - new Vector2(0.5f, 0.5f);
        int l = Math.Clamp((int)position.X, 0, Height - 1);
        int r = Math.Min(l + 1, Width - 1);
        int t = Math.Clamp((int)position.Y, 0, Height - 1);
        int b = Math.Min(t + 1, Height - 1);

        float rv = position.X - MathF.Floor(position.X);
        float lv = 1 - rv;
        float bv = position.Y - MathF.Floor(position.Y);
        float tv = 1 - bv;

        Vector2 vec =
            vectors[l + t * Width] * lv * tv +
            vectors[r + t * Width] * rv * tv +
            vectors[l + b * Width] * lv * bv +
            vectors[r + b * Width] * rv * bv;

        return vec;
    }

    //public void BuildNearestField2(Image image)
    //{
    //    Width = image.Width;
    //    Height = image.Height;
    //    vectors = new Vector2[Width * Height];
    //    var vectors2 = new Vector2[Width * Height];

    //    for (int j = 0; j < Height; j++)
    //    {
    //        int near = 0;
    //        for (int i = 0; i < Width; i++)
    //        {
    //            bool result = image.Query(i, j) > 0;
    //            result = !result;
    //            if (result)
    //            {
    //                near = i;
    //            }
    //            else
    //            {
    //                float d = near - i;
    //                vectors2[i + j * Width].X = d;
    //            }
    //        }
    //        near = Width;
    //        for (int i = Width - 1; i >= 0; i--)
    //        {
    //            bool result = image.Query(i, j) > 0;
    //            result = !result;
    //            if (result)
    //            {
    //                near = i;
    //            }
    //            else
    //            {
    //                float d = near - i;
    //                if (d < -vectors2[i + j * Width].X)
    //                    vectors2[i + j * Width].X = d;
    //            }
    //        }
    //    }
    //    for (int i = 0; i < Width; i++)
    //    {
    //        int near = 0;
    //        for (int j = 0; j < Height; j++)
    //        {
    //            bool result = image.Query(i, j) > 0;
    //            result = !result;
    //            if (result)
    //            {
    //                near = j;
    //            }
    //            else
    //            {
    //                float d = near - j;
    //                vectors2[i + j * Width].Y = d;
    //            }
    //        }
    //        near = Height;
    //        for (int j = Height - 1; j >= 0; j--)
    //        {
    //            bool result = image.Query(i, j) > 0;
    //            result = !result;
    //            if (result)
    //            {
    //                near = j;
    //            }
    //            else
    //            {
    //                float d = near - j;
    //                if (d < -vectors2[i + j * Width].Y)
    //                    vectors2[i + j * Width].Y = d;
    //            }
    //        }
    //    }
    //    for (int i = 0; i < vectors2.Length; i++)
    //    {
    //        Vector2 r = vectors2[i];
    //        if (r.X == 0 || r.Y == 0)
    //        {
    //            continue;
    //        }
    //        int state = (Math.Abs(r.X) <= Math.Abs(r.Y)) ? 0 : 1;

    //        int _x = i % Width;
    //        int _y = i / Width;
    //        if (state == 0)
    //        {
    //            vectors[i] = new Vector2(0, r.Y);
    //            float length = vectors[i].LengthSquared();
    //            for (int x = 1; x <= _x; x++)
    //            {
    //                int index = i + x;
    //                if (index >= 0 && index < vectors.Length)
    //                {
    //                    Vector2 n = vectors2[index];
    //                    n.X = x;
    //                    float length1 = n.LengthSquared();
    //                    if (length > length1)
    //                    {
    //                        length = length1;
    //                        vectors[i] = n;
    //                    }
    //                }
    //                index = i - x;
    //                if (index >= 0 && index < vectors.Length)
    //                {
    //                    Vector2 n = vectors2[index];
    //                    n.X = -x;
    //                    float length1 = n.LengthSquared();
    //                    if (length > length1)
    //                    {
    //                        length = length1;
    //                        vectors[i] = n;
    //                    }
    //                }
    //            }
    //        }
    //        else
    //        {
    //            vectors[i] = new Vector2(r.X, 0);
    //            float length = vectors[i].LengthSquared();
    //            for (int y = 1; y <= _y; y++)
    //            {
    //                int index = i + y * Width;
    //                if (index >= 0 && index < vectors.Length)
    //                {
    //                    Vector2 n = vectors2[index];
    //                    n.Y = y;
    //                    float length1 = n.LengthSquared();
    //                    if (length > length1)
    //                    {
    //                        length = length1;
    //                        vectors[i] = n;
    //                    }
    //                }
    //                index = i - y * Width;
    //                if (index >= 0 && index < vectors.Length)
    //                {
    //                    Vector2 n = vectors2[index];
    //                    n.Y = -y;
    //                    float length1 = n.LengthSquared();
    //                    if (length > length1)
    //                    {
    //                        length = length1;
    //                        vectors[i] = n;
    //                    }
    //                }
    //            }
    //        }
    //    }
    //}

    public void BuildNearestField(Image image, bool invert)
    {
        Width = image.Width;
        Height = image.Height;
        vectors = new Vector2[Width * Height];
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                vectors[x + y * Width] = (image.Query(x, y) == 0 ^ invert) ? Vector2.Zero : new Vector2(99999, 99999);
            }
        }
        GenerateSDF();
    }

    Vector2 GetPointInf(int x, int y)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
            return vectors[x + y * Width];
        else return new Vector2(99999, 99999);
    }

    void Put(int x, int y, Vector2 p)
    {
        this.vectors[x + y * Width] = p;
    }

    void Compare(ref Vector2 p, int x, int y, int offsetx, int offsety)
    {
        Vector2 other = this.GetPointInf(x + offsetx, y + offsety);
        other.X += offsetx;
        other.Y += offsety;

        if (other.LengthSquared() < p.LengthSquared())
            p = other;
    }

    void GenerateSDF()
    {
        // Pass 0
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Vector2 p = GetPointInf(x, y);
                Compare(ref p, x, y, -1, 0);
                Compare(ref p, x, y, 0, -1);
                Compare(ref p, x, y, -1, -1);
                Compare(ref p, x, y, 1, -1);
                Put(x, y, p);
            }

            for (int x = Width - 1; x >= 0; x--)
            {
                Vector2 p = GetPointInf(x, y);
                Compare(ref p, x, y, 1, 0);
                Put(x, y, p);
            }
        }

        // Pass 1
        for (int y = Height - 1; y >= 0; y--)
        {
            for (int x = Width - 1; x >= 0; x--)
            {
                Vector2 p = GetPointInf(x, y);
                Compare(ref p, x, y, 1, 0);
                Compare(ref p, x, y, 0, 1);
                Compare(ref p, x, y, -1, 1);
                Compare(ref p, x, y, 1, 1);
                Put(x, y, p);
            }

            for (int x = 0; x < Width; x++)
            {
                Vector2 p = GetPointInf(x, y);
                Compare(ref p, x, y, -1, 0);
                Put(x, y, p);
            }
        }
    }
}
