namespace SocketServer;

public enum ErrorCode : short
{

    None = 0, // 에러가 아니다

    // 서버 초기화 에라
    RedisInitFail = 1,    // Redis 초기화 에러

    // 로그인 
    LoginInvalidAuthToken = 1001, // 로그인 실패: 잘못된 인증 토큰
    AddUserDuplication = 1002,
    RemoveUserSearchFailureUserId = 1003,
    UserAuthSearchFailureUserId = 1004,
    UserAuthAlreadySetAuth = 1005,
    LoginAlreadyWorking = 1006,
    LoginFullUserCount = 1007,

    // DB 에러
    // DbLoginInvalidPassword = 1011,
    // DbLoginEmptyUser = 1012,
    // DbLoginException = 1013,

    // 방 입장
    RoomEnterInvalidState = 1021,
    RoomEnterInvalidUser = 1022,
    RoomEnterErrorSystem = 1023,
    RoomEnterInvalidRoomNumber = 1024,
    RoomEnterFailAddUser = 1025,
    // 기타 에러 코드 추가
}