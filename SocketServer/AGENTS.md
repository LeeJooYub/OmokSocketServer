# Repository Guidelines (Server)

## Project Layout
- Entry: `Program.cs` starts the server using options in `ServerOption.cs`.
- Gameplay: `GameLogic/` (`OmokBoard`, `OmokRule`).
- Rooms: `Rooms/` and `Managers/` manage sessions and rooms.
- Packets: `Packet/` for IDs and processing.

## Build & Run
- Restore: `dotnet restore SocketServer/SocketServer.csproj`.
- Build: `dotnet build SocketServer/SocketServer.csproj -c Debug`.
- Run: `dotnet run --project SocketServer/SocketServer.csproj`.

## Coding Style
- C#: 4 spaces, UTF-8, one type per file.
- Names: PascalCase (public), camelCase (locals/params), `_camelCase` (private fields).
- Namespaces mirror folders (e.g., `SocketServer.GameLogic`).

## Testing (xUnit)
- Project: `SocketServer.Tests` (xUnit + coverlet).
- Sample tests: `GameLogic/OmokRuleTests.cs` validate five-in-a-row detection.
- Run tests: `dotnet test SocketServer.Tests/SocketServer.Tests.csproj`.
- Coverage: `dotnet test -p:CollectCoverage=true -p:CoverletOutputFormat=cobertura`.
- Targets: packet processing, room lifecycle, rule verification.

## PR & Commits
- Conventional prefixes: `refactor:`, `docs:`, `chore:`, `fix:`, `feat:`.
- PRs: summary, reasoning, linked issues; include steps to run server and test.

## Security & Config
- Configure ports/timeouts in `ServerOption.cs`; avoid secrets in code and logs.
- Log only non-PII around connections, rooms, and errors.

