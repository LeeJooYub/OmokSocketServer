using SocketServer.Packet;
using SocketServer.Managers;

namespace SocketServer.Handlers;

public abstract class HandlerBase
{
    protected Func<string, byte[], bool> NetSendFunc;
    protected Action<ServerPacketData> DistributeFunc; 
    protected UserManager _userManager = null;
    protected RoomManager _roomManager = null;


    public abstract void RegisterPacketHandler(Dictionary<int, Action<ServerPacketData>> packetHandlerMap);

    public void Init(Func<string, byte[], bool> netSendFunc, Action<ServerPacketData> distributeFunc, UserManager userManager, RoomManager roomManager)
    {
        NetSendFunc = netSendFunc;
        DistributeFunc = distributeFunc;
        _userManager = userManager;
        _roomManager = roomManager;
    }


}