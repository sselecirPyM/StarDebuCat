using MilkWang2.Utility;
using StarDebuCat.Algorithm;
using StarDebuCat.Data;

namespace MilkWang2.Simulation
{
    public class UnitManager
    {
        public KDTree2<Unit> unitKDTree = new KDTree2<Unit>();
        public Dictionary<ulong, Unit> unitsDictionary = new Dictionary<ulong, Unit>();
        public List<Unit> units = new List<Unit>();

        public HashSet<ulong> previousUnits = new HashSet<ulong>();
        public HashSet<ulong> currentUnits = new HashSet<ulong>();

        public List<Unit> deadUnits = new List<Unit>();
        public List<Unit> totalDeadUnits = new List<Unit>();

        public GameData gameData = new GameData();

        public event Action<Unit> OnUnitAdd;
        public event Action<Unit> OnUnitDead;

        public void Init(SC2APIProtocol.ResponseData responseData)
        {
            gameData = Util.GetData<GameData>("GameData/GameData.json");
            gameData.Initialize(responseData);
        }

        public void Update(SC2APIProtocol.ResponseObservation observation)
        {
            var rd = observation.Observation.RawData;
            var loop = observation.Observation.GameLoop;
            deadUnits.Clear();
            if (rd.Event != null)
                foreach (var d in rd.Event.DeadUnits)
                {
                    if (unitsDictionary.TryGetValue(d, out var unit))
                    {
                        deadUnits.Add(unit);
                        totalDeadUnits.Add(unit);
                        OnUnitDead?.Invoke(unit);
                    }
                }
            (currentUnits, previousUnits) = (previousUnits, currentUnits);
            currentUnits.Clear();
            foreach (var unit in rd.Units)
            {
                currentUnits.Add(unit.Tag);
                if (unitsDictionary.TryGetValue(unit.Tag, out var unit1))
                {
                    unit1.UpdateBy(unit);
                }
                else
                {
                    unit1 = new Unit();
                    unit1.firstFrame = (int)loop;
                    unit1.UpdateBy(unit);
                    unitsDictionary.Add(unit.Tag, unit1);
                    OnUnitAdd?.Invoke(unit1);
                }
            }
            previousUnits.ExceptWith(currentUnits);
            foreach (var u in previousUnits)
            {
                unitsDictionary.Remove(u);
            }

            units.Clear();
            units.AddRange(unitsDictionary.Values);

            unitKDTree.Clear();
            foreach (var unit in units)
            {
                unitKDTree.Add(unit, unit.position);
            }
            unitKDTree.Build();

        }

    }
}
