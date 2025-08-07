using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using MessagePack;

using SocketServer.Packet;
using SocketServer.GameLogic;

namespace SocketServer.GameState;

public class Room
{
    public int Id { get; set; }
    public int Number { get; set; }
    public int Turn { get; set; } = 0; // 현재 턴. 게임 시작 전 = 0, 게임 시작 후 = 1, 2, ...
    int _maxUserCount = 2; // 기본 최대 사용자 수
    List<RoomUser> _userList = new List<RoomUser>(); // 방에 있는 유저 리스트. 0은 백. 1은 흑.
    bool IsGameStart = false;
    OmokBoard omokBoard = new OmokBoard(); // 오목 보드 인스턴스
    OmokRule omokRule = new OmokRule(); // 오목 룰 인스턴스
    // 인스턴스 생성시, supersocket의 Send 메소드를 받아서 초기화
    public static Func<string, byte[], bool> NetSendFunc;


    public void Init(int id, int number, int maxUserCount = 2)
    {
        Id = id;
        Number = number;
        _maxUserCount = maxUserCount;
    }

    public bool AddUser(string userID, string netSessionID)
    {

        if (GetUser(userID) != null)
        {
            return false;
        }

        var roomUser = new RoomUser();
        roomUser.Set(userID, netSessionID);
        _userList.Add(roomUser);

        return true;
        
    }
    public bool GetIsGameStart()
    {
        return IsGameStart;
    }

    public void RemoveUser(string netSessionID)
    {
        var index = _userList.FindIndex(x => x.NetSessionID == netSessionID);
        _userList.RemoveAt(index);
    }

    public bool RemoveUser(RoomUser user)
    {
        return _userList.Remove(user);
    }

    public RoomUser GetUser(string userID)
    {
        RoomUser? user = _userList.Find(x => x.UserID == userID);
        return user;
    }

    //유저가 백인지, 흑인지 확인
    public int GetPlayerColor(string userId)
    {
        for(int i = 0; i < _userList.Count; i++)
        {
            var user = _userList[i];
            if (user.UserID == userId)
            {
                return i + 1; // 백은 1, 흑은 2
            }
        }
        return 0; // 유저가 방에 없음
    }

    public RoomUser GetUserByNetSessionId(string netSessionID)
    {
        RoomUser? user = _userList.Find(x => x.NetSessionID == netSessionID);
        return user;
    }

    public int CurrentUserCount()
    {
        return _userList.Count();
    }

    public void StartGame()
    {
        IsGameStart = true;
        Turn = 1; // 게임 시작 시 턴을 1로 설정
    }

    public bool InitializeGameStatus()
    {
        IsGameStart = false;
        Turn = 0; // 턴 초기화
        omokBoard.Initialize(); // 오목판 초기화
        return true;
    }
    
    public bool InitializeRoom()
    {
        if (_userList.Count == 0)
        {
            return false;
        }

        _userList.Clear();
        return true;
    }

    // public void SendNotifyPacketGameEnd()
    // {
    //     var packet = new PKTNtfGameEnd();
    //     packet.RoomNumber = Number;

    //     var bodyData = MessagePackSerializer.Serialize(packet);
    //     var sendPacket = PacketToBytes.Make(PacketId.NtfRoomGameEnd, bodyData);

    //     MainServer.s_MainLogger.Debug($"방 {Number}에서 게임 종료 알림을 보냅니다.");

    //     foreach (var user in _userList)
    //     {
    //         NetSendFunc(user.NetSessionID, sendPacket);
    //     }
    // }

    // 플레이어에게 착수 패킷을 받으면, 이를 확인 후에, 서버에서 플레이어 착수 반영 확인 및 게임 종료 판단 여부 패킷을 보낸다.
    public void SendNotifyPlayerMove(int x, int y, bool isGameEnd, char winnerColor)
    {
        var packet = new PKTNtfGamePlayerMove();
        packet.X = x;
        packet.Y = y;
        packet.Turn = Turn;
        packet.IsGameEnd = isGameEnd;
        packet.WinnerColor = winnerColor;

        var bodyData = MessagePackSerializer.Serialize(packet);
        var sendPacket = PacketToBytes.Make(PacketId.NTF_GAME_PLAYER_MOVE, bodyData);

        MainServer.s_MainLogger.Debug($"방 {Number}에서 플레이어 이동 알림 혹은 게임 종료 안내 패킷을 보냅니다. X: {x}, Y: {y}, 턴: {Turn}");

        foreach (var user in _userList)
        {
            NetSendFunc(user.NetSessionID, sendPacket);
        }
    }

    //playerNum 1: 백, 2: 흑
    //0 반환시, 승패 결정 안남.
    public int PlayerMove(int x, int y, int playerNum)
    {
        if (Turn % 2 != playerNum % 2) // 현재 턴이 플레이어의 턴인지 확인
        {
            throw new InvalidOperationException("현재 턴이 아닙니다. 다른 플레이어의 차례입니다.");
        }
        // 현재 턴에 해당하는 플레이어의 돌을 놓는다
        omokBoard.PlaceStone(x, y, playerNum);

        // 승리 조건 체크
        int winCondition = omokRule.CheckWinCondition(omokBoard, x, y);
        if (winCondition != 0)
        {
            IsGameStart = false; // 게임 종료
            MainServer.s_MainLogger.Debug($"게임 종료: 플레이어 {winCondition} 승리");
            return winCondition;
        }

        // 다음 턴으로 이동
        Turn++;
        return 0;
    }



