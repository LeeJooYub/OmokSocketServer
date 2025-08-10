using SocketServer.Packet;
using SocketServer.Managers;

namespace SocketServer.Handlers;

public abstract class HandlerBase
{   
    // 델리게이트 (네트워크 통신용)
    protected Func<string, byte[], bool> NetSendFunc;
    protected Action<ServerPacketData> DistributeFunc;

    // 메인 서버 상태 확인용
    protected Func<int> GetSessionCountFunc;


    // 매니저
    protected UserManager _userManager = null;
    protected RoomManager _roomManager = null;


    public abstract void RegisterPacketHandler(Dictionary<int, Action<ServerPacketData>> packetHandlerMap);

    public void Init(Func<string, byte[], bool> netSendFunc,
    Action<ServerPacketData> distributeFunc,
    Func<int> getSessionCountFunc,
    UserManager userManager,
    RoomManager roomManager)
    {
        NetSendFunc = netSendFunc;
        DistributeFunc = distributeFunc;
        GetSessionCountFunc = getSessionCountFunc;
        _userManager = userManager;
        _roomManager = roomManager;
    }


}