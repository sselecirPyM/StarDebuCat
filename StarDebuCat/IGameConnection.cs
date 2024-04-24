using SC2APIProtocol;

namespace StarDebuCat
{
    public interface IGameConnection
    {
        public void SendMessage(Request request);
    }

    public static class IGameConnectionExt
    {
        public static void SendMessage(this IGameConnection gameConnection, RequestAction action)
        {
            gameConnection.SendMessage(new Request { Action = action });
        }
        public static void SendMessage(this IGameConnection gameConnection, RequestAvailableMaps availableMaps)
        {
            gameConnection.SendMessage(new Request { AvailableMaps = availableMaps });
        }
        public static void SendMessage(this IGameConnection gameConnection, RequestCreateGame createGame)
        {
            gameConnection.SendMessage(new Request { CreateGame = createGame });
        }
        public static void SendMessage(this IGameConnection gameConnection, RequestData data)
        {
            gameConnection.SendMessage(new Request { Data = data });
        }
        public static void SendMessage(this IGameConnection gameConnection, RequestDebug debug)
        {
            gameConnection.SendMessage(new Request { Debug = debug });
        }
        public static void SendMessage(this IGameConnection gameConnection, RequestGameInfo gameInfo)
        {
            gameConnection.SendMessage(new Request { GameInfo = gameInfo });
        }
        public static void SendMessage(this IGameConnection gameConnection, RequestJoinGame joinGame)
        {
            gameConnection.SendMessage(new Request { JoinGame = joinGame });
        }
        public static void SendMessage(this IGameConnection gameConnection, RequestLeaveGame leaveGame)
        {
            gameConnection.SendMessage(new Request { LeaveGame = leaveGame });
        }
        public static void SendMessage(this IGameConnection gameConnection, RequestMapCommand mapCommand)
        {
            gameConnection.SendMessage(new Request { MapCommand = mapCommand });
        }
        public static void SendMessage(this IGameConnection gameConnection, RequestObservation observation)
        {
            gameConnection.SendMessage(new Request { Observation = observation });
        }
        public static void SendMessage(this IGameConnection gameConnection, RequestObserverAction observerAction)
        {
            gameConnection.SendMessage(new Request { ObsAction = observerAction });
        }
        public static void SendMessage(this IGameConnection gameConnection, RequestPing ping)
        {
            gameConnection.SendMessage(new Request { Ping = ping });
        }
        public static void SendMessage(this IGameConnection gameConnection, RequestQuery query)
        {
            gameConnection.SendMessage(new Request { Query = query });
        }
        public static void SendMessage(this IGameConnection gameConnection, RequestQuickLoad quickLoad)
        {
            gameConnection.SendMessage(new Request { QuickLoad = quickLoad });
        }
        public static void SendMessage(this IGameConnection gameConnection, RequestQuickSave quickSave)
        {
            gameConnection.SendMessage(new Request { QuickSave = quickSave });
        }
        public static void SendMessage(this IGameConnection gameConnection, RequestQuit quit)
        {
            gameConnection.SendMessage(new Request { Quit = quit });
        }
        public static void SendMessage(this IGameConnection gameConnection, RequestReplayInfo replayInfo)
        {
            gameConnection.SendMessage(new Request { ReplayInfo = replayInfo });
        }
        public static void SendMessage(this IGameConnection gameConnection, RequestRestartGame restartGame)
        {
            gameConnection.SendMessage(new Request { RestartGame = restartGame });
        }
        public static void SendMessage(this IGameConnection gameConnection, RequestSaveMap saveMap)
        {
            gameConnection.SendMessage(new Request { SaveMap = saveMap });
        }
        public static void SendMessage(this IGameConnection gameConnection, RequestSaveReplay saveReplay)
        {
            gameConnection.SendMessage(new Request { SaveReplay = saveReplay });
        }
        public static void SendMessage(this IGameConnection gameConnection, RequestStartReplay startReplay)
        {
            gameConnection.SendMessage(new Request { StartReplay = startReplay });
        }
        public static void SendMessage(this IGameConnection gameConnection, RequestStep step)
        {
            gameConnection.SendMessage(new Request { Step = step });
        }
    }
}
