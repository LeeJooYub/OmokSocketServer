

namespace SocketServer.GameState;


/// 유저 클래스
public class User
{
    // 서버에 연결시 시퀀스 부여. 0이면 연결되지 않은 상태
    UInt64 _userSeq = 0;
    // 연결된 세션 ID
    string _sessionID;
    // 유저가 속한 방 번호. -1이면 방에 속하지 않음
    public int RoomNumber { get; private set; } = -1;
    // 유저 ID
    string _userID;


    public void Set(UInt64 userSeq, string sessionID, string userID)
    {
        _userSeq = userSeq;
        _sessionID = sessionID;
        _userID = userID;
    }

    //현재 세션 id와 비교하여 세션이 일치하는지 확인
    public bool IsConfirm(string netSessionID)
    {
        var isConfirm = _sessionID == netSessionID;
        return isConfirm;
    }

    public string GetUserID()
    {
        return _userID;
    }

    public void EnteredRoom(int roomNumber)
    {
        this.RoomNumber = roomNumber;
    }

    public void LeaveRoom()
    {
        RoomNumber = -1;
    }

    public bool IsStateLogin()
    {
        var isLogin = _userSeq != 0;
        return isLogin;
    }

    public bool IsStateRoom()
    {
        var isStateRoom = RoomNumber != -1;
        return isStateRoom;
    }

    public int GetRoomNumber()
    {
        return RoomNumber;
    }


}

