using MilkWangBase.Attributes;

namespace MilkWang1.Attributes;

public class QuadTreeAttribute : XFindAttribute
{
    public QuadTreeAttribute(params object[] objects) : base("QuadTree", objects)
    {

    }
}
