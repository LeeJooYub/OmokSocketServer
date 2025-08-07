
# Copilot Instructions for Omok Client (WinForms)

## 프로젝트 개요
- 이 폴더는 오목 클라이언트(WinForms, C#) 코드입니다. 서버는 별도 폴더(SocketServer)에서 관리합니다.
- 주요 진입점: `Program.cs` → `MainForm.cs` (윈폼 UI)
- 네트워크 통신은 TCP(포트 5000 등)로, 패킷 정의는 `Packet/` 폴더에 분리되어 있습니다.
- 클라이언트는 방 입장, 매칭, 오목판 UI, 채팅 등 기능을 폼(Form) 단위로 분리합니다.

## 중요 사항
- 네트워크 통신은 `NetworkManager.cs`에서 관리합니다. 싱글턴 패턴을 사용하여 어디서든 접근할 수 있습니다.
- 각 폼(예: `RoomEnterForm`, `OmokBoardForm`)은 UI만 담당하고, 네트워크/게임 상태 관리는 `NetworkManager`와 `OmokBoardForm`에서 처리합니다.
- 패킷 구조/ID는 `Packet/PacketData.cs`, `Packet/PacketDefine.cs` 참고 (오목 전용 패킷 포함) (정말 중요)

## 폴더/파일 구조
- `Forms/` : WinForms UI (MainForm, RoomEnterForm, OmokBoardForm 등)
- `Packet/` : 패킷 ID, 데이터 구조, 직렬화 등 네트워크 메시지 정의
- `OmokBoardForm에서` : 네트워크 상태 관리 (예: NetworkManager, Boards)
- `Program.cs` : 앱 진입점
- `Client.csproj` : 프로젝트/의존성 관리

## 네트워크/게임 상태 관리
- 서버 연결/송수신은 `NetworkManager.cs`에서 담당 (싱글턴/정적 클래스 추천)
- 게임 상태(내 돌, 방 번호, 턴, 보드 등)는 OmokBoardForm에서 등에서 관리
- 각 폼(예: RoomEnterForm, OmokBoardForm)은 NetworkManager의 메서드/이벤트만 사용하여 통신
- 패킷 구조/ID는 `Packet/PacketData.cs`, `Packet/PacketDefine.cs` 참고 (오목 전용 패킷 포함)

## 주요 개발 패턴/예시
- 네임스페이스는 파일/폴더명과 일치하게 한 줄로 선언
  ```csharp
  namespace Client.Forms;
  ```
- UI와 네트워크/게임로직 분리: 폼은 UI만, 상태/통신은 GameState/Packet에서 담당
- 서버 연결 예시: `NetworkManager.Connect("127.0.0.1", 5000)`
- 방 입장/매칭: RoomEnterForm → NetworkManager로 요청, 응답 오면 OmokBoardForm으로 이동
- 오목판 상태/턴/착수 등은 OmokBoardForm에서 관리

## 확장/연동 팁
- 새로운 기능(예: 채팅, 랭킹 등)은 폴더별로 파일만 추가하면 됨
- 서버 패킷 추가 시 Packet/PacketData.cs, Packet/PacketDefine.cs에 정의 후 사용
- 네트워크 연결은 NetworkManager.cs에서 일관되게 관리

## 예시 폴더 구조
```
Client/
  Forms/
    RoomEnterForm.cs
    OmokBoardForm.cs
  Packet/
    PacketData.cs
    PacketDefine.cs
  MainForm.cs
  Program.cs
  Client.csproj
  NetworkManager.cs
```

---
새로운 폴더/파일/패턴을 추가하면 이 문서도 함께 업데이트하세요.
