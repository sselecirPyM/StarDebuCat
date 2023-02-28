﻿using SC2APIProtocol;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;

namespace StarDebuCat;

public class GameConnection : IDisposable
{
    public ClientWebSocket clientWebSocket;
    public int connectTimeout = 100000;
    public int readWriteTimeout = 120000;

    public Status status;

    int bufferLength = 1024 * 1024;
    public void Connect(string address, int port)
    {
        int maxTryCount = 60;
        int count = 0;
        while (count < maxTryCount)
        {
            try
            {
                clientWebSocket = new ClientWebSocket();
                // Disable PING control frames (https://tools.ietf.org/html/rfc6455#section-5.5.2).
                // It seems SC2 built in websocket server does not do PONG but tries to process ping as
                // request and then sends empty response to client. 
                clientWebSocket.Options.KeepAliveInterval = TimeSpan.FromDays(30);
                var adr = string.Format("ws://{0}:{1}/sc2api", address, port);
                var uri = new Uri(adr);
                CancellationTokenSource cancellationSource = new CancellationTokenSource();
                {
                    cancellationSource.CancelAfter(connectTimeout);
                }
                clientWebSocket.ConnectAsync(uri, cancellationSource.Token).Wait();
                break;

            }
            catch { Thread.Sleep(100); count++; }
        }
        if (count >= maxTryCount)
        {
            throw new Exception("The maximum number of attempts has been reached");
        }
    }
    public void LeaveGame()
    {
        SendMessage(new Request
        {
            LeaveGame = new RequestLeaveGame()
        });
    }
    public void Quit()
    {
        SendMessage(new Request
        {
            Quit = new RequestQuit()
        });
    }

    public Response QueryPlacements(IEnumerable<RequestQueryBuildingPlacement> placements)
    {
        Request requestQuery = new Request();
        requestQuery.Query = new RequestQuery();
        requestQuery.Query.Placements.AddRange(placements);
        var result = Request(requestQuery);
        return result;
    }

    public Response RequestAction(IEnumerable<SC2APIProtocol.Action> values)
    {
        var actionRequest = new Request();
        actionRequest.Action = new RequestAction();
        actionRequest.Action.Actions.AddRange(values);
        if (actionRequest.Action.Actions.Count > 0)
            return Request(actionRequest);
        return null;
    }
    public ResponseGameInfo RequestGameInfo()
    {
        var gameInfoRequest = new Request
        {
            GameInfo = new RequestGameInfo()
        };
        return Request(gameInfoRequest).GameInfo;
    }
    public ResponseData RequestGameData()
    {
        return Request(new Request
        {
            Data = new RequestData
            {
                UnitTypeId = true,
                AbilityId = true,
                BuffId = true,
                EffectId = true,
                UpgradeId = true
            }
        }).Data;
    }
    public ResponseStep RequestStep(uint step)
    {
        var stepRequest = new Request
        {
            Step = new RequestStep
            {
                Count = step
            }
        };
        return Request(stepRequest).Step;
    }
    public ResponseObservation RequestObservation()
    {
        var observationRequest = new Request
        {
            Observation = new RequestObservation()

        };
        return Request(observationRequest).Observation;
    }
    public List<ResponseQueryAvailableAbilities> RequestAvailableAbilities(IReadOnlyList<Data.Unit> units)
    {
        var query = new RequestQuery();
        query.Abilities.EnsureCapacity(units.Count);
        foreach (var unit in units)
        {
            query.Abilities.Add(new RequestQueryAvailableAbilities() { UnitTag = unit.Tag });
        }

        var request = new Request()
        {
            Query = query
        };
        return Request(request).Query.Abilities;
    }

    public Response Request(Request request)
    {
        SendMessage(request);
        return ReceiveMessage();
    }
    public void SendMessage(Request request)
    {
        var sendBuf = ArrayPool<byte>.Shared.Rent(bufferLength);
        var outStream = new MemoryStream(sendBuf);
        ProtoBuf.Serializer.Serialize(outStream, request);
        using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
        {
            cancellationSource.CancelAfter(readWriteTimeout);
            clientWebSocket.SendAsync(new ArraySegment<byte>(sendBuf, 0, (int)outStream.Position),
                WebSocketMessageType.Binary, true, cancellationSource.Token).Wait();
        }
        ArrayPool<byte>.Shared.Return(sendBuf);
    }
    public Response ReceiveMessage()
    {
        var receiveBuf = ArrayPool<byte>.Shared.Rent(bufferLength);
        var finished = false;
        var currentPosition = 0;
        while (!finished)
        {
            var left = receiveBuf.Length - currentPosition;
            if (left <= 0)
            {
                // No space left in the array, enlarge the array by doubling its size.
                var temp = new byte[receiveBuf.Length * 2];
                Array.Copy(receiveBuf, temp, receiveBuf.Length);
                receiveBuf = temp;
                left = receiveBuf.Length - currentPosition;
            }
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                cancellationSource.CancelAfter(readWriteTimeout);
                var task = clientWebSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuf, currentPosition, left), cancellationSource.Token);
                task.Wait();
                var result = task.Result;
                if (result.MessageType != WebSocketMessageType.Binary)
                    throw new Exception("Expected Binary message type.");

                currentPosition += result.Count;
                finished = result.EndOfMessage;
            }
        }
        var response = ProtoBuf.Serializer.Deserialize<Response>(new ReadOnlySpan<byte>(receiveBuf, 0, currentPosition));
        status = response.Status;
        ArrayPool<byte>.Shared.Return(receiveBuf);
        return response;
    }

    public void NewGame(PlayerSetup player1, PlayerSetup player2, string mapPath)
    {
        if (!File.Exists(mapPath))
        {
            throw new Exception("Unable to locate map: " + mapPath);
        }

        var createGame = new RequestCreateGame
        {
            Realtime = false,
            LocalMap = new LocalMap
            {
                MapPath = mapPath
            },
            PlayerSetups =
            {
                player1,
                player2
            }
        };

        var request = new Request
        {
            CreateGame = createGame,
        };
        var response = Request(request);

        if (response.CreateGame.ShouldSerializeErrorDetails())
        {
            throw new Exception(string.Format("Response error \ndetail:{0}", response.CreateGame.ErrorDetails));
        }
    }

    public void Dispose()
    {
        clientWebSocket?.Dispose();
    }
}
