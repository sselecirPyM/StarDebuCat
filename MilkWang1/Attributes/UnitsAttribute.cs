using MilkWangBase.Attributes;

namespace MilkWang1.Attributes;

public class UnitsAttribute : XFindAttribute
{
    public UnitsAttribute(params object[] objects) : base("CollectUnits", objects)
    {
    }
}
