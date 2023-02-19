using System.Collections.Generic;

namespace MilkWangBase.Utility;

public static class DictionaryExt
{
    public static void Increment<T>(this Dictionary<T, int> dict, T type)
    {
        dict.TryGetValue(type, out int val);
        dict[type] = val + 1;
    }
    public static void Decrement<T>(this Dictionary<T, int> dict, T type)
    {
        dict.TryGetValue(type, out int val);
        dict[type] = val - 1;
    }
}
