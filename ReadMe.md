
# OmokSocketServer 프로젝트

![오목 이미지](omok.png)


## 개요
- .NET 9.0 기반 오목 네트워크 게임 프로젝트입니다.
- 서버(`SocketServer/`)는 SuperSocketLite로 TCP 통신만 담당하는 콘솔 앱입니다.
- 클라이언트(`Client/`)는 WinForms 기반 GUI로 오목 게임을 구현합니다.

## 폴더 구조
- `SocketServer/` : 서버 코드 (SuperSocketLite 기반)
- `Client/` : 클라이언트 코드 (WinForms 기반)
- `ReadMe.md` : 프로젝트 설명 및 체크리스트

## 플레이 방식 (로컬 기반 설명)
- SocketServer 폴더에서 Dotnet Run
- Client 폴더를 두개로 복제.
- Client 폴더에서 Dotnet Run 
- Client(1) 폴더에서 Dotnet Run
- Client 모두 매칭 버튼 클릭
- 결과 확인


## 특이사항 (바이브 코딩)
- **서버:**
  - 바이브 코딩 활용 비율 = **(직접 코딩) 7** : (바이드 코딩 이용) 3
  - 바이브 코딩 시, 사용한 기능은 다음과 같습니다.
    - 코드 자동 완성 기능
    - 명칭 추천
    - 아키텍쳐 추천
    - 궁금한 점 질의 응답
- **클라이언트:**
  - 바이브 코딩 활용 비율 = (직접 코딩) 3 : **(바이드 코딩 이용) 7**
  - 바이브 코딩 시, agent 모드로 대부분의 코드를 작성하였습니다.
  - 사유 : 윈폼에 대한 지식 부족 및, 코딩 시간 단축 (퀄리티보다는 일단 렌더링 가능하게)

---

## 플레이 UI

아래는 실제 클라이언트 플레이 과정에서의 주요 UI 화면 예시입니다.

<table>
  <tr>
    <td align="center">
      <img src="로그인확인.png" alt="로그인 확인" width="250"/><br/>
      <b>1. 로그인 화면</b><br/>
      <span style="font-size:12px;">사용자가 닉네임 등 정보를 입력하고 서버에 접속합니다.</span>
    </td>
    <td align="center">
      <img src="매치매이킹.png" alt="매치메이킹" width="250"/><br/>
      <b>2. 매치메이킹 화면</b><br/>
      <span style="font-size:12px;">매칭 큐에 들어가 상대방을 기다리는 화면입니다.</span>
    </td>
  </tr>
  <tr>
    <td align="center">
      <img src="플레이화면.png" alt="플레이 화면" width="250"/><br/>
      <b>3. 플레이(오목판) 화면</b><br/>
      <span style="font-size:12px;">오목판에서 돌을 두며 실시간으로 게임이 진행됩니다.</span>
    </td>
    <td align="center">
      <img src="결과화면.png" alt="결과 화면" width="250"/><br/>
      <b>4. 결과 화면</b><br/>
      <span style="font-size:12px;">게임 종료 후 승패 결과 및 후처리 안내가 표시됩니다.</span>
    </td>
  </tr>
</table>

---

## 클래스 다이어그램

