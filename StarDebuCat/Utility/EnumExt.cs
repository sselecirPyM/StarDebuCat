using System;

namespace StarDebuCat.Utility
{
    public static class EnumExt
    {
        public static bool HasAnyFlag(this Enum source, Enum value)
        {
            return ((int)(object)source & (int)(object)value) != 0;
        }
    }
}
