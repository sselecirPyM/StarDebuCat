using CommandLine;
using SC2APIProtocol;

namespace MilkWang1;

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
    [Option("MapDir")]
    public string MapDir { get; set; }
    [Option("Debug")]
    public bool Debug { get; set; }

    [Option('a', "ComputerRace")]
    public Race ComputerRace { get; set; } = Race.Random;
    [Option('d', "ComputerDifficulty")]
    public Difficulty ComputerDifficulty { get; set; } = Difficulty.VeryHard;

    [Option("OpponentId")]
    public string OpponentId { get; set; } = "LocalPlayer";

    [Option("Learning")]
    public bool Learning { get; set; }
    [Option("Repeat")]
    public int Repeat { get; set; } = 1;
    [Option("AIBuild")]
    public AIBuild AIBuild { get; set; } = AIBuild.RandomBuild;

    [Option]
    public string TestStrategy { get; set; }
}
