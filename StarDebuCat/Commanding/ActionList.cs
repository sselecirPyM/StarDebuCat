using SC2APIProtocol;
using StarDebuCat.Data;
using System.Collections.Generic;
using System.Numerics;
using Action = SC2APIProtocol.Action;
using Unit = StarDebuCat.Data.Unit;

namespace StarDebuCat.Commanding;

public class ActionList
{
    public List<Action> actions = new List<Action>();
    public void Clear()
    {
        actions.Clear();
    }
    public void EnqueueChat(string message, bool broadcast = false)
    {
        var actionChat = new ActionChat();
        actionChat.Channel = broadcast ? ActionChat.Types.Channel.Broadcast : ActionChat.Types.Channel.Team;
        actionChat.Message = message;
        actions.Add(new Action { ActionChat = actionChat });
    }

    public void EnqueueAbility(IEnumerable<Unit> units, Abilities abilities)
    {
        UnitsAction(Command(abilities), units);
    }
    public void EnqueueAbility(IEnumerable<Unit> units, Abilities abilities, ulong target)
    {
        UnitsAction(Command(abilities, target), units);
    }
    public void EnqueueAbility(IEnumerable<Unit> units, Abilities abilities, Vector2 target)
    {
        UnitsAction(Command(abilities, target), units);
    }

    public void EnqueueAbility(Unit unit, Abilities abilities)
    {
        UnitsAction(Command(abilities), unit);
    }
    public void EnqueueAbility(Unit unit, Abilities abilities, ulong target)
    {
        UnitsAction(Command(abilities, target), unit);
    }
    public void EnqueueAbility(Unit unit, Abilities abilities, Vector2 target)
    {
        UnitsAction(Command(abilities, target), unit);
    }

    public void UnitsAction(Action action, IEnumerable<Unit> units)
    {
        foreach (var unit in units)
            action.ActionRaw.UnitCommand.UnitTags.Add(unit.Tag);
        if (action.ActionRaw.UnitCommand.UnitTags.Count > 0)
            actions.Add(action);
    }

    public void UnitsAction(Action action, Unit unit)
    {
        if (unit == null)
            return;
        action.ActionRaw.UnitCommand.UnitTags.Add(unit.Tag);
        actions.Add(action);
    }

    public static Action Command(Abilities ability)
    {
        var action = new Action();
        action.ActionRaw = new ActionRaw();
        action.ActionRaw.UnitCommand = new ActionRawUnitCommand();
        action.ActionRaw.UnitCommand.AbilityId = (int)ability;
        return action;
    }

    public static Action Command(Abilities ability, ulong target)
    {
        var action = new Action();
        action.ActionRaw = new ActionRaw();
        action.ActionRaw.UnitCommand = new ActionRawUnitCommand();
        action.ActionRaw.UnitCommand.AbilityId = (int)ability;
        action.ActionRaw.UnitCommand.TargetUnitTag = target;
        return action;
    }

    public static Action Command(Abilities ability, Vector2 target)
    {
        var action = new Action();
        action.ActionRaw = new ActionRaw();
        action.ActionRaw.UnitCommand = new ActionRawUnitCommand();
        action.ActionRaw.UnitCommand.AbilityId = (int)ability;
        action.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D
        {
            X = target.X,
            Y = target.Y
        };
        return action;
    }
}
