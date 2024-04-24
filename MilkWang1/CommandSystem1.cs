using StarDebuCat;
using StarDebuCat.Data;
using StarDebuCat.Utility;
using System.Collections.Generic;
using System.Numerics;

namespace MilkWang1;

public class CommandSystem1
{
    public GameData GameData;
    public GameConnectionFSM gameConnection;
    public AnalysisSystem1 analysisSystem;

    public List<SC2APIProtocol.ActionChat> actionChats = new List<SC2APIProtocol.ActionChat>();

    public void Update()
    {
        var action = new SC2APIProtocol.RequestAction();
        foreach (var chat in actionChats)
        {
            action.Actions.Add(new SC2APIProtocol.Action()
            {
                ActionChat = chat,
            });
        }
        actionChats.Clear();
        foreach (var unit in analysisSystem.units)
        {
            foreach (var toggle in unit.toggleAutoCast)
            {
                action.Actions.Add(new SC2APIProtocol.Action()
                {
                    ActionRaw = new SC2APIProtocol.ActionRaw()
                    {
                        ToggleAutocast = new SC2APIProtocol.ActionRawToggleAutocast()
                        {
                            AbilityId = (int)toggle,
                            UnitTags = new ulong[] { unit.Tag },
                        }
                    }
                });
            }
            unit.toggleAutoCast.Clear();
            if (unit.command == null)
                continue;


            if (unit.command.buildUnit != UnitType.INVALID)
            {
                unit.command.ability = (Abilities)GameData.GetUnitTypeData(unit.command.buildUnit).AbilityId;
            }
            if (unit.command.upgrade != UpgradeType.NONE)
            {
                unit.command.ability = (Abilities)GameData.upgradeDatas[(int)unit.command.upgrade].AbilityId;
            }

            if (!OptimiseUnitCommand(unit))
            {
                var unitCommand = new SC2APIProtocol.ActionRawUnitCommand()
                {
                    AbilityId = (int)unit.command.ability,
                    UnitTags = new ulong[] { unit.Tag },
                };
                if (unit.command.targetUnit.HasValue)
                    unitCommand.TargetUnitTag = unit.command.targetUnit.Value;
                if (unit.command.targetPosition.HasValue)
                    unitCommand.TargetWorldSpacePos = unit.command.targetPosition.Value.ToPoint2D();
                action.Actions.Add(new SC2APIProtocol.Action()
                {
                    ActionRaw = new SC2APIProtocol.ActionRaw()
                    {
                        UnitCommand = unitCommand
                    }
                });
            }
            unit.command = null;
        }

        gameConnection.SendMessage(action);
    }

    float maxOptimiseDistance = 0.2f;

    public void EnqueueChat(string message, bool broadcast) => actionChats.Add(new SC2APIProtocol.ActionChat()
    {
        Message = message,
        channel = broadcast ? SC2APIProtocol.ActionChat.Channel.Broadcast : SC2APIProtocol.ActionChat.Channel.Team
    });

    bool OptimiseUnitCommand(Unit unit)
    {
        foreach (var order in unit.orders)
        {
            var ability = unit.command.ability;
            var currentAbility = (Abilities)order.AbilityId;
            switch (currentAbility)
            {
                case Abilities.ATTACK_ATTACK:
                    currentAbility = Abilities.ATTACK;
                    break;
            }
            switch (ability)
            {
                case Abilities.ATTACK_ATTACK:
                    ability = Abilities.ATTACK;
                    break;
            }
            if (order.TargetCase == SC2APIProtocol.UnitOrder.TargetOneofCase.TargetUnitTag &&
                unit.command.targetUnit.HasValue)
            {
                if (currentAbility == ability && order.TargetUnitTag == unit.command.targetUnit.Value)
                {
                    return true;
                }
            }
            else if (order.TargetCase == SC2APIProtocol.UnitOrder.TargetOneofCase.TargetWorldSpacePos &&
                unit.command.targetPosition.HasValue)
            {
                if (currentAbility == ability)
                {
                    var pos = order.TargetWorldSpacePos.ToVector2();
                    if (Vector2.DistanceSquared(unit.command.targetPosition.Value, pos) < maxOptimiseDistance)
                    {
                        return true;
                    }
                }
            }
            else if (order.TargetCase == SC2APIProtocol.UnitOrder.TargetOneofCase.None &&
                !unit.command.targetPosition.HasValue &&
                !unit.command.targetUnit.HasValue)
            {
                if (currentAbility == ability)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
