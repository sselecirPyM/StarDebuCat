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
        actionChat.channel = broadcast ? ActionChat.Channel.Broadcast : ActionChat.Channel.Team;
        actionChat.Message = message;
        actions.Add(new Action { ActionChat = actionChat });
    }

    public void EnqueueAbility(IReadOnlyList<Unit> units, Abilities abilities)
    {
        UnitsAction(Command(abilities), units);
    }
    public void EnqueueAbility(IReadOnlyList<Unit> units, Abilities abilities, ulong target)
    {
        UnitsAction(Command(abilities, target), units);
    }
    public void EnqueueAbility(IReadOnlyList<Unit> units, Abilities abilities, Vector2 target)
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

    public void UnitsAction(Action action, IReadOnlyList<Unit> units)
    {
        ulong[] units1 = new ulong[units.Count];
        for (int i = 0; i < units.Count; i++)
            units1[i] = units[i].Tag;

        action.ActionRaw.UnitCommand.UnitTags = units1;
        if (action.ActionRaw.UnitCommand.UnitTags.Length > 0)
            actions.Add(action);
    }

    public void UnitsAction(Action action, Unit unit)
    {
        if (unit == null)
            return;
        //action.ActionRaw.UnitCommand.UnitTags.Add(unit.Tag);
        action.ActionRaw.UnitCommand.UnitTags = new ulong[] { unit.Tag };
        actions.Add(action);
    }

    public void UnitsAction(Action action, ulong unit)
    {
        //action.ActionRaw.UnitCommand.UnitTags.Add(unit.Tag);
        action.ActionRaw.UnitCommand.UnitTags = new ulong[] { unit };
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
