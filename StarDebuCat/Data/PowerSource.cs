using System.Numerics;

namespace StarDebuCat.Data;

public class PowerSource
{
    public Vector2 Pos;
    public float Radius;
    public ulong Tag;

    public void Update(SC2APIProtocol.PowerSource powerSource)
    {
        Pos = new Vector2(powerSource.Pos.X, powerSource.Pos.Y);
        Radius = powerSource.Radius;
        Tag = powerSource.Tag;
    }
}
