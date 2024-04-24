using SC2APIProtocol;
using StarDebuCat.Data;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Unit = StarDebuCat.Data.Unit;

namespace MilkWang1;

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

    public void EnqueueAbility(IReadOnlyList<ulong> units, Abilities abilities)
    {
        UnitsAction(Command(abilities), units);
    }
    public void EnqueueAbility(IReadOnlyList<ulong> units, Abilities abilities, ulong target)
    {
        UnitsAction(Command(abilities, target), units);
    }
    public void EnqueueAbility(IReadOnlyList<ulong> units, Abilities abilities, Vector2 target)
    {
        UnitsAction(Command(abilities, target), units);
    }

    public void EnqueueAbility(ulong unit, Abilities abilities)
    {
        UnitsAction(Command(abilities), unit);
    }
    public void EnqueueAbility(ulong unit, Abilities abilities, ulong target)
    {
        UnitsAction(Command(abilities, target), unit);
    }
    public void EnqueueAbility(ulong unit, Abilities abilities, Vector2 target)
    {
        UnitsAction(Command(abilities, target), unit);
    }

    public void UnitsAction(Action action, IReadOnlyList<ulong> units)
    {

        action.ActionRaw.UnitCommand.UnitTags = units.ToArray();
        if (action.ActionRaw.UnitCommand.UnitTags.Length > 0)
            actions.Add(action);
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
        UnitsAction(action, unit.Tag);
    }

    public void UnitsAction(Action action, ulong unit)
    {
        action.ActionRaw.UnitCommand.UnitTags = new ulong[] { unit };
        actions.Add(action);
    }

    public void UnitsAutocastAction(Abilities ability, Unit unit)
    {
        if (unit == null)
            return;
        UnitsAutocastAction(ability, unit.Tag);
    }

    public void UnitsAutocastAction(Abilities ability, ulong unit)
    {
        var action = AutoCastCommand(ability);
        action.ActionRaw.ToggleAutocast.UnitTags = new ulong[] { unit };
        actions.Add(action);
    }

    static Action AutoCastCommand(Abilities ability)
    {
        var action = new Action
        {
            ActionRaw = new ActionRaw
            {
                ToggleAutocast = new ActionRawToggleAutocast()
                {
                    AbilityId = (int)ability
                }
            }
        };
        return action;
    }

    public static Action Command(Abilities ability)
    {
        var action = new Action
        {
            ActionRaw = new ActionRaw
            {
                UnitCommand = new ActionRawUnitCommand()
                {
                    AbilityId = (int)ability
                }
            }
        };
        return action;
    }

    public static Action Command(Abilities ability, ulong target)
    {
        var action = new Action
        {
            ActionRaw = new ActionRaw
            {
                UnitCommand = new ActionRawUnitCommand
                {
                    AbilityId = (int)ability,
                    TargetUnitTag = target
                }
            }
        };
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
