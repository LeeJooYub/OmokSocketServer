using System;
using System.Collections.Generic;
using System.Threading;

using SuperSocketLite.SocketBase.Logging;
using SuperSocketLite.SocketBase;
using SuperSocketLite.SocketBase.Protocol;
using SuperSocketLite.SocketBase.Config;

using SocketServer.Managers;
using SocketServer.Handlers;
using SocketServer.Packet;

/*
 [동작 예시]
 클라이언트 → (네트워크) → MainServer → PacketProcessor.PushPacket(packet)
                                              │
                                               ▼
                                  BufferBlock<ServerPacketData>
                                               │
                                               ▼
                                  PacketProcessor.Process() 스레드
                                               │
                                               ▼
                         패킷ID별 핸들러(_packetHandlerMap)로 분기
                                               │
                                              ▼
                         실제 게임/방/유저 로직 처리 (예: 방 입장, 채팅 등)
*/

namespace SocketServer;

public class MainServer : AppServer<ClientSession, EFBinaryRequestInfo>
{
    // 메인 로거 인스턴스입니다.
    public static ILog s_MainLogger;


    // 서버 옵션 
    public static ServerOption s_ServerOption; // 서버 설정 참고용
    public IServerConfig _config; // supersocket setup 초기화용


    // 패킷 처리기 (메인 컴포넌트)
    public PacketProcessor _mainPacketProcessor;


    // 매니저들
    public RoomManager _roomManager = new RoomManager();
    public UserManager _userManager = new UserManager();


    // MainServer 클래스의 새 인스턴스를 초기화합니다.
    public MainServer()
        : base(new DefaultReceiveFilterFactory<ReceiveFilter, EFBinaryRequestInfo>())
    {
        NewSessionConnected += OnConnected;
        SessionClosed += OnClosed;
        NewRequestReceived += OnPacketReceived;
    }


    // 서버 설정을 초기화합니다.
    public void InitConfig(ServerOption option)
    {
        s_ServerOption = option;

        _config = new ServerConfig
        {
            Name = option.Name,
            Port = option.Port,
            Ip = "Any",
            MaxConnectionNumber = option.MaxConnectionNumber,
            MaxRequestLength = option.MaxRequestLength,
        };
    }

    // 서버를 생성합니다.
    public void CreateServer()
    {
        try
        {
            // 순번을 지켜야 합니다.
            InitLogger();
            InitManagers();
            InitComponent();
            s_MainLogger.Info($"[{DateTime.Now}] 서버 생성 성공");
        }
        catch (Exception ex)
        {
            s_MainLogger.Error($"서버 생성 실패: {ex}");
        }
    }

    public void InitLogger()
    {
        bool isResult = Setup(new RootConfig(),
        _config,
        logFactory: new ConsoleLogFactory());

        if (isResult == false)
        {
            Console.WriteLine("[ERROR] 서버 네트워크 설정 실패 ㅠㅠ");
            return;
        }
        s_MainLogger = Logger;
    }


    public void InitManagers()
    {
        Room.NetSendFunc = this.SendData;
        _roomManager.CreateRooms();

        var maxUserCount = s_ServerOption.RoomMaxCount * s_ServerOption.RoomMaxUserCount;
        _userManager.Init(maxUserCount);
    }

    // 주요 컴포넌트들  생성
    public ErrorCode InitComponent()
    {
        _mainPacketProcessor = new PacketProcessor();
        _mainPacketProcessor.Create(this);
        _mainPacketProcessor.Start();


        s_MainLogger.Info("주요 객체 생성 완료");
        return ErrorCode.None;
    }

    // 네트워크로 패킷을 보낸다
    public bool SendData(string sessionID, byte[] sendData)
    {
        var session = GetSessionByID(sessionID); // 세션 ID로 클라이언트 세션 조회
        try
        {
            s_MainLogger.Debug($"SendData HEX: {BitConverter.ToString(sendData)}");
            if (session == null)
            {
                s_MainLogger.Error($"세션 번호 {sessionID} 전송 실패: 세션이 존재하지 않습니다.");
                return false; // 세션이 없으면 실패 반환
            }

            session.Send(sendData, 0, sendData.Length); // 데이터 전송
            s_MainLogger.Debug($"세션 번호 {sessionID} 전송 성공: {sendData.Length} bytes");
        }
        catch (Exception ex)
        {
            // 전송 중 예외 처리
            MainServer.s_MainLogger.Error($"{ex.ToString()},  {ex.StackTrace}");
            MainServer.s_MainLogger.Debug($"세션 번호 {sessionID} 전송 실패: {ex.Message}");
            session.SendEndWhenSendingTimeOut(); // 전송 타임아웃 처리
            session.Close(); // 세션 종료
        }
        return true; // 성공 반환
    }


    // 패킷처리기로 패킷을 전달한다 (버퍼로 전달)
    public void Distribute(ServerPacketData requestPacket)
    {
        _mainPacketProcessor.PushPacket(requestPacket); // 패킷 처리기에 패킷 삽입
    }


    // 클라이언트 연결 이벤트 처리
    void OnConnected(ClientSession session)
    {
        // 옵션의 최대 연결 수를 초과한 경우, 이 OnConnected 메서드는 호출되지 않습니다.
        s_MainLogger.Info(string.Format("OnConnected 세션 번호 {0} 접속", session.SessionID));

        var packet = ServerPacketData.MakeNTFInConnectOrDisConnectClientPacket(true, session.SessionID);
        Distribute(packet); // 연결 알림 패킷 분배
    }

    // 클라이언트 연결 해제 이벤트 처리
    void OnClosed(ClientSession session, CloseReason reason)
    {
        s_MainLogger.Info($"세션 번호 {session.SessionID} 접속해제: {reason.ToString()}");

        var packet = ServerPacketData.MakeNTFInConnectOrDisConnectClientPacket(false, session.SessionID);
        Distribute(packet); // 연결 해제 알림 패킷 분배
    }

    // 클라이언트로부터 패킷 수신 이벤트 처리
    void OnPacketReceived(ClientSession session, EFBinaryRequestInfo reqInfo)
    {
        s_MainLogger.Debug($"세션 번호 {session.SessionID} 받은 데이터 크기: {reqInfo.Body.Length}, ThreadId: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

        var packet = new ServerPacketData();
        packet.SessionID = session.SessionID; // 세션 ID 설정
        packet.PacketSize = reqInfo.TotalSize; // 패킷 크기 설정
        packet.PacketID = reqInfo.PacketID; // 패킷 ID 설정
        packet.Dummy = reqInfo.Dummy; // 패킷 더미 설정
        packet.BodyData = reqInfo.Body; // 패킷 본문 데이터 설정

        Distribute(packet); // 패킷 분배
    }

    public int GetSessionCount()
    {
        var count = this.SessionCount;
        return count;
    }

    public void StopServer()
    {
        _mainPacketProcessor.Destroy();
        Stop();
    }
    


}


public class ClientSession : AppSession<ClientSession, EFBinaryRequestInfo>
{
}

public class SessionManager
{
    public string SessionID { get; set; }



}
