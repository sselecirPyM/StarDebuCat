using StarDebuCat;
using StarDebuCat.Data;
using System.Numerics;

namespace MilkWang2.Simulation
{
    public class CommandManager
    {
        public void Command(Unit unit, Abilities ability)
        {
            action.Actions.Add(new SC2APIProtocol.Action()
            {
                ActionRaw = new SC2APIProtocol.ActionRaw()
                {
                    UnitCommand = new SC2APIProtocol.ActionRawUnitCommand()
                    {
                        AbilityId = (int)ability,
                        UnitTags = new ulong[] { unit.Tag }
                    }
                }
            });
        }
        public void Command(Unit unit, Abilities ability, Vector2 position)
        {
            action.Actions.Add(new SC2APIProtocol.Action()
            {
                ActionRaw = new SC2APIProtocol.ActionRaw()
                {
                    UnitCommand = new SC2APIProtocol.ActionRawUnitCommand()
                    {
                        AbilityId = (int)ability,
                        UnitTags = new ulong[] { unit.Tag },
                        TargetWorldSpacePos = new SC2APIProtocol.Point2D() { X = position.X, Y = position.Y },
                    }
                }
            });
        }
        public void Command(Unit unit, Abilities ability, Unit target)
        {
            action.Actions.Add(new SC2APIProtocol.Action()
            {
                ActionRaw = new SC2APIProtocol.ActionRaw()
                {
                    UnitCommand = new SC2APIProtocol.ActionRawUnitCommand()
                    {
                        AbilityId = (int)ability,
                        UnitTags = new ulong[] { unit.Tag },
                        TargetUnitTag = target.Tag,
                    }
                }
            });
        }
        SC2APIProtocol.RequestAction action = new SC2APIProtocol.RequestAction();

        public void SendCommand(IGameConnection gameConnection)
        {
            var request = new SC2APIProtocol.Request()
            {
                Action = action
            };
            action = new SC2APIProtocol.RequestAction();

            gameConnection.SendMessage(request);
        }
    }
}
