
using SocketServer.GameState;
using SocketServer.Packet;
using MessagePack;

namespace SocketServer.Handlers;

public class RoomHandler : HandlerBase
{

    

    public override void RegisterPacketHandler(Dictionary<int, Action<ServerPacketData>> packetHandlerMap)
    {
        // 방 관련 패킷 핸들러 등록
        // 방 입장, 퇴장 요청 핸들러 등록
        //packetHandlerMap.Add((int)PacketId.ReqRoomEnter, HandleRequestRoomEnter);
        packetHandlerMap.Add((int)PacketId.ReqRoomLeave, HandleRequestLeave);

        // 매치 메이킹 요청 및 응답 핸들러 등록
        packetHandlerMap.Add((int)PacketId.REQ_MATCH_MAKE, HandleRequestMatchMake);

        // 착수 요청 핸들러
        packetHandlerMap.Add((int)PacketId.REQ_PUT_STONE, HandleRequestMoveStone);

        // 방 퇴장 알림 핸들러
        packetHandlerMap.Add((int)PacketId.NtfInRoomLeave, HandleNotifyLeaveInternal);
    }

    // 착수 요청 핸들러
    public void HandleRequestMoveStone(ServerPacketData packetData)
    {
        MainServer.s_MainLogger.Debug("HandleRequestMoveStone 핸들러 호출");
        var sessionID = packetData.SessionID;
        var user = _userManager.GetUser(sessionID);
        if (user == null || user.IsConfirm(sessionID) == false )
        {
            MainServer.s_MainLogger.Debug($"HandleRequestMoveStone - Invalid user or state. SessionID: {sessionID}");
            SendResponseEnterRoomToClient(ErrorCode.RoomEnterInvalidUser, sessionID);
            return;
        }

        if(user.IsStateRoom() == false)
        {
            MainServer.s_MainLogger.Debug($"HandleRequestMoveStone - User not in room. SessionID: {sessionID}");
            SendResponseEnterRoomToClient(ErrorCode.RoomEnterInvalidState, sessionID);
            return;
        }

        var room = _roomManager.GetRoom(user.GetRoomNumber());
        if (room == null)
        {
            MainServer.s_MainLogger.Debug($"HandleRequestMoveStone - Invalid room. SessionID: {sessionID}, RoomNumber: {user.GetRoomNumber()}");
            SendResponseEnterRoomToClient(ErrorCode.RoomEnterInvalidState, sessionID);
            return;
        }
        
        var playerColor = room.GetPlayerColor(user.GetUserID());
        // 착수 처리
        var req = MessagePackSerializer.Deserialize<PKTReqPutStone>(packetData.BodyData);
        MainServer.s_MainLogger.Debug($"HandleRequestMoveStone: {sessionID}, X: {req.X}, Y: {req.Y}, PlayerColor: {playerColor}");
        var result = room.PlayerMove(req.X, req.Y, playerColor);
        MainServer.s_MainLogger.Debug($"PlayerMove 결과: {result}");

        // 승리 조건 체크. result가 0이면 게임이 끝나지 않은 상태, 1이면 백이 승리, 2면 흑이 승리
        if (result == 0)
        {
            // 성공적으로 착수한 경우, 모든 유저에게 알림
            room.SendNotifyPlayerMove(req.X, req.Y, false, ' '); // 게임 종료 아님, 승리자 없음
            return;
        }
        else if (result == 1 || result == 2) // 백이 이겼거나 흑이 이긴 경우
        {
            var winnerColor = result == 1 ? 'W' : 'B';
            room.SendNotifyPlayerMove(req.X, req.Y, true, winnerColor);
            MainServer.s_MainLogger.Debug($"게임 종료: {winnerColor} 승리");
            room.InitializeGameStatus();
            return;
        }
    }