```mermaid
classDiagram
    %% SuperSocket 기본 클래스들
    class AppServer~ClientSession, EFBinaryRequestInfo~
    class AppSession~ClientSession, EFBinaryRequestInfo~
    class IRequestInfo
    
    %% 서버 관련 클래스
    class MainServer {
        +static ILog s_MainLogger
        +static ServerOption s_ServerOption
        +IServerConfig _config
        +PacketProcessor _mainPacketProcessor
        +RoomManager _roomManager
        +UserManager _userManager
        +MainServer()
        +InitConfig(ServerOption)
        +CreateServer()
        +InitLogger()
        +InitManagers()
        +InitComponent() ErrorCode
        +SendData(string, byte[]) bool
        +Distribute(ServerPacketData)
        -OnConnected(ClientSession)
        -OnClosed(ClientSession, CloseReason)
        -OnPacketReceived(ClientSession, EFBinaryRequestInfo)
        +GetSessionCount() int
        +StopServer()
    }
    
    class ClientSession {
    }
    
    class EFBinaryRequestInfo {
        +int PacketSize
        +int PacketID
        +short Dummy
        +byte[] Body
        +int TotalSize
    }
    
    class ReceiveFilter
    
    %% 패킷 처리 관련 클래스
    class PacketProcessor {
        -bool _isThreadRunning
        -Thread _processThread
        +BufferBlock~ServerPacketData~ _packetBuffer
        +Dictionary~int, Action~ _packetHandlerMap
        -UserManager _userManager
        -RoomManager _roomManager
        -ConnectionHandler _connectionHandler
        -RoomHandler _roomHandler
        +Create(MainServer)
        +Start()
        +Destroy()
        +PushPacket(ServerPacketData)
        -Process()
        -RegisterPacketHandlers(Func, Action, Func)
    }
    
    class ServerPacketData {
        +string SessionID
        +int PacketSize
        +int PacketID
        +short Dummy
        +byte[] BodyData
        +static MakeNTFInConnectOrDisConnectClientPacket(bool, string)
    }
    
    %% 핸들러 관련 클래스
    class HandlerBase {
        #Func~string, byte[], bool~ NetSendFunc
        #Action~ServerPacketData~ DistributeFunc
        #Func~int~ GetSessionCountFunc
        #UserManager _userManager
        #RoomManager _roomManager
        +abstract RegisterPacketHandler(Dictionary~int, Action~)
        +Init(Func, Action, Func, UserManager, RoomManager)
    }
    
    class ConnectionHandler {
        +RegisterPacketHandler(Dictionary~int, Action~)
        +HandleLogin(ServerPacketData)
        +SendResponseToClient(ErrorCode, string, string)
    }
    
    class RoomHandler {
        +RegisterPacketHandler(Dictionary~int, Action~)
        +HandleRequestRoomEnter(ServerPacketData)
        +HandleRequestRoomLeave(ServerPacketData)
        +HandleRequestMoveStone(ServerPacketData)
        +HandleRequestMatchMake(ServerPacketData)
    }
    
    %% 관리자 클래스
    class RoomManager {
        +List~Room~ _roomList
        +List~(string, string)~ _matchWaitingQueue
        +CreateRooms()
        +GetRoom(int)
        +GetEmptyRoom()
    }
    
    class UserManager {
        +Dictionary~string, User~ _userSessionDic
        +Dictionary~string, User~ _userIdDic
        +Init(int)
        +GetUser(string)
        +AddUser(string, string)
        +RemoveUser(string)
    }
    
    %% 게임 상태 관련 클래스
    class Room {
        +int Number
        +int Turn
        +bool IsGameStart
        +static Func~string, byte[], bool~ NetSendFunc
        +AddUser(string, string)
        +RemoveUser(RoomUser)
        +PlayerMove(int, int)
        +SendNotifyPlayerMove(int, int, bool, char)
        +InitializeGameStatus()
        +GetPlayerColor(string)
    }
    
    class User {
        +string UserID
        +string SessionID
        +int RoomNumber
        +bool IsConfirm(string)
        +bool IsStateRoom()
        +void EnteredRoom(int)
        +void LeaveRoom()
    }
    
    class RoomUser {
        +string UserID
        +string NetSessionID
        +void Set(string, string)
    }
    
    %% 게임 로직 관련 클래스
    class OmokRule {
        +CheckWinCondition(OmokBoard, int, int)
    }
    
    class OmokBoard {
        +int[,] Board
        +PlaceStone(int, int, int)
        +Initialize()
    }
    
    %% 설정 관련 클래스
    class ServerOption {
        +string Name
        +int Port
        +int MaxRequestLength
        +int MaxConnectionNumber
        +int RoomMaxCount
        +int RoomMaxUserCount
    }
    
    class ErrorCode {
        +None
        +RoomEnterInvalidRoomNumber
        +RoomEnterAlreadyUser
        +RoomEnterFullRoom
        +RoomEnterInvalidUser
        +RoomEnterInvalidState
    }
    
    %% 클래스 간의 관계
    AppServer <|-- MainServer
    AppSession <|-- ClientSession
    IRequestInfo <|-- EFBinaryRequestInfo
    
    MainServer *-- PacketProcessor
    MainServer *-- RoomManager
    MainServer *-- UserManager
    MainServer --> ClientSession
    
    RoomManager *-- Room
    UserManager *-- User
    
    Room *-- RoomUser
    Room *-- OmokBoard
    Room *-- OmokRule
    
    PacketProcessor --> ConnectionHandler
    PacketProcessor --> RoomHandler
    PacketProcessor --> ServerPacketData: processes
    PacketProcessor o-- UserManager
    PacketProcessor o-- RoomManager
    
       %% 관계 정의
    HandlerBase <|-- ConnectionHandler
    HandlerBase <|-- RoomHandler
    
    MainServer --> ServerOption
```