    // TODO : 오목은 2인용으로 해당 함수들을 수정 ? 안해도 작동은 된다.
    // 옵저버 기능을 고려하여 놔둘것인지.

    // 새 유저 입장 전 -> 방에 있는 유저들의 정보를, 들어올 사람 에게 보낸다.
    // 방에 새로 들어올 유저에게 현재 방에 있는 모든 유저의 정보를 전송한다.
    // 아래는 채팅 기능 추가할 시, 로직.
    // public void SendNotifyPacketUserList(string userNetSessionID)
    // {
    //     var packet = new PKTNtfRoomUserList();
    //     foreach (var user in _userList)
    //     {
    //         packet.UserIDList.Add(user.UserID);
    //     }

    //     var bodyData = MessagePackSerializer.Serialize(packet);
    //     var sendPacket = PacketToBytes.Make(PacketId.NtfRoomUserList, bodyData);

    //     MainServer.s_MainLogger.Debug($"당신은 새 유저. 방에 있는 사람들 정보를 보냅니다.");

    //     NetSendFunc(userNetSessionID, sendPacket);
    // }

    // // 새 유저 입장 처리 후 -> newUserNetSessionID (새 유저) 를 제외한 방에 있는 유저들에게 알림 패킷을 보낸다
    // // 방에 새로 들어오는 유저의 정보를 보낸다(이미 방에 있는 사람들에게 보낸다)
    // public void SendNotifyPacketNewUser(string newUserNetSessionID, string newUserID)
    // {
    //     var packet = new PKTNtfRoomNewUser();
    //     packet.UserID = newUserID;

    //     var bodyData = MessagePackSerializer.Serialize(packet);
    //     var sendPacket = PacketToBytes.Make(PacketId.NtfRoomNewUser, bodyData);

    //     MainServer.s_MainLogger.Debug($"방에 새 유저가 들어왔습니다. UserID: {newUserID}");

    //     Broadcast(newUserNetSessionID, sendPacket);
    // }

    // 나가는 유저 퇴장 처리 및 userId를 저장. 해당 id로 방에 남은 유저들에게 알림 패킷을 보낸다
    // 방에서 나가는 유저의 정보를 보낸다(아직 방에 있는 사람들에게 보낸다)
    public void SendNotifyPacketLeaveUser(string userID)
    {
        if (CurrentUserCount() == 0)
        {
            return;
        }

        var packet = new PKTNtfRoomLeaveUser();
        packet.UserID = userID;

        var bodyData = MessagePackSerializer.Serialize(packet);
        var sendPacket = PacketToBytes.Make(PacketId.NtfRoomLeaveUser, bodyData);

        MainServer.s_MainLogger.Debug($"방에서 해당 유저가 나갔습니다. UserID: {userID}");

        // ""는 제외하는 유저가 없다는 뜻. 모든 유저에게 정보전달.
        Broadcast("", sendPacket);
    }

    // 방에 있는 모든 유저에게 패킷을 전송한다. 특정 세션 ID는 제외할 수 있다.
    public void Broadcast(string excludeNetSessionID, byte[] sendPacket)
    {
        foreach (var user in _userList)
        {
            if (user.NetSessionID == excludeNetSessionID)
            {
                continue;
            }

            NetSendFunc(user.NetSessionID, sendPacket);
        }
    }

}

public class RoomUser
{
    public string UserID { get; private set; }

    public string NetSessionID { get; private set; }


    public void Set(string userID, string netSessionID)
    {
        UserID = userID;
        NetSessionID = netSessionID;
    }
}


    // // 매칭 시작 후, 게임 시작 알림 패킷을 방에 있는 모든 유저에게 전송한다.
    // public void SendNotifyPacketGameStart()
    // {
    //     var packet1 = new PKTNtfGameStart();
    //     packet1.MyStoneColor = 'W';
    //     var bodyData = MessagePackSerializer.Serialize(packet1);
    //     var sendPacket = PacketToBytes.Make(PacketId.NTF_GAME_START, bodyData);
    //     var player1SessionID = _userList[0].NetSessionID;
    //     NetSendFunc(player1SessionID, sendPacket);
    //     MainServer.s_MainLogger.Debug($"{_userList[0].UserID} 님에게 게임 시작 알림을 보냅니다. 백돌입니다.");

    //     var packet2 = new PKTNtfGameStart();
    //     packet2.MyStoneColor = 'B';
    //     bodyData = MessagePackSerializer.Serialize(packet2);
    //     sendPacket = PacketToBytes.Make(PacketId.NTF_GAME_START, bodyData);
    //     var player2SessionID = _userList[1].NetSessionID;
    //     NetSendFunc(player2SessionID, sendPacket);
    //     MainServer.s_MainLogger.Debug($"{_userList[1].UserID} 님에게 게임 시작 알림을 보냅니다. 흑돌입니다.");
    // }