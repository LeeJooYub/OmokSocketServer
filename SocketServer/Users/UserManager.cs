

using SocketServer;

namespace SocketServer.Users;

/// <summary>
/// 유저 관리자 클래스
/// </summary>
public class UserManager
{
    int _maxUserCount;
    UInt64 _userSeq = 0;
    // session id와 유저를 매핑하는 맵
    Dictionary<string, User> _userMap = new ();



    public void Init(int maxUserCount)
    {
        _maxUserCount = maxUserCount;
    }

    // 접속해 있는 유저 추가하고 유저 시퀀스 증가
    // 중복된 세션 ID가 있으면 추가하지 않음
    public ErrorCode AddUser(string userID, string sessionID)
    {
        if (IsFullUserCount())
        {
            return ErrorCode.LoginFullUserCount;
        }

        if (_userMap.ContainsKey(sessionID))
        {
            return ErrorCode.AddUserDuplication;
        }

        ++_userSeq;

        var user = new User();
        user.Set(sessionID, userID);
        _userMap.Add(sessionID, user);

        return ErrorCode.None;
    }

    // 유저 매니저에서 유저 삭제
    public ErrorCode RemoveUser(string sessionID)
    {
        if (_userMap.Remove(sessionID) == false)
        {
            return ErrorCode.RemoveUserSearchFailureUserId;
        }

        return ErrorCode.None;
    }

    // 유저 매니저에 유저 등록
    public User GetUser(string sessionID)
    {
        User user = null;
        _userMap.TryGetValue(sessionID, out user);
        return user;
    }

    // 최대 유저수 보다, 현재 유저수가 큰지 확인
    private bool IsFullUserCount()
    {
        return _maxUserCount <= _userMap.Count;
    }
}
