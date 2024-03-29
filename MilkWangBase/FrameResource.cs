﻿using StarDebuCat.Data;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MilkWangBase;

public class FrameResource
{
    public float CollectionRateMinerals = new();
    public float CollectionRateVespene = new();
    public float CollectedMinerals = new();
    public float CollectedVespene = new();
    public float KilledMineralsArmy = new();
    public float KilledMineralsTechnology = new();
    public float KilledVespeneArmy = new();
    public float KilledVespeneTechnology = new();

    public float TotalUsedMineralsArmy = new();
    public float TotalUsedMineralsTechnology = new();
    public float TotalUsedVespeneArmy = new();
    public float TotalUsedVespeneTechnology = new();

    public float UsedMineralsArmy = new();
    public float UsedVespeneArmy = new();

    public float LostMineralsArmy = new();
    public float LostVespeneArmy = new();
    public float SpentMinerals = new();
    public float SpentVespene = new();
    public float FoodUsedArmy = new();

    public int Minerals;
    public int Vespene;
    public int ArmyCount;
    public int FoodArmy;
    public int FoodCap;
    public int FoodWorkers;
    public int FoodUsed;

    public int MineralLost;
    public int VespeneLost;
    public int TotalLost => MineralLost + VespeneLost;

    public int MineralKill;
    public int VespeneKill;
    public int TotalKill => MineralKill + VespeneKill;

    public int WarpGateCount;
    public int IdleWorkerCount;
    public int GameLoop;

    public Dictionary<UnitType, int> KillUnitCount;
    public Dictionary<UnitType, int> LostUnitCount;

    public FrameResource Clone()
    {
        return (FrameResource)MemberwiseClone();
    }

    static FieldInfo[] fieldInfos = typeof(FrameResource).GetFields();

    public static FrameResource Interpolate(FrameResource left, FrameResource right, int gameloop)
    {
        if (left.GameLoop == right.GameLoop)
        {
            return left.Clone();
        }
        float distance = right.GameLoop - left.GameLoop;
        float rate = (gameloop - left.GameLoop) / distance;
        FrameResource frameResource = new FrameResource();
        foreach (var member in fieldInfos)
        {
            if (member.FieldType == typeof(float))
            {
                float l1 = (float)member.GetValue(left);
                float l2 = (float)member.GetValue(right);
                member.SetValue(frameResource, l1 * (1 - rate) + l2 * rate);
            }
            else if (member.FieldType == typeof(int))
            {
                float l1 = (float)(int)member.GetValue(left);
                float l2 = (float)(int)member.GetValue(right);
                member.SetValue(frameResource, (int)Math.Round(l1 * (1 - rate) + l2 * rate));
            }
        }
        frameResource.GameLoop = gameloop;
        frameResource.FoodUsed = Math.Clamp(frameResource.FoodUsed, 0, 200);
        frameResource.FoodCap = Math.Clamp(frameResource.FoodCap, 0, 200);
        return frameResource;
    }
}