    // 매치메이킹 요청 핸들러
    public void HandleRequestMatchMake(ServerPacketData packetData)
    {
        var sessionID = packetData.SessionID;
        var user = _userManager.GetUser(sessionID);
        MainServer.s_MainLogger.Debug($"HandleRequestMatchMake 핸들러 호출: {sessionID}");
        if (user == null || user.IsConfirm(sessionID) == false)
        {
            MainServer.s_MainLogger.Debug($"HandleRequestMatchMake - Invalid user. SessionID: {sessionID}");
            SendResponseEnterRoomToClient(ErrorCode.RoomEnterInvalidUser, sessionID);
            return;
        }
        if (user.IsStateRoom())
        {
            MainServer.s_MainLogger.Debug($"HandleRequestMatchMake - User already in room. SessionID: {sessionID}");
            SendResponseEnterRoomToClient(ErrorCode.RoomEnterInvalidState, sessionID);
            return;
        }

        // 대기 큐에 추가
        _roomManager._matchWaitingQueue.Add((sessionID, user.GetUserID()));
        MainServer.s_MainLogger.Debug($"매치 대기 큐에 추가됨: {sessionID}, 유저ID: {user.GetUserID()}");

        // 두 명이 모이면 매치 성사
        if (_roomManager._matchWaitingQueue.Count >= 2)
        {
            try
            {
                MainServer.s_MainLogger.Debug("매치 메이킹 시작: 두 명의 유저가 대기 중");
                // 랜덤으로 색깔 배분
                var rnd = new Random();
                int first = rnd.Next(2); // 0 또는 1
                var playerA = _roomManager._matchWaitingQueue[0];
                var playerB = _roomManager._matchWaitingQueue[1];

                var colorA = 'W';
                var colorB = 'B';

                var room = _roomManager.GetEmptyRoom();
                room.AddUser(playerA.userID, playerA.sessionID);
                room.AddUser(playerB.userID, playerB.sessionID);
                MainServer.s_MainLogger.Debug($"전투 방 생성: {room.Number}, 유저A: {playerA.sessionID}({playerA.userID}), 유저B: {playerB.sessionID}({playerB.userID})");
                var userA = _userManager.GetUser(playerA.sessionID);
                var userB = _userManager.GetUser(playerB.sessionID);
                userA.EnteredRoom(room.Number);
                userB.EnteredRoom(room.Number);

                MainServer.s_MainLogger.Debug($"유저 방 입장: {userA.GetUserID()}({userA.RoomNumber}), {userB.GetUserID()}({userB.RoomNumber})");


                // 응답 패킷 생성 및 전송
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

                _mainServer.SendData(playerA.sessionID, sendA);
                _mainServer.SendData(playerB.sessionID, sendB);
                room.StartGame();


                // 대기 큐 초기화   
                _roomManager._matchWaitingQueue.Clear();
            }
            catch (Exception ex)
            {
                MainServer.s_MainLogger.Error($"매치 메이킹 처리 중 오류 발생: {ex}");
                // 오류 발생 시 대기 큐 초기화
                _roomManager._matchWaitingQueue.Clear();
                return;
            }
        }
        // 두 명이 안 모이면 아무 응답도 하지 않고 대기
    }



    void SendResponseEnterRoomToClient(ErrorCode errorCode, string sessionID)
    {
        var resRoomEnter = new PKTResRoomEnter()
        {
            Result = (short)errorCode
        };

        var bodyData = MessagePackSerializer.Serialize(resRoomEnter);
        var sendData = PacketToBytes.Make(PacketId.ResRoomEnter, bodyData);

        _mainServer.SendData(sessionID, sendData);
    }

    // 방에서 나가겠다는 요청 처리
    public void HandleRequestLeave(ServerPacketData packetData)
    {
        var sessionID = packetData.SessionID;
        MainServer.s_MainLogger.Debug("게임 방 벗어나기 요청");

        try
        {
            var user = _userManager.GetUser(sessionID);
            if (user == null)
            {
                MainServer.s_MainLogger.Debug("Room RequestLeave - Invalid user");
                return;
            }

            MainServer.s_MainLogger.Debug($"LeaveRoomUser. SessionID:{sessionID}, RoomNumber:{user.RoomNumber}");
            if (LeaveRoomUser(sessionID, user.RoomNumber) == false)
            {
                MainServer.s_MainLogger.Debug("Room RequestLeave - Fail to leave room");
                return;
            }

            user.LeaveRoom();

            SendResponseLeaveRoomToClient(sessionID);

            MainServer.s_MainLogger.Debug("Room RequestLeave - Success");
        }
        catch (Exception ex)
        {
            MainServer.s_MainLogger.Debug("Room RequestLeave - Error: " );
            MainServer.s_MainLogger.Error(ex.ToString());
        }
    }

    bool LeaveRoomUser(string sessionID, int RoomNumber)
    {
        MainServer.s_MainLogger.Debug($"LeaveRoomUser. SessionID:{sessionID}");

        var room = _roomManager.GetRoom(RoomNumber);
        if (room == null)
        {
            MainServer.s_MainLogger.Debug($"LeaveRoomUser - Invalid room number: {RoomNumber}");
            return false;
        }

        var roomUser = room.GetUserByNetSessionId(sessionID);
        if (roomUser == null)
        {
            MainServer.s_MainLogger.Debug($"LeaveRoomUser - Invalid user session ID: {sessionID}");
            return false;
        }

        var userID = roomUser.UserID;
        room.RemoveUser(roomUser);

        room.SendNotifyPacketLeaveUser(userID);
        if(room.CurrentUserCount() == 0)
        {
            MainServer.s_MainLogger.Debug($"LeaveRoomUser - Room {RoomNumber} is empty, removing room");
            room.InitializeRoom(); // 방 초기화
        }
        return true;
    }

    void SendResponseLeaveRoomToClient(string sessionID)
    {
        var resRoomLeave = new PKTResRoomLeave()
        {
            Result = (short)ErrorCode.None
        };

        var bodyData = MessagePackSerializer.Serialize(resRoomLeave);
        var sendData = PacketToBytes.Make(PacketId.ResRoomLeave, bodyData);

        _mainServer.SendData(sessionID, sendData);
    }
    
    public void HandleNotifyLeaveInternal(ServerPacketData packetData)
    {
        var sessionID = packetData.SessionID;
        MainServer.s_MainLogger.Debug($"NotifyLeaveInternal. SessionID: {sessionID}");

        var reqData = MessagePackSerializer.Deserialize<PKTInternalNtfRoomLeave>(packetData.BodyData);            
        LeaveRoomUser(sessionID, reqData.RoomNumber);
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