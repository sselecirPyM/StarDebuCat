﻿using System.Numerics;

namespace StarDebuCat.Data;

public class PointEffect
{
    public ulong Id;
    public Vector2 Pos;
    public void Update(ulong id, Vector2 pos)
    {
        Id = id;
        Pos = pos;
    }
}