---
## 시퀸스 다이어그램

### 1. 연결 , 로그인, 연결 해제
```mermaid
sequenceDiagram
    participant Client as 클라이언트
    participant AppServer as MainServer
    participant PP as PacketProcessor
    participant CH as ConnectionHandler
    participant RH as RoomHandler
    participant RM as RoomManager
    participant Room as Room
    participant UM as UserManager
    participant User as User
    participant OR as OmokRule

    %% 연결 및 로그인 과정
    Client->>AppServer: TCP 연결 요청
    activate AppServer
    AppServer->>AppServer: OnConnected(session)
    AppServer->>PP: Distribute(NtfInConnectClient)
    activate PP
    PP->>CH: HandleNotifyInConnectClient(packet)
    activate CH
    CH->>CH: 연결 정보 로깅
    deactivate CH
    deactivate PP

    %% 로그인 과정
    Client->>AppServer: ReqLogin(userID)
    AppServer->>AppServer: OnPacketReceived
    AppServer->>PP: Distribute(packet)
    activate PP
    PP->>CH: HandleRequestLogin(packet)
    activate CH
    CH->>UM: AddUser(sessionID, userID)
    activate UM
    UM->>User: 생성 및 초기화
    UM-->>CH: 성공/실패
    deactivate UM
    CH->>Client: ResLogin(결과)
    deactivate CH
    deactivate PP
    
    %% 연결 종료
    Client->>AppServer: 연결 종료
    AppServer->>AppServer: OnClosed(session)
    AppServer->>PP: Distribute(NtfInDisconnectClient)
    activate PP
    PP->>CH: HandleNotifyInDisConnectClient(packet)
    activate CH
    CH->>UM: GetUser(sessionID)
    
    alt 방에 있었던 경우
        CH->>PP: Distribute(NtfInRoomLeave)
        PP->>RH: HandleNotifyLeaveInternal(packet)
        activate RH
        RH->>RM: GetRoom(roomNum)
        RH->>Room: RemoveUser(user)
        RH->>Room: 남은 유저에게 퇴장 알림
        deactivate RH
    end
    
    CH->>UM: RemoveUser(sessionID)
    deactivate CH
    deactivate PP
    AppServer-->>Client: 연결 종료
    deactivate AppServer
```

