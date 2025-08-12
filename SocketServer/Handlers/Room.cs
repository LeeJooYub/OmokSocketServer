
using SocketServer.Room;
using SocketServer.Packet;
using MessagePack;

namespace SocketServer.Handlers;

/// <summary>
/// 방 관련 요청을 처리하는 핸들러 클래스
/// </summary>
public class RoomHandler : HandlerBase
{
    public override void RegisterPacketHandler(Dictionary<int, Action<ServerPacketData>> packetHandlerMap)
    {
        // 방 관련 패킷 핸들러 등록
        // 방 입장, 퇴장 요청 핸들러 등록
        packetHandlerMap.Add((int)PacketId.ReqRoomLeave, HandleRequestLeave);

        // 매치 메이킹 요청 및 응답 핸들러 등록
        packetHandlerMap.Add((int)PacketId.REQ_MATCH_MAKE, HandleRequestMatchMake);

        // 착수 요청 핸들러
        packetHandlerMap.Add((int)PacketId.REQ_PUT_STONE, HandleRequestMoveStone);

        // 방 퇴장 알림 핸들러 (서버 내부에서 사용)
        packetHandlerMap.Add((int)PacketId.NtfInRoomLeave, HandleNotifyLeaveInternal);
    }


    // ------------------------------외부 요청 핸들러--------------------------------

    /// <summary>
    /// 착수 요청 핸들러 - 오목판에 돌을 놓는 요청을 처리
    /// </summary>
    public void HandleRequestMoveStone(ServerPacketData packetData)
    {
        MainServer.s_MainLogger.Debug("RoomHandler -> HandleRequestMoveStone 핸들러 호출");
        var sessionID = packetData.SessionID;
        
        // 유저 유효성 검증
        if (!ValidateUserForGameAction(sessionID, out var user, out var room, out var playerColor))
        {
            return;
        }

        try
        {
            // 착수 처리
            var req = MessagePackSerializer.Deserialize<PKTReqPutStone>(packetData.BodyData);
            MainServer.s_MainLogger.Debug($"HandleRequestMoveStone: {sessionID}, X: {req.X}, Y: {req.Y}, PlayerColor: {playerColor}");
            
            // 플레이어 이동 처리 및 결과 확인
            var result = room!.PlayerMove(req.X, req.Y, playerColor);
            MainServer.s_MainLogger.Debug($"HandleRequestMoveStone - PlayerMove 결과: {result}");

            // 게임 결과 처리
            ProcessGameMoveResult(room, req.X, req.Y, result);
        }
        catch (Exception ex)
        {
            MainServer.s_MainLogger.Error($"착수 처리 중 오류 발생: {ex}");
            SendErrorResponse(ErrorCode.RoomEnterErrorSystem, sessionID);
        }
    }

    /// <summary>
    /// 매치메이킹 요청 핸들러 - 다른 플레이어와의 매칭을 요청
    /// </summary>
    public void HandleRequestMatchMake(ServerPacketData packetData)
    {
        var sessionID = packetData.SessionID;
        var user = _userManager.GetUser(sessionID);
        MainServer.s_MainLogger.Debug($"RoomHandler -> HandleRequestMatchMake 핸들러 호출: {sessionID}");
        
        // 유저 검증
        if (!ValidateUserForMatchMaking(sessionID, user))
        {
            return;
        }

        // 대기 큐에 추가
        _roomManager.AddToMatchWaitingQueue(sessionID, user.GetUserID());

        // 매치메이킹 진행 (두 명 이상일 때)
        if (_roomManager.IsMatchWaitingQueueReady())
        {
            ProcessMatchMaking();
        }
    }

