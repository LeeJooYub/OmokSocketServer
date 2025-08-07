namespace SocketServer.Packet;

// 패킷 ID 정의. 1 ~ 10000
public enum PacketId : int
{
    ReqResTestEcho = 101,


    // 클라이언트
    CsBegin = 1001,

    ReqLogin = 1002,
    ResLogin = 1003,
    NtfMustClose = 1005,

    REQ_MATCH_MAKE = 1006, // 매치 메이킹 요청
    RES_MATCH_MAKE = 1007, // 매치 메이킹 응답



    ReqRoomEnter = 1015,
    ResRoomEnter = 1016,
   
   
    NtfRoomUserList = 1017,
    NtfRoomNewUser = 1018,



    ReqRoomLeave = 1021,
    ResRoomLeave = 1022,


    NtfRoomLeaveUser = 1023,
    ReqRoomChat = 1026,
    NtfRoomChat = 1027,
    


    ReqRoomDevAllRoomStartGame = 1091,
    ResRoomDevAllRoomStartGame = 1092,

    ReqRoomDevAllRoomEndGame = 1093,
    ResRoomDevAllRoomEndGame = 1094,

    CsEnd = 1100,


    // 시스템, 서버 - 서버
    S2sStart = 8001,

    NtfInConnectClient = 8011,
    NtfInDisconnectClient = 8012,

    ReqSsServerinfo = 8021,
    ResSsServerinfo = 8023,

    ReqInRoomEnter = 8031,
    ResInRoomEnter = 8032,

    NtfInRoomLeave = 8036,


    // DB 8101 ~ 9000
    ReqDbLogin = 8101,
    ResDbLogin = 8102,



    // 오목 게임 관련 패킷 (추가)
    REQ_PUT_STONE = 2001,
    NTF_GAME_PLAYER_MOVE = 2002,
    NTF_GAME_START = 2003,
    NTF_GAME_END = 2004,
}
