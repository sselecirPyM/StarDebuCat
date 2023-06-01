using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
namespace MilkWang1.Learning;

public class WinLostDraw
{
    public int win;
    public int lost;
    public int draw;
    public int total { get => win + lost + draw; }

    public void AddWin()
    {
        win++;
    }

    public void AddLost()
    {
        lost++;
    }

    public void AddDraw()
    {
        draw++;
    }
}

public class Statistics
{
    static string statisticsFile = "data/statistics.json";

    public WinLostDraw all = new();
    public WinLostDraw vsT = new();
    public WinLostDraw vsP = new();
    public WinLostDraw vsZ = new();

    public Dictionary<string, WinLostDraw> vsEnemy = new();
    public Dictionary<string, WinLostDraw> useStrategy = new();

    public void LogResult(SC2APIProtocol.Race enemyRace, SC2APIProtocol.Result result, string enemyId, string strategy)
    {
        if (!this.vsEnemy.TryGetValue(enemyId, out var vsEnemy1))
        {
            vsEnemy1 = new WinLostDraw();
            vsEnemy[enemyId] = vsEnemy1;
        }
        if (!this.useStrategy.TryGetValue(strategy, out var st))
        {
            st = new WinLostDraw();
            useStrategy[strategy] = st;
        }
        WinLostDraw vsRace = enemyRace switch
        {
            SC2APIProtocol.Race.Terran => vsT,
            SC2APIProtocol.Race.Protoss => vsP,
            SC2APIProtocol.Race.Zerg => vsZ,
            _ => null,
        };
        WinLostDraw[] winLostDraws = new[]
        {
            vsEnemy1,
            all,
            vsRace,
            st
        };

        if (result == SC2APIProtocol.Result.Victory)
        {
            foreach(var s in winLostDraws)
            {
                s.AddWin();
            }
        }
        if (result == SC2APIProtocol.Result.Defeat)
        {
            foreach (var s in winLostDraws)
            {
                s.AddLost();
            }
        }
        if (result == SC2APIProtocol.Result.Tie)
        {
            foreach (var s in winLostDraws)
            {
                s.AddDraw();
            }
        }
    }

    public static Statistics Load()
    {
        if (new FileInfo(statisticsFile).Exists)
        {
            return Util.GetData<Statistics>(statisticsFile);

        }
        else
        {
            return new Statistics();
        }
    }

    public void Save()
    {
        var directoryInfo = new DirectoryInfo(Path.GetDirectoryName(statisticsFile));
        if (!directoryInfo.Exists)
        {
            directoryInfo.Create();
        }
        File.WriteAllText(statisticsFile, JsonConvert.SerializeObject(this));
    }
}
