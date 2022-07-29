using StarDebuCat;
using StarDebuCat.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MilkWangBase;
using MilkWangP1;

namespace MilkWang1
{
    public class BotController
    {
        [System]
        public GameConnection gameConnection;
        [System]
        public InputSystem inputSystem;
        [System]
        public AnalysisSystem analysisSystem;
        [System]
        public PredicationSystem predicationSystem;
        [System]
        public MarkerSystem markerSystem;
        [System]
        public ProtossBot1 terranBot1;
        [System]
        public BattleSystem battleSystem;
        [System]
        public BuildSystem buildSystem;
        [System]
        public CommandSystem commandSystem;
        [System]
        public DebugSystem debugSystem;
    }
}
