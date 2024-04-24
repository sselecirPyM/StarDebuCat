using StarDebuCat.Algorithm;
using System.Numerics;

namespace MilkWang2.Simulation
{
    public class PathManager
    {
        public Image pathingGrid;
        public Image placementGrid;
        public Image terrainHeight;

        public List<Vector2> startLocations;

        public void Init(SC2APIProtocol.ResponseGameInfo responseGameInfo)
        {
            var sr = responseGameInfo.StartRaw;
            pathingGrid = new Image(sr.PathingGrid);
            placementGrid = new Image(sr.PlacementGrid);
            terrainHeight = new Image(sr.TerrainHeight);
            startLocations = new List<Vector2>();
            foreach (var position in sr.StartLocations)
            {
                startLocations.Add(new Vector2(position.X, position.Y));
            }

        }

    }
}
