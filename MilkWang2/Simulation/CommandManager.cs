using StarDebuCat;
using StarDebuCat.Data;
using StarDebuCat.Utility;
using System.Numerics;

namespace MilkWang2.Simulation
{
    public class CommandManager
    {
        public UnitManager unitManager;
        public GameData gameData;

        SC2APIProtocol.RequestAction action = new SC2APIProtocol.RequestAction();

        public void SendCommand(IGameConnection gameConnection)
        {
            foreach (var unit in unitManager.selfUnits)
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
                    unit.command.ability = (Abilities)gameData.GetUnitTypeData(unit.command.buildUnit).AbilityId;
                }
                if (unit.command.upgrade != UpgradeType.NONE)
                {
                    unit.command.ability = (Abilities)gameData.upgradeDatas[(int)unit.command.upgrade].AbilityId;
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

            var request = new SC2APIProtocol.Request()
            {
                Action = action
            };
            action = new SC2APIProtocol.RequestAction();

            gameConnection.SendMessage(request);
        }


        float maxOptimiseDistance = 0.2f;
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
}