### 2. 게임 진행
```mermaid
sequenceDiagram
    participant Client as 클라이언트
    participant AppServer as MainServer
    participant PP as PacketProcessor
    participant CH as ConnectionHandler
    participant RH as RoomHandler
    participant RM as RoomManager
    participant Room as Room
    participant UM as UserManager
    participant User as User
    participant OR as OmokRule
    
    %% 매치메이킹 요청
    Client->>AppServer: REQ_MATCH_MAKE
    AppServer->>AppServer: OnPacketReceived
    AppServer->>PP: Distribute(packet)
    activate PP
    PP->>RH: HandleRequestMatchMake(packet)
    activate RH
    RH->>UM: GetUser(sessionID)
    RH->>RM: _matchWaitingQueue.Add(user)
    
    alt 매치 가능한 경우 (큐에 2명 이상)
        RH->>RM: GetEmptyRoom()
        activate RM
        RM-->>RH: room
        deactivate RM
        RH->>Room: AddUser(userID, sessionID) x 2명
        activate Room
        Room-->>RH: 성공
        deactivate Room
        RH->>UM: EnteredRoom(roomNum) x 2명
        RH->>Client: RES_MATCH_MAKE(성공, 색상정보)
    end
    deactivate RH
    deactivate PP

    %% 게임 진행 (착수)
    Client->>AppServer: REQ_PUT_STONE(x, y)
    AppServer->>AppServer: OnPacketReceived
    AppServer->>PP: Distribute(packet)
    activate PP
    PP->>RH: HandleRequestMoveStone(packet)
    activate RH
    RH->>UM: GetUser(sessionID)
    RH->>UM: IsStateRoom()
    RH->>RM: GetRoom(roomNum)
    RH->>Room: GetPlayerColor(userID)
    RH->>Room: PlayerMove(x, y, color)
    activate Room
    Room->>OR: CheckWinCondition(board, x, y)
    activate OR
    OR-->>Room: 승패 결과
    deactivate OR

    alt 게임 계속 (승패 없음)
        Room->>Room: Turn++
        Room-->>RH: result=0
        RH->>Room: SendNotifyPlayerMove(x, y, false, ' ')
        Room->>Client: NTF_GAME_PLAYER_MOVE(x, y, turn, false, ' ')
        activate Client
        Client->>Client: HandleMoveStone(data)
        Client->>Client: 상대방 돌 표시(board[x,y]=color)
        Client->>Client: 턴 업데이트(Turn=res.Turn)
        Client->>Client: Invalidate() (화면 갱신)
        deactivate Client
    else 게임 종료 (승리자 발생)
        Room->>Room: 게임 종료 처리(IsGameStart=false)
        Room-->>RH: result=1 or 2
        RH->>Room: SendNotifyPlayerMove(x, y, true, winnerColor)
        Room->>Client: NTF_GAME_PLAYER_MOVE(x, y, turn, true, winnerColor)
        activate Client
        Client->>Client: HandleMoveStone(data)
        Client->>Client: 마지막 돌 표시(board[x,y]=color)
        Client->>Client: GameEnd(winnerColor) 호출
        Client->>Client: 승리 메시지 표시(MessageBox.Show)
        Client->>Client: 게임창 닫기(DialogResult=OK, Close())
        deactivate Client
        RH->>Room: InitializeGameStatus()
        Room->>Room: 보드, 턴 초기화
    end
    deactivate Room
    deactivate RH
    deactivate PP
```





---

# 체크리스트 (개발 순서)

## 0. 학습
- ✅ Echo서버 코드 분석
- ✅ [.NET Conf 2023 x Seoul] Hands-on-Lab: Echo Server 영상 시청
- ✅  chat서버 코드 분석

## 1. 기본 뼈대 구축
### 서버 구축
- ✅  chat 서버 코드를 기반으로, 서버 코드 뼈대 구축
- ✅  MainServer.cs , 패킷 처리기 구축
- ✅  room, room manager 구축 
- ✅  매칭 시스템 구축 (매칭 큐)

### 클라이언트 구축
- ✅ (바이브 코딩 + WinForm)을 이용하여 기본 클라이언트 뼈대 구축
- ✅ 오목판 Winform을 이용한 GUI 구축

### 클라-서버 테스트
- ✅ 서버-클라이언트 통신 테스트 (매칭 큐 테스트)

## 2. 게임 로직 (룰) 추가 및 프로토 타입 완성
- ✅ 서버 : 오목 게임 관련 로직 추가(게임 상태 관리) , 승리 조건 확인
- ✅ 클라이언트: 게임 진행 실시간 렌더링 로직 추가
- ✅ 프로토 타입 게임 테스트
- ✅ 게임 결과 및 후처리 로직 추가 (서버 , 클라이언트)

## 3. 코드 리팩토링 , 예외/에러 처리 및 유지보수 
- ✅ 코드 리뷰 후, 필요한 부분 리팩토링
- ✅ 디렉토리 구조 정리
- ✅ 문서 정리 (시퀀스 다이어그램, 클래스 다이어그램 작성)

## 4. 예외/에러 처리 및 유지보수 
- ⬜ 예외/에러 핸들링 및 사용자 알림
- ⬜ 주석 및 코드 정리

## 5. 추가 기능 (시간이 남는 경우)
- ⬜ 채팅 기능 추가
- ⬜ 게임 방 기능 추가



---
