using SC2APIProtocol;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;

namespace StarDebuCat;

public class GameConnectionFSM2 : IGameConnection
{
    public ClientWebSocket clientWebSocket;

    public int connectTimeout = 100000;
    public int readWriteTimeout = 120000;

    static int bufferLength = 1024 * 1024 * 2;
    public Status status;

    public ResponseProcessor responseProcessor;

    public int messageCount = 0;

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


    byte[] receiveBuffer = new byte[bufferLength];
    public void SendMessage(Request request)
    {
        var outStream = new MemoryStream(receiveBuffer);
        ProtoBuf.Serializer.Serialize(outStream, request);
        using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
        {
            cancellationSource.CancelAfter(readWriteTimeout);
            clientWebSocket.SendAsync(new ArraySegment<byte>(receiveBuffer, 0, (int)outStream.Position),
                WebSocketMessageType.Binary, true, cancellationSource.Token).Wait();
        }
        messageCount++;
    }

    byte[] sendBuffer = new byte[bufferLength];
    Response ReceiveMessage()
    {
        var finished = false;
        var currentPosition = 0;
        while (!finished)
        {
            var left = sendBuffer.Length - currentPosition;
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                cancellationSource.CancelAfter(readWriteTimeout);
                var task = clientWebSocket.ReceiveAsync(new ArraySegment<byte>(sendBuffer, currentPosition, left), cancellationSource.Token);
                task.Wait();
                var result = task.Result;
                if (result.MessageType != WebSocketMessageType.Binary)
                    throw new Exception("Expected Binary message type.");

                currentPosition += result.Count;
                finished = result.EndOfMessage;
            }
        }
        var response = ProtoBuf.Serializer.Deserialize<Response>(new ReadOnlySpan<byte>(sendBuffer, 0, currentPosition));
        status = response.Status;
        messageCount--;
        return response;
    }

    public void FSM()
    {
        while(messageCount > 0)
        {
            Process();
        }
    }

    void Process()
    {
        var response = ReceiveMessage();
        switch (response.responseCase)
        {
            case Response.responseOneofCase.CreateGame:
                responseProcessor.OnResponseCreateGame(response.CreateGame);
                break;
            case Response.responseOneofCase.JoinGame:
                responseProcessor.OnResponseJoinGame(response.JoinGame);
                break;
            case Response.responseOneofCase.RestartGame:
                responseProcessor.OnResponseRestartGame(response.RestartGame);
                break;
            case Response.responseOneofCase.StartReplay:
                responseProcessor.OnResponseStartReplay(response.StartReplay);
                break;
            case Response.responseOneofCase.LeaveGame:
                responseProcessor.OnResponseLeaveGame(response.LeaveGame);
                break;
            case Response.responseOneofCase.QuickSave:
                responseProcessor.OnResponseQuickSave(response.QuickSave);
                break;
            case Response.responseOneofCase.QuickLoad:
                responseProcessor.OnResponseQuickLoad(response.QuickLoad);
                break;
            case Response.responseOneofCase.Quit:
                responseProcessor.OnResponseQuit(response.Quit);
                break;
            case Response.responseOneofCase.GameInfo:
                responseProcessor.OnResponseGameInfo(response.GameInfo);
                break;
            case Response.responseOneofCase.Observation:
                responseProcessor.OnResponseObservation(response.Observation);
                break;
            case Response.responseOneofCase.Action:
                responseProcessor.OnResponseAction(response.Action);
                break;
            case Response.responseOneofCase.ObsAction:
                responseProcessor.OnResponseObserverAction(response.ObsAction);
                break;
            case Response.responseOneofCase.Step:
                responseProcessor.OnResponseStep(response.Step);
                break;
            case Response.responseOneofCase.Data:
                responseProcessor.OnResponseData(response.Data);
                break;
            case Response.responseOneofCase.Query:
                responseProcessor.OnResponseQuery(response.Query);
                break;
            case Response.responseOneofCase.SaveReplay:
                responseProcessor.OnResponseSaveReplay(response.SaveReplay);
                break;
            case Response.responseOneofCase.ReplayInfo:
                responseProcessor.OnResponseReplayInfo(response.ReplayInfo);
                break;
            case Response.responseOneofCase.AvailableMaps:
                responseProcessor.OnResponseAvailableMaps(response.AvailableMaps);
                break;
            case Response.responseOneofCase.SaveMap:
                responseProcessor.OnResponseSaveMap(response.SaveMap);
                break;
            case Response.responseOneofCase.MapCommand:
                responseProcessor.OnResponseMapCommand(response.MapCommand);
                break;
            case Response.responseOneofCase.Ping:
                responseProcessor.OnResponsePing(response.Ping);
                break;
            case Response.responseOneofCase.Debug:
                responseProcessor.OnResponseDebug(response.Debug);
                break;
        }
    }

    public void Dispose()
    {
        clientWebSocket?.Dispose();
    }
}
