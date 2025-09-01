# Omok 프로젝트

오목 게임의 클라이언트(WinForms)와 서버(.NET)로 구성된 실시간 멀티플레이 오목 시스템입니다.

## 폴더 구조

- `Client/` : WinForms 기반 오목 클라이언트
	- `Forms/` : UI 폼 (방 입장, 오목판 등)
	- `Packet/` : 패킷 정의/라우팅/버퍼 관리
	- `NetworkManager.cs` : TCP 네트워크 관리
- `SocketServer/` : .NET 기반 오목 서버
	- `GameLogic/` : 오목판, 룰 등 게임 로직
	- `Rooms/`, `Managers/` : 방/유저 관리
	- `Packet/` : 패킷 처리 및 정의
	- `Handlers/` : 각종 서버 핸들러
- `AgentRef/` : 코드 스타일, PR, 리뷰 가이드라인
- `docs/` : 문서 및 추가 안내
- `*.png` : 주요 UI/기능 스크린샷

## 빌드 및 실행

### 서버

```sh
dotnet restore SocketServer/SocketServer.csproj
dotnet build SocketServer/SocketServer.csproj -c Debug
dotnet run --project SocketServer/SocketServer.csproj
```

### 클라이언트

```sh
dotnet restore Client/Client.csproj
dotnet build Client/Client.csproj -c Debug
dotnet run --project Client/Client.csproj
```

## 테스트

- 서버: `SocketServer.Tests` (xUnit)
- 클라이언트: `Client.Tests` (xUnit)
- 실행:  
	```sh
	dotnet test SocketServer.Tests/SocketServer.Tests.csproj
	dotnet test Client.Tests/Client.Tests.csproj
	```
- 커버리지:  
	```sh
	dotnet test -p:CollectCoverage=true -p:CoverletOutputFormat=cobertura
	```

## 주요 기능

- 실시간 오목 게임 (19x19)
- 방 생성/입장/퇴장/채팅
- TCP 패킷 기반 통신
- WinForms UI (로그인, 매치메이킹, 플레이 화면 등)
- 서버: SuperSocketLite 기반, 멀티스레드 패킷 처리

## 개발/PR/리뷰 가이드라인

- 코드 스타일: `AgentRef/coding_style.md` 참고
- 커밋/PR 규칙: `AgentRef/pr_rules.md` 참고
- 리뷰 기준: `AgentRef/review_guideline.md` 참고
- 클라이언트/서버별 상세 가이드: 각 폴더의 `AGENTS.md` 참고

## 스크린샷

- 로그인, 매치메이킹, 플레이, 결과 화면 등 PNG 파일 참고
