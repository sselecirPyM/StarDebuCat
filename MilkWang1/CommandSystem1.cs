using StarDebuCat;
using StarDebuCat.Data;
using StarDebuCat.Utility;
using System.Numerics;

namespace MilkWang1;

public class CommandSystem1
{
    public GameData GameData;
    public GameConnectionFSM gameConnection;

    public ActionList actionList = new();

    public void Update()
    {
        var action = new SC2APIProtocol.RequestAction();
        action.Actions.AddRange(actionList.actions);
        gameConnection.SendMessage(action);
        actionList.Clear();

    }

    float maxOptimiseDistance = 2e-1f;

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
                var pos = order.TargetWorldSpacePos.ToVector2();
                if (Vector2.DistanceSquared(target, pos) < maxOptimiseDistance)
                {
                    return;
                }
            }

        }
        else if (unit.orders.Count == 0)
        {
            if (Vector2.DistanceSquared(target, unit.position) < maxOptimiseDistance)
            {
                return;
            }
        }
        EnqueueAbility(unit, abilities, target);
    }

    public void OptimiseCommand(Unit unit, Abilities abilities, Unit targetUnit)
    {
        if (unit == null)
            return;
        if (unit.TryGetOrder(out var order) &&
            order.TargetCase == SC2APIProtocol.UnitOrder.TargetOneofCase.TargetUnitTag)
        {
            var unitAbilities = (Abilities)order.AbilityId;
            switch (unitAbilities)
            {
                case Abilities.ATTACK_ATTACK:
                    unitAbilities = Abilities.ATTACK;
                    break;
            }
            if (unitAbilities == abilities && order.TargetUnitTag == targetUnit.Tag)
            {
                return;
            }
        }
        EnqueueAbility(unit, abilities, targetUnit);
    }

    public void OptimiseCommand(Unit unit, Abilities abilities)
    {
        if (unit == null)
            return;
        if (unit.TryGetOrder(out var order) &&
            order.TargetCase == SC2APIProtocol.UnitOrder.TargetOneofCase.None)
        {
            var unitAbilities = (Abilities)order.AbilityId;
            if (unitAbilities == abilities)
            {
                return;
            }
        }
        EnqueueAbility(unit, abilities);
    }

    public void ToggleAutocastAbility(Unit unit, Abilities abilities)
    {
        actionList.UnitsAutocastAction(abilities, unit.Tag);
    }

    public void EnqueueAbility(Unit unit, Abilities abilities) => actionList.EnqueueAbility(unit.Tag, abilities);
    public void EnqueueAbility(Unit unit, Abilities abilities, Vector2 target) => actionList.EnqueueAbility(unit.Tag, abilities, target);
    public void EnqueueAbility(Unit unit, Abilities abilities, Unit target) => actionList.EnqueueAbility(unit.Tag, abilities, target.Tag);
    public void EnqueueAbility(Unit unit, Abilities abilities, ulong target) => actionList.EnqueueAbility(unit.Tag, abilities, target);


    public void EnqueueBuild(Unit unit, UnitType unitType, Vector2 position) => EnqueueBuild(unit.Tag, unitType, position);
    public void EnqueueBuild(ulong unit, UnitType unitType, Vector2 position)
    {
        var cmd = ActionList.Command(GetBuildAbility(unitType));
        cmd.ActionRaw.UnitCommand.TargetWorldSpacePos = new SC2APIProtocol.Point2D
        {
            X = position.X,
            Y = position.Y
        };
        actionList.UnitsAction(cmd, unit);
        //Console.WriteLine("{0}:{1}", analysisSystem.GameLoop / 22.4, unitType.ToString());
    }
    public void EnqueueBuild(Unit unit, UnitType unitType, Unit target) => EnqueueBuild(unit.Tag, unitType, target.Tag);
    public void EnqueueBuild(ulong unit, UnitType unitType, ulong target)
    {
        var cmd = ActionList.Command(GetBuildAbility(unitType));
        cmd.ActionRaw.UnitCommand.TargetUnitTag = target;
        actionList.UnitsAction(cmd, unit);
        //Console.WriteLine("{0}:{1}", analysisSystem.GameLoop / 22.4, unitType.ToString());
    }

    public void EnqueueTrain(Unit unit, UnitType unitType) => EnqueueTrain(unit.Tag, unitType);
    public void EnqueueTrain(ulong unit, UnitType unitType)
    {
        var cmd = ActionList.Command(GetBuildAbility(unitType));
        actionList.UnitsAction(cmd, unit);
        //Console.WriteLine("{0}:{1}", analysisSystem.GameLoop / 22.4, unitType.ToString());
    }
    Abilities GetBuildAbility(UnitType unitType)
    {
        return (Abilities)GameData.GetUnitTypeData(unitType).AbilityId;
    }
}
