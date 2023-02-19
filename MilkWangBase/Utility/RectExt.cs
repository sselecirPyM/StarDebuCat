using System.Drawing;

namespace MilkWangBase.Utility;

public static class RectExt
{
    public static Rectangle GetRectangle(SC2APIProtocol.RectangleI rectangleI)
    {
        var P0 = rectangleI.P0;
        var P1 = rectangleI.P1;
        Rectangle rect = new Rectangle(P0.X, P0.Y, P1.X - P0.X, P1.Y - P0.Y);
        return rect;
    }
}
