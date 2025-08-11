namespace SocketServer.Room;


// 방 관리 클래스
// 방의 생성, 삭제, 유저 관리 등을 담당한다
public class RoomManager
{
    List<Room> _roomList = new List<Room>();

    // 매치 대기 큐 (RoomHandler 클래스 내부에 선언)
    public List<(string sessionID, string userID)> _matchWaitingQueue = new();


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

    public Room GetEmptyRoom()
    {
        for(int i = 0; i < _roomList.Count; i++)
        {
            var room = _roomList[i];
            if (room.CurrentUserCount() ==0 && room.GetIsGameStart() == false)
            {
                return room;
            }
        }
        return null;
    }

    public Room GetRoom(int roomNumber)
    {
        return _roomList.Find(x => x.Number == roomNumber);
    }


    public List<Room> GetRoomList() { return _roomList; }

}
