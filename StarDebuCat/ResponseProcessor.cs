using SC2APIProtocol;

namespace StarDebuCat;

public abstract class ResponseProcessor
{
    public IGameConnection GameConnection { get; set; }
    protected void SendMessage(Request request)
    {
        GameConnection.SendMessage(request);
    }
    public abstract void StartGame(RequestJoinGame baseRequest);

    public virtual void OnResponseCreateGame(ResponseCreateGame responseCreateGame)
    {

    }
    public virtual void OnResponseJoinGame(ResponseJoinGame responseJoinGame)
    {

    }
    public virtual void OnResponseRestartGame(ResponseRestartGame responseRestartGame)
    {

    }
    public virtual void OnResponseStartReplay(ResponseStartReplay responseStartReplay)
    {

    }
    public virtual void OnResponseLeaveGame(ResponseLeaveGame responseLeaveGame)
    {

    }
    public virtual void OnResponseQuickSave(ResponseQuickSave responseQuickSave)
    {

    }
    public virtual void OnResponseQuickLoad(ResponseQuickLoad responseQuickLoad)
    {

    }
    public virtual void OnResponseQuit(ResponseQuit responseQuit)
    {

    }
    public virtual void OnResponseGameInfo(ResponseGameInfo responseGameInfo)
    {

    }

    public virtual void OnResponseObservation(ResponseObservation responseObservation)
    {

    }
    public virtual void OnResponseAction(ResponseAction responseAction)
    {

    }
    public virtual void OnResponseObserverAction(ResponseObserverAction responseObserverAction)
    {

    }
    public virtual void OnResponseStep(ResponseStep responseStep)
    {

    }
    public virtual void OnResponseData(ResponseData responseData)
    {

    }
    public virtual void OnResponseQuery(ResponseQuery responseQuery)
    {

    }
    public virtual void OnResponseSaveReplay(ResponseSaveReplay responseSaveReplay)
    {

    }
    public virtual void OnResponseReplayInfo(ResponseReplayInfo responseReplayInfo)
    {

    }
    public virtual void OnResponseAvailableMaps(ResponseAvailableMaps responseAvailableMaps)
    {

    }
    public virtual void OnResponseSaveMap(ResponseSaveMap responseSaveMap)
    {

    }
    public virtual void OnResponseMapCommand(ResponseMapCommand responseMapCommand)
    {

    }
    public virtual void OnResponsePing(ResponsePing responsePing)
    {

    }
    public virtual void OnResponseDebug(ResponseDebug responseDebug)
    {

    }
}
