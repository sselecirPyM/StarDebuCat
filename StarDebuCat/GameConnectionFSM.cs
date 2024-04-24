using SC2APIProtocol;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;

namespace StarDebuCat;

public class GameConnectionFSM : IGameConnection
{
    public ClientWebSocket clientWebSocket;

    public int connectTimeout = 100000;
    public int readWriteTimeout = 120000;

    static int bufferLength = 1024 * 1024 * 2;
    public Status status;

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
        while (messageCount > 0)
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
                OnResponseCreateGame?.Invoke(response.CreateGame);
                break;
            case Response.responseOneofCase.JoinGame:
                OnResponseJoinGame?.Invoke(response.JoinGame);
                break;
            case Response.responseOneofCase.RestartGame:
                OnResponseRestartGame?.Invoke(response.RestartGame);
                break;
            case Response.responseOneofCase.StartReplay:
                OnResponseStartReplay?.Invoke(response.StartReplay);
                break;
            case Response.responseOneofCase.LeaveGame:
                OnResponseLeaveGame?.Invoke(response.LeaveGame);
                break;
            case Response.responseOneofCase.QuickSave:
                OnResponseQuickSave?.Invoke(response.QuickSave);
                break;
            case Response.responseOneofCase.QuickLoad:
                OnResponseQuickLoad?.Invoke(response.QuickLoad);
                break;
            case Response.responseOneofCase.Quit:
                OnResponseQuit?.Invoke(response.Quit);
                break;
            case Response.responseOneofCase.GameInfo:
                OnResponseGameInfo?.Invoke(response.GameInfo);
                break;
            case Response.responseOneofCase.Observation:
                OnResponseObservation?.Invoke(response.Observation);
                break;
            case Response.responseOneofCase.Action:
                OnResponseAction?.Invoke(response.Action);
                break;
            case Response.responseOneofCase.ObsAction:
                OnResponseObserverAction?.Invoke(response.ObsAction);
                break;
            case Response.responseOneofCase.Step:
                OnResponseStep?.Invoke(response.Step);
                break;
            case Response.responseOneofCase.Data:
                OnResponseData?.Invoke(response.Data);
                break;
            case Response.responseOneofCase.Query:
                OnResponseQuery?.Invoke(response.Query);
                break;
            case Response.responseOneofCase.SaveReplay:
                OnResponseSaveReplay?.Invoke(response.SaveReplay);
                break;
            case Response.responseOneofCase.ReplayInfo:
                OnResponseReplayInfo?.Invoke(response.ReplayInfo);
                break;
            case Response.responseOneofCase.AvailableMaps:
                OnResponseAvailableMaps?.Invoke(response.AvailableMaps);
                break;
            case Response.responseOneofCase.SaveMap:
                OnResponseSaveMap?.Invoke(response.SaveMap);
                break;
            case Response.responseOneofCase.MapCommand:
                OnResponseMapCommand?.Invoke(response.MapCommand);
                break;
            case Response.responseOneofCase.Ping:
                OnResponsePing?.Invoke(response.Ping);
                break;
            case Response.responseOneofCase.Debug:
                OnResponseDebug?.Invoke(response.Debug);
                break;
        }
    }
    public event Action<ResponseCreateGame> OnResponseCreateGame;
    public event Action<ResponseJoinGame> OnResponseJoinGame;
    public event Action<ResponseRestartGame> OnResponseRestartGame;
    public event Action<ResponseStartReplay> OnResponseStartReplay;
    public event Action<ResponseLeaveGame> OnResponseLeaveGame;
    public event Action<ResponseQuickSave> OnResponseQuickSave;
    public event Action<ResponseQuickLoad> OnResponseQuickLoad;
    public event Action<ResponseQuit> OnResponseQuit;
    public event Action<ResponseGameInfo> OnResponseGameInfo;
    public event Action<ResponseObservation> OnResponseObservation;
    public event Action<ResponseAction> OnResponseAction;
    public event Action<ResponseObserverAction> OnResponseObserverAction;
    public event Action<ResponseStep> OnResponseStep;
    public event Action<ResponseData> OnResponseData;
    public event Action<ResponseQuery> OnResponseQuery;
    public event Action<ResponseSaveReplay> OnResponseSaveReplay;
    public event Action<ResponseReplayInfo> OnResponseReplayInfo;
    public event Action<ResponseAvailableMaps> OnResponseAvailableMaps;
    public event Action<ResponseSaveMap> OnResponseSaveMap;
    public event Action<ResponseMapCommand> OnResponseMapCommand;
    public event Action<ResponsePing> OnResponsePing;
    public event Action<ResponseDebug> OnResponseDebug;

    public void Dispose()
    {
        clientWebSocket?.Dispose();
    }
}
