using MilkWangBase;
using MilkWangBase.Attributes;
using MilkWangP1;
using StarDebuCat;
using StarDebuCat.Data;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace MilkWang1;

public struct TagPosition
{
    public Vector2 position;
    public string name;
}
public class DebugSystem
{
    AnalysisSystem analysisSystem;
    GameConnection gameConnection;
    BuildSystem buildSystem;
    ProtossBot1 bot;
    MarkerSystem markerSystem;

    [Find("ReadyToPlay")]
    bool readyToPlay;

    public List<(Unit, string)> tagUnits = new();
    public List<(Vector2, string)> tagPositions = new();

    SC2APIProtocol.Request debugRequest;
    int _debugTextCount = 0;
    int _debugSphereCount = 0;
    void Update()
    {
        if (!readyToPlay)
            return;
        if (analysisSystem.Debugging)
            Debug();

        tagUnits.Clear();
        tagPositions.Clear();
    }

    void Debug()
    {
        MessageCacheInit();
        _debugTextCount = 0;
        _debugSphereCount = 0;
        var draw = debugRequest.Debug.Debug[0].Draw;
        draw.Spheres.Clear();
        draw.Text.Clear();
        draw.Lines.Clear();
        draw.Boxes.Clear();

        foreach (var mark in markerSystem.marks)
        {
            if (analysisSystem.Debugging && mark.position != null && mark.unit == null)
            {
                tagPositions.Add((mark.position.Value, mark.name));
            }
        }

        foreach (var tagUnit in tagUnits)
        {
            var position = tagUnit.Item1.position;
            draw.Text.Add(new SC2APIProtocol.DebugText()
            {
                Size = 10,
                Text = tagUnit.Item2,
                WorldPos = new SC2APIProtocol.Point() { X = position.X, Y = position.Y, Z = tagUnit.Item1.positionZ, }
            });
        }
        foreach (var tagPosition in tagPositions)
        {
            var position = tagPosition.Item1;
            float height = analysisSystem.terrainHeight.Query(position) / 8.0f - 16.0f;
            draw.Text.Add(new SC2APIProtocol.DebugText()
            {
                Size = 10,
                Text = tagPosition.Item2,
                WorldPos = new SC2APIProtocol.Point() { X = position.X, Y = position.Y, Z = height, }
            });
        }

        foreach (var point in analysisSystem.patioPointsMerged)
        {
            float height = analysisSystem.terrainHeight.Query(point) / 8.0f - 15.25f;
            draw.Spheres.Add(new SC2APIProtocol.DebugSphere()
            {
                P = new SC2APIProtocol.Point()
                {
                    X = point.X,
                    Y = point.Y,
                    Z = height
                },
                R = 0.5f,
                Color = new SC2APIProtocol.Color() { R = 255, G = 255, B = 255 }
            });
        }
        double wave = Math.Abs(Math.Sin(analysisSystem.GameLoop / 512.0 * Math.PI));
        //foreach (var point in analysisSystem.patioPoints)
        //{
        //    float height = analysisSystem.terrainHeight.Query(point) / 8.0f - 15.5f;

        //    draw.Spheres.Add(new SC2APIProtocol.DebugSphere()
        //    {
        //        P = new SC2APIProtocol.Point()
        //        {
        //            X = point.X,
        //            Y = point.Y,
        //            Z = height
        //        },
        //        R = 0.5f,
        //        Color = new SC2APIProtocol.Color() { R = (uint)(wave * 128) + 1, G = 1, B = 1 }
        //    });
        //}
        //foreach (var point in buildSystem.debugPositions)
        //{
        //    byte height = analysisSystem.terrainHeight.Query(point);

        //    draw.Spheres.Add(new SC2APIProtocol.DebugSphere()
        //    {
        //        P = new SC2APIProtocol.Point()
        //        {
        //            X = point.X,
        //            Y = point.Y,
        //            Z = height / 8.0f - 15.5f
        //        },
        //        R = 0.5f,
        //        Color = new SC2APIProtocol.Color() { R = (uint)(wave * 128) + 1, G = 1, B = 1 }
        //    });
        //}
        foreach (var point in bot.enemyBases)
        {
            byte height = analysisSystem.terrainHeight.Query(point);

            draw.Spheres.Add(new SC2APIProtocol.DebugSphere()
            {
                P = new SC2APIProtocol.Point()
                {
                    X = point.X,
                    Y = point.Y,
                    Z = height / 8.0f - 15.5f
                },
                R = 2.0f,
                Color = new SC2APIProtocol.Color() { R = 1, G = (uint)(wave * 128) + 1, B = 1 }
            });
        }

        gameConnection.Request(debugRequest);
    }

    void MessageCacheInit()
    {
        if (debugRequest != null)
            return;
        debugRequest = new SC2APIProtocol.Request();
        var debugCommand = new SC2APIProtocol.DebugCommand();
        var draw = new SC2APIProtocol.DebugDraw();
        debugCommand.Draw = draw;
        debugRequest.Debug = new SC2APIProtocol.RequestDebug();
        debugRequest.Debug.Debug.Add(debugCommand);
    }

    void MessagePostProcess()
    {
        var debugTexts = debugRequest.Debug.Debug[0].Draw.Text;
        while (_debugTextCount < debugTexts.Count)
        {
            debugTexts.RemoveAt(debugTexts.Count - 1);
        }

        var debugSpheres = debugRequest.Debug.Debug[0].Draw.Spheres;
        while (_debugSphereCount < debugSpheres.Count)
        {
            debugSpheres.RemoveAt(debugSpheres.Count - 1);
        }
    }
}
