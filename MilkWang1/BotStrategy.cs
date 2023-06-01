﻿using StarDebuCat.Data;
using System.Collections.Generic;

namespace MilkWang1;

public class BotStrategyList
{
    public List<string> strategies;
}

public class BuildRail
{
    public Dictionary<UnitType, int>[] buildSequence;
    public int[] buildSequenceStart;
}

public class BotStrategy
{
    public string Name;
    public string Description;

    public BuildRail[] buildRails;
    public int attackCount;
}
