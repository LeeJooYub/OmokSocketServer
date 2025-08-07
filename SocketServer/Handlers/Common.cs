using SocketServer.Packet;
using SocketServer.Managers;
using MessagePack;

namespace SocketServer.Handlers;


public class ConnectionHandler : HandlerBase
{
    public override void RegisterPacketHandler(Dictionary<int, Action<ServerPacketData>> packetHandlerMap)
    {
        // 클라이언트가 서버에 새로 접속했을 때(세션 생성) 호출되는 핸들러 등록
        // 주로 접속 카운트 갱신, 초기화 등 공통 처리에 사용
        packetHandlerMap.Add((int) PacketId.NtfInConnectClient, HandleNotifyInConnectClient);

        // 클라이언트가 서버에서 연결을 끊었을 때(세션 해제) 호출되는 핸들러 등록
        // 유저 정보 정리, 방 퇴장 처리 등 공통 정리 작업에 필요
        packetHandlerMap.Add((int) PacketId.NtfInDisconnectClient, HandleNotifyInDisConnectClient);

        // 클라이언트가 로그인 요청을 보냈을 때 호출되는 핸들러 등록
        // 중복 로그인 방지, 유저 등록, 로그인 응답 등 인증 관련 공통 처리
        packetHandlerMap.Add((int) PacketId.ReqLogin, HandleRequestLogin);                                            
    }

    public void HandleNotifyInConnectClient(ServerPacketData requestData)
    {
        
    }

    public void HandleNotifyInDisConnectClient(ServerPacketData requestData)
    {
        var sessionID = requestData.SessionID;
        var user = _userManager.GetUser(sessionID);

        if (user != null)
        {
            var roomNum = user.RoomNumber;

            if (roomNum != PacketDef.InvalidRoomNumber)
            {
                var packet = new PKTInternalNtfRoomLeave()
                {
                    RoomNumber = roomNum,
                    UserID = user.GetUserID(),
                };

                var packetBodyData = MessagePackSerializer.Serialize(packet);
                var internalPacket = new ServerPacketData();
                internalPacket.SetPacketData(sessionID, (Int16)PacketId.NtfInRoomLeave, packetBodyData);

                DistributeFunc(internalPacket);
            }

            _userManager.RemoveUser(sessionID);
        }

        MainServer.s_MainLogger.Debug($"Current Connected Session Count: {_mainServer.SessionCount}");
    }


    public void HandleRequestLogin(ServerPacketData packetData)
    {
        var sessionID = packetData.SessionID;
        MainServer.s_MainLogger.Debug("로그인 요청 받음");

        try
        {
            if (_userManager.GetUser(sessionID) != null)
            {
                SendResponseLoginToClient(ErrorCode.LoginAlreadyWorking, packetData.SessionID);
                return;
            }

            var reqData = MessagePackSerializer.Deserialize<PKTReqLogin>(packetData.BodyData);

            var errorCode = _userManager.AddUser(reqData.UserID, sessionID);
            if (errorCode != ErrorCode.None)
            {
                SendResponseLoginToClient(errorCode, packetData.SessionID);

                if (errorCode == ErrorCode.LoginFullUserCount)
                {
                    SendNotifyMustCloseToClient(ErrorCode.LoginFullUserCount, packetData.SessionID);
                }

                return;
            }

            SendResponseLoginToClient(errorCode, packetData.SessionID);

            MainServer.s_MainLogger.Debug("로그인 요청 답변 보냄");
            MainServer.s_MainLogger.Debug($"UserID: {reqData.UserID}, SessionID: {sessionID}, ErrorCode: {errorCode}");

        }
        catch (Exception ex)
        {
            // 패킷 해제에 의해서 로그가 남지 않도록 로그 수준을 Debug로 한다.
            MainServer.s_MainLogger.Error(ex.ToString());
        }
    }

    public void SendResponseLoginToClient(ErrorCode errorCode, string sessionID)
    {
        var resLogin = new PKTResLogin()
        {
            Result = (short)errorCode
        };

        var bodyData = MessagePackSerializer.Serialize(resLogin);
        var sendData = PacketToBytes.Make(PacketId.ResLogin, bodyData);

        NetSendFunc(sessionID, sendData);
    }

    public void SendNotifyMustCloseToClient(ErrorCode errorCode, string sessionID)
    {
        var resLogin = new PKNtfMustClose()
        {
            Result = (short)errorCode
        };

        var bodyData = MessagePackSerializer.Serialize(resLogin);
        var sendData = PacketToBytes.Make(PacketId.NtfMustClose, bodyData);

        NetSendFunc(sessionID, sendData);
    }




}
