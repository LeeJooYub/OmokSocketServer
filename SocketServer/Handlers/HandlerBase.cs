using SocketServer.Packet;
using SocketServer.GameState;

namespace SocketServer.Handlers;

public abstract class HandlerBase
{
    protected MainServer _mainServer;
    protected UserManager _userManager = null;
    protected RoomManager _roomManager = null;


    public abstract void RegisterPacketHandler(Dictionary<int, Action<ServerPacketData>> packetHandlerMap);

    public void Init(MainServer mainServer, UserManager userManager, RoomManager roomManager)
    {
        _mainServer = mainServer;
        _userManager = userManager;
        _roomManager = roomManager;
    }


}