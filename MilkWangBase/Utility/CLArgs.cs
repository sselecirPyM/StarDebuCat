using CommandLine;
using SC2APIProtocol;

namespace MilkWangBase.Utility;

public class CLArgs
{
    [Option('g', "GamePort")]
    public int GamePort { get; set; } = 5678;
    [Option('o', "StartPort")]
    public int StartPort { get; set; } = 5678;
    [Option('l', "LadderServer")]
    public string LadderServer { get; set; }
    [Option('m', "Map")]
    public string Map { get; set; }
    [Option("Debug")]
    public bool Debug { get; set; }

    [Option('a', "ComputerRace")]
    public Race ComputerRace { get; set; } = Race.Random;
    [Option('d', "ComputerDifficulty")]
    public Difficulty ComputerDifficulty { get; set; } = Difficulty.VeryHard;

    [Option("OpponentId")]
    public string OpponentId { get; set; }
}
