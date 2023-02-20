using System.Collections.Generic;
using System.Numerics;

namespace StarDebuCat.Data;

public class Unit
{
    public int owner;
    public ulong Tag;
    public UnitType type;
    public Alliance alliance;
    public float health;
    public float healthMax;
    public float shield;
    public float shieldMax;
    public float energy;
    public float energyMax;
    public Vector2 position;
    public float positionZ;
    public float weaponCooldown;
    public float radius;
    public float buildProgress;
    public bool isCloaked;
    public bool isPowered;
    public bool isFlying;
    public bool isBurrowed;
    public float lastTrackTime;
    public List<int> passangers;
    public int cargoSpaceTaken;
    public int cargoSpaceMax;
    public int passagerGuess;
    public bool tracking;
    public ulong lastTrackFrame;
    public int assignedHarvesters;
    public int idealHarvesters;
    public int mineralContents;
    public int vespeneContents;
    public ulong addOnTag;
    public ulong engagedTargetTag;
    public float radarRange;
    public bool isBlip;

    public bool fired;
    public List<SC2APIProtocol.UnitOrder> orders = new();

    public void UpdateBy(SC2APIProtocol.Unit unit)
    {
        owner = unit.Owner;
        Tag = unit.Tag;
        type = (UnitType)unit.UnitType;
        alliance = (Alliance)unit.Alliance;
        health = unit.Health;
        healthMax = unit.HealthMax;
        shield = unit.Shield;
        shieldMax = unit.ShieldMax;
        energy = unit.Energy;
        energyMax = unit.EnergyMax;
        position = new Vector2(unit.Pos.X, unit.Pos.Y);
        positionZ = unit.Pos.Z;

        fired = weaponCooldown < unit.WeaponCooldown;
        weaponCooldown = unit.WeaponCooldown;
        radius = unit.Radius;
        buildProgress = unit.BuildProgress;
        isCloaked = unit.Cloak == SC2APIProtocol.CloakState.Cloaked;
        isPowered = unit.IsPowered;
        isFlying = unit.IsFlying;
        isBurrowed = unit.IsBurrowed;
        cargoSpaceTaken = unit.CargoSpaceTaken;
        cargoSpaceMax = unit.CargoSpaceMax;
        assignedHarvesters = unit.AssignedHarvesters;
        idealHarvesters = unit.IdealHarvesters;
        orders.Clear();
        orders.AddRange(unit.Orders);
        addOnTag = unit.AddOnTag;
        engagedTargetTag = unit.EngagedTargetTag;
        mineralContents = unit.MineralContents;
        vespeneContents = unit.VespeneContents;
        radarRange = unit.RadarRange;
        isBlip = unit.IsBlip;
        //tracking = true;
    }

    public bool TryGetOrder(out SC2APIProtocol.UnitOrder order)
    {
        if (orders.Count > 0)
        {
            order = orders[0];
            return true;
        }
        else
        {
            order = null;
            return false;
        }
    }

    public override string ToString()
    {
        return string.Format("{0}|{1}", type, owner);
    }
}
