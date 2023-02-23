using MilkWangBase.Attributes;
using StarDebuCat;
using StarDebuCat.Commanding;
using StarDebuCat.Data;
using System.Collections.Generic;
using System.Numerics;

namespace MilkWangBase;

public class CommandSystem
{
    AnalysisSystem analysisSystem;
    public GameConnection gameConnection;

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

    public void OptimiseCommand(Unit unit, Abilities abilities, Vector2 target)
    {
        if (unit == null)
            return;
        if (unit.TryGetOrder(out var order) &&
            order.TargetCase == SC2APIProtocol.UnitOrder.TargetOneofCase.TargetWorldSpacePos)
        {
            var unitAbilities = (Abilities)order.AbilityId;
            switch (unitAbilities)
            {
                case Abilities.ATTACK_ATTACK:
                    unitAbilities = Abilities.ATTACK;
                    break;
            }
            if (unitAbilities == abilities)
            {
                var pos = order.TargetWorldSpacePos;
                var pos1 = new Vector2(pos.X, pos.Y);
                if (Vector2.DistanceSquared(target, pos1) < 1e-1f)
                {
                    return;
                }
            }

        }
        else if (unit.orders.Count == 0)
        {
            if (Vector2.DistanceSquared(target, unit.position) < 1e-1f)
            {
                return;
            }
        }
        EnqueueAbility(unit, abilities, target);
    }

    public void EnqueueAbility(Unit unit, Abilities abilities) => actionList.EnqueueAbility(unit, abilities);
    public void EnqueueAbility(Unit unit, Abilities abilities, Vector2 target) => actionList.EnqueueAbility(unit, abilities, target);
    public void EnqueueAbility(Unit unit, Abilities abilities, Unit target) => actionList.EnqueueAbility(unit, abilities, target.Tag);
    public void EnqueueAbility(Unit unit, Abilities abilities, ulong target) => actionList.EnqueueAbility(unit, abilities, target);

    public void EnqueueAbility(IReadOnlyList<Unit> unit, Abilities abilities) => actionList.EnqueueAbility(unit, abilities);
    public void EnqueueAbility(IReadOnlyList<Unit> unit, Abilities abilities, Vector2 target) => actionList.EnqueueAbility(unit, abilities, target);

    public void EnqueueBuild(Unit unit, UnitType unitType, Vector2 position) => EnqueueBuild(unit.Tag, unitType, position);
    public void EnqueueBuild(ulong unit, UnitType unitType, Vector2 position)
    {
        var cmd = ActionList.Command(GetBuildAbility(unitType));
        cmd.ActionRaw.UnitCommand.TargetWorldSpacePos = new SC2APIProtocol.Point2D
        {
            X = position.X,
            Y = position.Y
        };
        //cmd.ActionRaw.UnitCommand.UnitTags.Add(unit);
        //actionList.actions.Add(cmd);
        actionList.UnitsAction(cmd, unit);
    }
    public void EnqueueBuild(Unit unit, UnitType unitType, Unit target) => EnqueueBuild(unit.Tag, unitType, target.Tag);
    public void EnqueueBuild(ulong unit, UnitType unitType, ulong target)
    {
        var cmd = ActionList.Command(GetBuildAbility(unitType));
        cmd.ActionRaw.UnitCommand.TargetUnitTag = target;
        //cmd.ActionRaw.UnitCommand.UnitTags.Add(unit);
        //actionList.actions.Add(cmd);
        actionList.UnitsAction(cmd, unit);
    }

    public void EnqueueTrain(Unit unit, UnitType unitType) => EnqueueTrain(unit.Tag, unitType);
    public void EnqueueTrain(ulong unit, UnitType unitType)
    {
        var cmd = ActionList.Command(GetBuildAbility(unitType));
        //cmd.ActionRaw.UnitCommand.UnitTags.Add(unit);
        //actionList.actions.Add(cmd);
        actionList.UnitsAction(cmd, unit);
    }
    Abilities GetBuildAbility(UnitType unitType)
    {
        return (Abilities)analysisSystem.GetUnitTypeData(unitType).AbilityId;
    }
}
