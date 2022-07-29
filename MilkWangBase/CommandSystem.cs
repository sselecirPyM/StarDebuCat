using StarDebuCat.Attributes;
using StarDebuCat.Commanding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using StarDebuCat;
using StarDebuCat.Data;
using StarDebuCat.Utility;

namespace MilkWangBase
{
    public class CommandSystem
    {
        AnalysisSystem analysisSystem;
        GameConnection gameConnection;

        [Find("ReadyToPlay")]
        bool readyToPlay;


        public ActionList actionList = new();

        void Update()
        {
            if (!readyToPlay)
                return;
            gameConnection.RequestAction(actionList.actions);
            actionList.Clear();

        }

        public void EnqueueChat(string message, bool broadcast) => actionList.EnqueueChat(message, broadcast);

        public void EnqueueAbility(Unit unit, Abilities abilities) => actionList.EnqueueAbility(new[] { unit }, abilities);
        public void EnqueueAbility(Unit unit, Abilities abilities, Vector2 target) => actionList.EnqueueAbility(new[] { unit }, abilities, target);
        public void EnqueueAbility(Unit unit, Abilities abilities, Unit target) => actionList.EnqueueAbility(new[] { unit }, abilities, target.Tag);
        public void EnqueueAbility(Unit unit, Abilities abilities, ulong target) => actionList.EnqueueAbility(new[] { unit }, abilities, target);

        public void EnqueueAbility(IEnumerable<Unit> unit, Abilities abilities) => actionList.EnqueueAbility(unit, abilities);
        public void EnqueueAbility(IEnumerable<Unit> unit, Abilities abilities, Vector2 target) => actionList.EnqueueAbility(unit, abilities, target);

        public void EnqueueBuild(Unit unit, UnitType unitType, Vector2 position) => EnqueueBuild(unit.Tag, unitType, position);
        public void EnqueueBuild(ulong unit, UnitType unitType, Vector2 position)
        {
            var cmd = ActionList.Command(GetBuildAbility(unitType));
            cmd.ActionRaw.UnitCommand.UnitTags.Add(unit);
            cmd.ActionRaw.UnitCommand.TargetWorldSpacePos = new SC2APIProtocol.Point2D();
            cmd.ActionRaw.UnitCommand.TargetWorldSpacePos.X = position.X;
            cmd.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = position.Y;
            actionList.actions.Add(cmd);
        }
        public void EnqueueBuild(Unit unit, UnitType unitType, Unit target) => EnqueueBuild(unit.Tag, unitType, target.Tag);
        public void EnqueueBuild(ulong unit, UnitType unitType, ulong target)
        {
            var cmd = ActionList.Command(GetBuildAbility(unitType));
            cmd.ActionRaw.UnitCommand.UnitTags.Add(unit);
            cmd.ActionRaw.UnitCommand.TargetUnitTag = target;
            actionList.actions.Add(cmd);
        }

        public void EnqueueTrain(Unit unit, UnitType unitType) => EnqueueTrain(unit.Tag, unitType);
        public void EnqueueTrain(ulong unit, UnitType unitType)
        {
            var cmd = ActionList.Command(GetBuildAbility(unitType));
            cmd.ActionRaw.UnitCommand.UnitTags.Add(unit);
            actionList.actions.Add(cmd);
        }
        public Abilities GetBuildAbility(UnitType unitType)
        {
            return (Abilities)analysisSystem.unitTypeDatas[(int)unitType].AbilityId;
        }
    }
}
