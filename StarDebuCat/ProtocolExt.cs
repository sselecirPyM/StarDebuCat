using System.Numerics;

namespace SC2APIProtocol;

partial class Point2D
{
    public static implicit operator Vector2(Point2D point)
    {
        return new Vector2(point.X, point.Y);
    }
}
