using SocketServer.Users;
using MessagePack;
using SocketServer.Packet;

namespace SocketServer.Room;


// 방 관리 클래스
// 방의 생성, 삭제, 유저 관리 등을 담당한다
public class RoomManager
{
    List<Room> _roomList = new List<Room>();

    // 매치 대기 큐
    private List<(string sessionID, string userID)> _matchWaitingQueue = new();

    // 네트워크 전송 함수 참조
    private Func<string, byte[], bool>? _netSendFunc;

    public void SetNetSendFunc(Func<string, byte[], bool> netSendFunc)
    {
        _netSendFunc = netSendFunc;
    }

    //  방들을 만든다.
    public void CreateRooms()
    {
        var maxRoomCount = MainServer.s_ServerOption.RoomMaxCount;
        var startNumber = MainServer.s_ServerOption.RoomStartNumber;
        var maxUserCount = MainServer.s_ServerOption.RoomMaxUserCount;

        for (int i = 0; i < maxRoomCount; i++)
        {
            var RoomNumber= (startNumber + i);
            var room = new Room();
            room.Init(i + 1, RoomNumber, maxUserCount);
            _roomList.Add(room);
        }
    }

    public Room? GetEmptyRoom()
    {
        for(int i = 0; i < _roomList.Count; i++)
        {
            var room = _roomList[i];
            if (room.CurrentUserCount() == 0 && room.GetIsGameStart() == false)
            {
                return room;
            }
        }
        return null;
    }

    public Room? GetRoom(int roomNumber)
    {
        return _roomList.Find(x => x.Number == roomNumber);
    }

    public List<Room> GetRoomList() 
    { 
        return _roomList; 
    }
    
    // 매치메이킹 큐 관리 메소드들
    
    public void AddToMatchWaitingQueue(string sessionID, string userID)
    {
        _matchWaitingQueue.Add((sessionID, userID));
        MainServer.s_MainLogger.Debug($"매치 대기 큐에 추가됨: {sessionID}, 유저ID: {userID}");
    }
    
    public List<(string sessionID, string userID)> GetMatchWaitingQueue()
    {
        return _matchWaitingQueue;
    }
    
    public void ClearMatchWaitingQueue()
    {
        _matchWaitingQueue.Clear();
    }
    
    public bool IsMatchWaitingQueueReady()
    {
        return _matchWaitingQueue.Count >= 2;
    }
    
    public void RemoveInvalidUsersFromQueue(UserManager userManager)
    {
        _matchWaitingQueue.RemoveAll(p => userManager.GetUser(p.sessionID) == null);
    }
    
    // 방 관련 기능
    
    public bool LeaveRoomUser(string sessionID, int roomNumber, out string userID)
    {
        userID = string.Empty;
        var room = GetRoom(roomNumber);
        if (room == null)
        {
            MainServer.s_MainLogger.Debug($"LeaveRoomUser - Invalid room number: {roomNumber}");
            return false;
        }

        var roomUser = room.GetUserByNetSessionId(sessionID);
        if (roomUser == null)
        {
            MainServer.s_MainLogger.Debug($"LeaveRoomUser - Invalid user session ID: {sessionID}");
            return false;
        }

        userID = roomUser.GetUserID();
        room.RemoveUser(roomUser);

        room.SendNotifyPacketLeaveUser(userID);
        if (room.CurrentUserCount() == 0)
        {
            MainServer.s_MainLogger.Debug($"LeaveRoomUser - Room {roomNumber} is empty, initializing room");
            room.InitializeRoom(); // 방 초기화
        }
        return true;
    }

}
