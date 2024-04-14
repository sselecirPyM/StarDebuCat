using System;
using System.Composition;

namespace MilkWang1.Micros;

//[Export(typeof(IMicro))]
//[ExportMetadata("unit", UnitType.TERRAN_VIKINGASSAULT)]
//[ExportMetadata("unit2", UnitType.TERRAN_VIKINGFIGHTER)]
public class VikingMicro : IMicro
{
    [Import]
    public CommandSystem1 commandSystem { get; set; }
    [Import]
    public AnalysisSystem1 analysisSystem { get; set; }
    [Import]
    public BattleSystem1 battleSystem { get; set; }

    public void Micro(BattleUnit battleUnit)
    {

    }

    public void Update()
    {

    }

    public static bool Filter()
    {
        return true;
    }
}
