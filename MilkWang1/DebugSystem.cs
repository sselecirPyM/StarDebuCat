using MilkWangBase.Attributes;
using StarDebuCat;
using StarDebuCat.Algorithm;
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
    public GameConnection gameConnection;

    public bool enable;
    AnalysisSystem1 analysisSystem;
    BuildSystem1 buildSystem;
    TerranBot1 bot;
    MarkerSystem1 markerSystem;

    List<(Unit, string)> tagUnits = new();
    List<(Vector2, string)> tagPositions = new();

    SC2APIProtocol.Request debugRequest;
    int _debugTextCount = 0;
    int _debugSphereCount = 0;

    bool initialize = false;
    Field field = new Field();

    [XFind("CollectUnits", Alliance.Self)]
    List<Unit> myUnits;

    void Update()
    {
        if (!initialize)
        {
            initialize = true;
            Initialize1();
        }
        if (enable)
            Debug();

        tagUnits.Clear();
        tagPositions.Clear();
    }

    void Debug()
    {
        MessageCacheInit();
        _debugTextCount = 0;
        _debugSphereCount = 0;
        var draw = debugRequest.Debug.Debugs[0].Draw;
        draw.Spheres.Clear();
        draw.Texts.Clear();
        draw.Lines.Clear();
        draw.Boxes.Clear();

        foreach (var mark in markerSystem.marks)
        {
            if (mark.position != null && mark.unit == null)
            {
                tagPositions.Add((mark.position.Value, mark.name));
            }
        }

        foreach (var tagUnit in tagUnits)
        {
            var position = tagUnit.Item1.position;
            draw.Texts.Add(new SC2APIProtocol.DebugText()
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
            draw.Texts.Add(new SC2APIProtocol.DebugText()
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
        //foreach (var point in buildSystem.resourcePoints)
        //{
        //    byte height = analysisSystem.terrainHeight.Query(point);
        //    float z = height / 8.0f - 16f;
        //    draw.Spheres.Add(new SC2APIProtocol.DebugSphere()
        //    {
        //        P = new SC2APIProtocol.Point()
        //        {
        //            X = point.X,
        //            Y = point.Y,
        //            Z = z + 0.5f
        //        },
        //        R = 0.5f,
        //        Color = new SC2APIProtocol.Color() { R = (uint)(wave * 128) + 1, G = 1, B = 1 }
        //    });
        //}
        //foreach (var unit in myUnits)
        //{
        //    Vector2 point = unit.position;
        //    byte height = analysisSystem.terrainHeight.Query(point);
        //    float z = height / 8.0f - 16f;
        //    Vector2 fieldValue = field.Query(point);
        //    if (fieldValue.X != 0 || fieldValue.Y != 0)
        //        draw.Lines.Add(new SC2APIProtocol.DebugLine()
        //        {
        //            Line = new SC2APIProtocol.Line()
        //            {
        //                P0 = new SC2APIProtocol.Point()
        //                {
        //                    X = point.X,
        //                    Y = point.Y,
        //                    Z = z + 0.5f
        //                },
        //                P1 = new SC2APIProtocol.Point()
        //                {
        //                    X = point.X + fieldValue.X,
        //                    Y = point.Y + fieldValue.Y,
        //                    Z = z + 0.5f
        //                }
        //            },
        //            Color = new SC2APIProtocol.Color()
        //            {

        //            }
        //        });
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
        debugRequest.Debug.Debugs.Add(debugCommand);
    }

    void MessagePostProcess()
    {
        var debugTexts = debugRequest.Debug.Debugs[0].Draw.Texts;
        while (_debugTextCount < debugTexts.Count)
        {
            debugTexts.RemoveAt(debugTexts.Count - 1);
        }

        var debugSpheres = debugRequest.Debug.Debugs[0].Draw.Spheres;
        while (_debugSphereCount < debugSpheres.Count)
        {
            debugSpheres.RemoveAt(debugSpheres.Count - 1);
        }
    }

    void Initialize1()
    {
        Image image = new Image(analysisSystem.build);
        for (int i = 0; i < image.Data.Length; i++)
        {
            image.Data[i] |= analysisSystem.pathing.Data[i];
        }
        field.BuildNearestField(image, false);
    }
}
