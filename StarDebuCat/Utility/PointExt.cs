using System.Numerics;

namespace StarDebuCat.Utility;

public static class PointExt
{
    public static Vector2 ToVector2(this SC2APIProtocol.Point point)
    {
        return new Vector2(point.X, point.Y);
    }

    public static Vector2 ToVector2(this SC2APIProtocol.Point2D point)
    {
        return new Vector2(point.X, point.Y);
    }

    public static SC2APIProtocol.Point ToPoint(this Vector2 vector2)
    {
        return new SC2APIProtocol.Point() { X = vector2.X, Y = vector2.Y };
    }

    public static SC2APIProtocol.Point ToPoint(this Vector2 vector2, float Z)
    {
        return new SC2APIProtocol.Point() { X = vector2.X, Y = vector2.Y, Z = Z };
    }
}
