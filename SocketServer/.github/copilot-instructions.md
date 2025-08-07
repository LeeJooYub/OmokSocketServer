# Copilot Instructions for SocketServer

## Project Overview
- 이 프로젝트는 C#, SuperSocketLite를 이용하여 오목 게임을 위한 소켓 서버를 구현하는 것을 목적으로 합니다.
- 이 프로젝트는 소켓 기반 게임서버 학습을 위한 것입니다.
- 클라이언트, 서버 모두 구현할 것이고, 각각 다른 폴더에 위치할 것입니다. (서버 : SocketServer, 클라이언트 : Client)
- 현재 이 폴더 자체는 SocketServer 폴더입니다.(서버는 다른 폴더에서 작업중입니다)
- 서버는 TCP 소켓을 사용하여 클라이언트와 통신합니다.
- 서버는 SuperSocketLite 라이브러리를 사용하여 소켓 서버 기능을 구현합니다.
- 클라이언트는 윈폼 기반으로 GUI를 구성할 것입니다. 
- Core dependencies: [SuperSocketLite](https://www.nuget.org/packages/SuperSocketLite) for socket server functionality,
- Main entry point: `Program.cs`.
- Project file: `SocketServer.csproj` manages dependencies and build configuration.

## Conventions
- Use PascalCase for class and method names, camelCase for local variables and parameters.
- 파일 하나당 네임스페이스 한 개 사용, 그러니까 {}사용 없이, 네임스페이스를 한줄로 선언
예시 : 
```csharp
namespace SocketServer;
``` 
- 필요시, 적절한 디렉토리를 만들어서 파일을 분리하고, 네임스페이스를 해당 디렉토리 이름으로 설정하세요.

## Integration Points
- SuperSocketLite handles TCP socket communication. Refer to its documentation for advanced scenarios.

## Current Progress

### Directory Structure
- **GameLogic/**: 오목 룰, 오목 판 등의 게임 로직을 포함합니다.
- **Handlers/**: 클라이언트 요청을 처리하고 응답을 전송하는 핸들러들을 포함합니다.
- **Packet/**: 패킷 프로세서, DTO, 및 패킷 관련 클래스들을 포함합니다.
- **Server/**: 

### Key Implementations
- **Room Management**:
  - `Room` class manages room state, including user lists and room properties.
  - `RoomHandler` (planned) will handle room-related notifications and packet broadcasting.
- **Packet Processing**:
  - `PacketProcessor` processes incoming packets and routes them to appropriate handlers.
- **Server Initialization**:
  - `MainServer` initializes and manages the server using SuperSocketLite.

### Naming Conventions
- Packet-related classes use prefixes like `PKT` (Packet), `Req` (Request), `Res` (Response), and `Ntf` (Notification) to indicate their purpose.
- DTOs are defined with `MessagePackObject` for serialization.

### Next Steps
- Finalize `RoomHandler` to handle room-specific notifications.
- Refactor and test packet processing logic for robustness.
- Integrate client-side communication for end-to-end testing.

---

For new features, follow the patterns in `Program.cs` and use SuperSocketLite APIs. If you add new files or directories, update this guide with new conventions or patterns.
