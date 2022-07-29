using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace StarDebuCat.Data
{
    public class Effect
    {
        public ulong Id;
        public Vector2 Pos;
        public void Update(ulong id, Vector2 pos)
        {
            Id = id;
            Pos = pos;
        }
    }
}