    /// <summary>
    /// 방에서 나가겠다는 요청을 처리
    /// </summary>
    public void HandleRequestLeave(ServerPacketData packetData)
    {
        var sessionID = packetData.SessionID;
        MainServer.s_MainLogger.Debug("Room Handler -> HandleRequestLeave 핸들러 호출: " + sessionID);

        try
        {
            var user = _userManager.GetUser(sessionID);
            if (user == null)
            {
                MainServer.s_MainLogger.Debug("Room RequestLeave - Invalid user");
                return;
            }

            MainServer.s_MainLogger.Debug($"LeaveRoomUser. SessionID:{sessionID}, RoomNumber:{user.RoomNumber}");
            
            // 방에서 유저 제거
            string userID;
            if (!_roomManager.LeaveRoomUser(sessionID, user.RoomNumber, out userID))
            {
                MainServer.s_MainLogger.Debug("Room RequestLeave - Fail to leave room");
                return;
            }

            // 유저 상태 업데이트
            user.LeaveRoom();

            // 클라이언트에 방 퇴장 응답 전송
            SendResponseLeaveRoomToClient(sessionID);

            MainServer.s_MainLogger.Debug("Room RequestLeave - Success");
        }
        catch (Exception ex)
        {
            MainServer.s_MainLogger.Debug("Room RequestLeave - Error: ");
            MainServer.s_MainLogger.Error(ex.ToString());
        }
    }


    // -------------------------- 서버 내부 핸들러 -----------------------------

    /// <summary>
    /// Connection Handler에서 방 퇴장 알림을 받았을 때 호출되는 핸들러 (서버 내부 사용)
    /// </summary>
    public void HandleNotifyLeaveInternal(ServerPacketData packetData)
    {
        var sessionID = packetData.SessionID;
        MainServer.s_MainLogger.Debug($"NotifyLeaveInternal. SessionID: {sessionID}");

        var reqData = MessagePackSerializer.Deserialize<PKTInternalNtfRoomLeave>(packetData.BodyData);
        string userID;
        _roomManager.LeaveRoomUser(sessionID, reqData.RoomNumber, out userID);
    }


    // -------------------------- 유효성 검사 및 기능 메소드 -----------------------------
    
