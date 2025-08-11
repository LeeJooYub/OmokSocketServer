using System.Threading.Tasks.Dataflow;
using SocketServer.Packet;
using SocketServer.Users;
using SocketServer.Room;
using SocketServer.Handlers;

namespace SocketServer.Packet;


/*
=====================================================================
PacketProcessor 클래스 구조 및 동작 흐름
=====================================================================

[전체 구조]

┌─────────────────────────────┐
│        PacketProcessor      │
│─────────────────────────────│
│ 1. 패킷 수신 (PushPacket)   │
│ 2. 패킷 버퍼(BufferBlock)   │
│ 3. 패킷 처리 스레드(Process) │
│ 4. 패킷 핸들러 맵            │
│ 5. 방/유저/핸들러 관리        │
└─────────────┬───────────────┘
───────────────────────────────────────────────────────────────────────────────
1. [PushPacket] : 외부에서 패킷이 들어오면 BufferBlock에 저장
2. [Process]    : 별도 스레드가 BufferBlock에서 패킷을 꺼내 패킷ID별 핸들러로 라우팅
3. [핸들러]     : 패킷ID에 따라 등록된 핸들러가 실제 로직 처리
───────────────────────────────────────────────────────────────────────────────

[핵심 역할]
- 패킷을 안전하게 큐잉(BufferBlock)하여 멀티스레드 환경에서 안정적으로 처리
- 패킷ID별로 핸들러를 등록/관리하여, 확장성과 유지보수성 향상
- 방(Room), 유저(User) 등 게임 상태와 연동

※ 이 클래스는 서버의 "패킷 처리 허브" 역할을 하며, 모든 네트워크 패킷의 입구/출구입니다.
================================================================================
*/

public class PacketProcessor
{
    // 패킷 처리 맵, 스레드 등 추가 예정
    bool _isThreadRunning;
    Thread _processThread = null;
    BufferBlock<ServerPacketData> _packetBuffer = new BufferBlock<ServerPacketData>();

    //패킷 라우팅을 위한 핸들러 맵
    Dictionary<int, Action<ServerPacketData>> _packetHandlerMap = new Dictionary<int, Action<ServerPacketData>>();


    //Managers
    UserManager _userManager = null;
    RoomManager _roomManager = null;


    //Handlers
    ConnectionHandler _connectionHandler = new ();
    RoomHandler _roomHandler = new ();


    // 여러 매니저 초기화
    public void Create(MainServer mainServer)
    {
        // 필요한 매니저 주입받아서 초기화
        _userManager = mainServer._userManager;
        _roomManager = mainServer._roomManager;
        
        // 핸들러 초기화
        RegisterPacketHandlers(mainServer.SendData, mainServer.Distribute, mainServer.GetSessionCount);
    }

    // 패킷 라우팅 시작
    public void Start()
    {
        _isThreadRunning = true;
        _processThread = new Thread(this.Process);
        _processThread.Start();
    }
    

    public void Destroy()
    {
        _isThreadRunning = false;
        if (_processThread != null && _processThread.IsAlive)
        {
            _processThread.Join();
        }
        _packetBuffer.Complete();
    }

    public void PushPacket(ServerPacketData packet)
    {
        if (_isThreadRunning)
        {
            _packetBuffer.Post(packet);
        }
    }

    void RegisterPacketHandlers(
        Func<string, byte[], bool> netSendFunc,
        Action<ServerPacketData> distributeFunc,
        Func<int> getSessionCountFunc)
    {
        // 핸들러 초기화
        _connectionHandler.Init(netSendFunc, distributeFunc, getSessionCountFunc, _userManager, _roomManager);
        _roomHandler.Init(netSendFunc, distributeFunc, getSessionCountFunc, _userManager, _roomManager);

        // 핸들러 등록
        _connectionHandler.RegisterPacketHandler(_packetHandlerMap);
        _roomHandler.RegisterPacketHandler(_packetHandlerMap);
    }


    // 패킷 라우팅
    public void Process()
    {
        // 패킷 처리 로직
        while (_isThreadRunning)
        {
            try
            {
                var packet = _packetBuffer.Receive(); // 패킷 버퍼에서 패킷 받기
                if (_packetHandlerMap.ContainsKey(packet.PacketID))
                {
                    // 패킷 ID에 해당하는 핸들러가 있는 경우
                    _packetHandlerMap[packet.PacketID](packet);
                }
                else
                {
                    // 핸들러가 없는 경우, 로그 출력 등 처리
                    MainServer.s_MainLogger.Warn($"No handler found for packet ID: {packet.PacketID}");
                }
            }
            catch (Exception ex)
            {
                MainServer.s_MainLogger.Error($"Packet processing error: {ex}");
            }
        }

    }
}