    /// <summary>
    /// 게임 액션(착수)을 위한 유저 유효성 검증
    /// </summary>
    private bool ValidateUserForGameAction(string sessionID, out Users.User? user, out Room.Room? room, out int playerColor)
    {
        user = null;
        room = null;
        playerColor = 0;
        
        // 유저 확인
        user = _userManager.GetUser(sessionID);
        if (user == null || user.IsConfirm(sessionID) == false)
        {
            MainServer.s_MainLogger.Debug($"ValidateUserForGameAction - Invalid user or state. SessionID: {sessionID}");
            SendErrorResponse(ErrorCode.RoomEnterInvalidUser, sessionID);
            return false;
        }

        // 유저가 방에 있는지 확인
        if (user.IsStateRoom() == false)
        {
            MainServer.s_MainLogger.Debug($"ValidateUserForGameAction - User not in room. SessionID: {sessionID}");
            SendErrorResponse(ErrorCode.RoomEnterInvalidState, sessionID);
            return false;
        }

        // 방 확인
        room = _roomManager.GetRoom(user.GetRoomNumber());
        if (room == null)
        {
            MainServer.s_MainLogger.Debug($"ValidateUserForGameAction - Invalid room. SessionID: {sessionID}, RoomNumber: {user.GetRoomNumber()}");
            SendErrorResponse(ErrorCode.RoomEnterInvalidState, sessionID);
            return false;
        }

        // 플레이어 색상 확인
        playerColor = room.GetPlayerColor(user.GetUserID());
        if (playerColor == 0)
        {
            MainServer.s_MainLogger.Debug($"ValidateUserForGameAction - Invalid player color. SessionID: {sessionID}");
            SendErrorResponse(ErrorCode.RoomEnterInvalidState, sessionID);
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 매치메이킹을 위한 유저 유효성 검증
    /// </summary>
    private bool ValidateUserForMatchMaking(string sessionID, Users.User? user)
    {
        if (user == null || user.IsConfirm(sessionID) == false)
        {
            MainServer.s_MainLogger.Debug($"HandleRequestMatchMake - Invalid user. SessionID: {sessionID}");
            SendErrorResponse(ErrorCode.RoomEnterInvalidUser, sessionID);
            return false;
        }
        
        if (user.IsStateRoom())
        {
            MainServer.s_MainLogger.Debug($"HandleRequestMatchMake - User already in room. SessionID: {sessionID}");
            SendErrorResponse(ErrorCode.RoomEnterInvalidState, sessionID);
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 게임 이동(착수) 결과 처리
    /// </summary>
    private void ProcessGameMoveResult(Room.Room? room, int x, int y, int result)
    {
        if (room == null) return;
        
        // result: 0=게임 계속, 1=백 승리, 2=흑 승리
        if (result == 0)
        {
            // 성공적으로 착수한 경우, 모든 유저에게 알림
            room.SendNotifyPlayerMove(x, y, false, ' '); // 게임 종료 아님, 승리자 없음
        }
        else if (result == 1 || result == 2) // 백이 이겼거나 흑이 이긴 경우
        {
            var winnerColor = result == 1 ? 'W' : 'B';
            room.SendNotifyPlayerMove(x, y, true, winnerColor);
            MainServer.s_MainLogger.Debug($"게임 종료: {winnerColor} 승리");
            room.InitializeGameStatus();
        }
    }
    
    /// <summary>
    /// 매치메이킹 처리
    /// </summary>
    private void ProcessMatchMaking()
    {
        try
        {
            MainServer.s_MainLogger.Debug("HandleRequestMatchMake - 매칭 완료");
            var queue = _roomManager.GetMatchWaitingQueue();
            
            // 랜덤으로 색깔 배분 (필요시)
            var playerA = queue[0];
            var playerB = queue[1];

            // 매치메이킹 전에 두 유저가 여전히 유효한지 확인
            if (_userManager.GetUser(playerA.sessionID) == null || _userManager.GetUser(playerB.sessionID) == null)
            {
                // 유효하지 않은 유저 제거 후 대기 큐 정리
                _roomManager.RemoveInvalidUsersFromQueue(_userManager);
                return;
            }

            // 색상 설정 (백과 흑)
            var colorA = 'W';
            var colorB = 'B';

            // 방 할당 및 유저 추가
            var room = _roomManager.GetEmptyRoom();
            if (room == null)
            {
                MainServer.s_MainLogger.Error("매치메이킹 실패: 빈 방이 없습니다.");
                _roomManager.ClearMatchWaitingQueue();
                return;
            }
            
            room.AddUser(playerA.userID, playerA.sessionID);
            room.AddUser(playerB.userID, playerB.sessionID);
            
            // 유저 상태 업데이트
            var userA = _userManager.GetUser(playerA.sessionID);
            var userB = _userManager.GetUser(playerB.sessionID);
            if (userA != null) userA.EnteredRoom(room.Number);
            if (userB != null) userB.EnteredRoom(room.Number);

            // 매치메이킹 응답 전송
            SendMatchMakingResponse(playerA.sessionID, playerB.sessionID, colorA, colorB);
            
            // 게임 시작
            room.StartGame();

            // 대기 큐 초기화
            _roomManager.ClearMatchWaitingQueue();
        }
        catch (Exception ex)
        {
            MainServer.s_MainLogger.Error($"매치 메이킹 처리 중 오류 발생: {ex}");
            _roomManager.ClearMatchWaitingQueue();
        }
    }
    
    /// <summary>
    /// 매치메이킹 응답 전송
    /// </summary>
    private void SendMatchMakingResponse(string sessionIdA, string sessionIdB, char colorA, char colorB)
    {
        // 응답 패킷 생성
        var resA = new PKTResMatchMake
        {
            Result = (short)ErrorCode.None,
            Color = colorA
        };
        var resB = new PKTResMatchMake
        {
            Result = (short)ErrorCode.None,
            Color = colorB
        };

        var bodyA = MessagePackSerializer.Serialize(resA);
        var bodyB = MessagePackSerializer.Serialize(resB);

        var sendA = PacketToBytes.Make(PacketId.RES_MATCH_MAKE, bodyA);
        var sendB = PacketToBytes.Make(PacketId.RES_MATCH_MAKE, bodyB);

        // 각각 응답 전송
        NetSendFunc(sessionIdA, sendA);
        NetSendFunc(sessionIdB, sendB);
    }

    /// <summary>
    /// 방 퇴장 응답을 클라이언트에 전송
    /// </summary>
    void SendResponseLeaveRoomToClient(string sessionID)
    {
        var resRoomLeave = new PKTResRoomLeave()
        {
            Result = (short)ErrorCode.None
        };

        var bodyData = MessagePackSerializer.Serialize(resRoomLeave);
        var sendData = PacketToBytes.Make(PacketId.ResRoomLeave, bodyData);

        NetSendFunc(sessionID, sendData);
    }

    /// <summary>
    /// 오류 응답을 클라이언트에 전송
    /// </summary>
    void SendErrorResponse(ErrorCode errorCode, string sessionID)
    {
        var resRoomEnter = new PKTResRoomEnter()
        {
            Result = (short)errorCode
        };

        var bodyData = MessagePackSerializer.Serialize(resRoomEnter);
        var sendData = PacketToBytes.Make(PacketId.ResRoomEnter, bodyData);

        NetSendFunc(sessionID, sendData);
    }
    

}
    // TODO: 아래는 채팅 기능 추가시 주석 해체할 것입니다.
    // 방 입장 요청 처리
    // public void HandleRequestRoomEnter(ServerPacketData packetData)
    // {
    //     var sessionID = packetData.SessionID;
    //     MainServer.s_MainLogger.Debug("RequestRoomEnter: " + sessionID);

    //     try
    //     {
    //         var user = _userManager.GetUser(sessionID);
    //         if (user == null || user.IsConfirm(sessionID) == false)
    //         {
    //             SendResponseEnterRoomToClient(ErrorCode.RoomEnterInvalidUser, sessionID);
    //             MainServer.s_MainLogger.Debug("RequestEnterInternal - Invalid user");
    //             return;
    //         }

    //         if (user.IsStateRoom())
    //         {
    //             SendResponseEnterRoomToClient(ErrorCode.RoomEnterInvalidState, sessionID);
    //             MainServer.s_MainLogger.Debug("RequestEnterInternal - Invalid state");
    //             return;
    //         }

    //         // body 꺼내기
    //         var reqData = MessagePackSerializer.Deserialize<PKTReqRoomEnter>(packetData.BodyData);
    //         MainServer.s_MainLogger.Debug($"[reqdata] SessionID:{sessionID}, ReqRoomNumber:{reqData.RoomNumber}, UserRoomNumber:{user.RoomNumber}, IsStateRoom:{user.IsStateRoom()}");
    //         // body에서 RoomNumber 꺼내기
    //         var room = GetRoom(reqData.RoomNumber);
    //         MainServer.s_MainLogger.Debug($"[room이 있어?] _startRoomNumber:{_startRoomNumber}, _roomList.Count:{_roomList.Count}");
    //         // room이 null이면 방 번호가 잘못된 것
    //         if (room == null)
    //         {
    //             SendResponseEnterRoomToClient(ErrorCode.RoomEnterInvalidRoomNumber, sessionID);
    //             MainServer.s_MainLogger.Debug("RequestEnterInternal - Invalid room number");
    //             return;
    //         }

    //         // 방에 유저가 들어갈 수 있는지 확인
    //         if (room.AddUser(user.GetUserID(), sessionID) == false)
    //         {
    //             SendResponseEnterRoomToClient(ErrorCode.RoomEnterFailAddUser, sessionID);
    //             MainServer.s_MainLogger.Debug("RequestEnterInternal - Fail to add user");
    //             return;
    //         }


    //         user.EnteredRoom(reqData.RoomNumber);
    //         MainServer.s_MainLogger.Debug($"RequestEnterInternal - UserID: {user.GetUserID()}, RoomNumber: {reqData.RoomNumber}, roomUserCount: {room.CurrentUserCount()}");

    //         room.SendNotifyPacketUserList(sessionID);
    //         room.SendNotifyPacketNewUser(sessionID, user.GetUserID());

    //         SendResponseEnterRoomToClient(ErrorCode.None, sessionID);

    //         MainServer.s_MainLogger.Debug("RequestEnterInternal - Success");
    //     }
    //     catch (Exception e)
    //     {
    //         MainServer.s_MainLogger.Debug("enter request error: " + e.ToString());
    //         Console.WriteLine(e);
    //         throw;
    //     }
    // }