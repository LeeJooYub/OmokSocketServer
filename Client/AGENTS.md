# AGENTS.md — Client (WinForms Omok)

## 1. Project Layout
- Entry point: `Program.cs` (registers network handlers, launches `MainForm`)
- UI: `Forms/RoomEnterForm.cs`, `Forms/OmokBoardForm.cs`, `MainForm.cs`
- Networking: `NetworkManager.cs` (TCP), `Packet/` (buffer, router, definitions)
- Packet: `Packet/PacketBufferManager.cs`, `Packet/PacketRouter.cs`, `Packet/PacketDefine.cs`, `Packet/PacketData.cs`

## 2. Build & Run
- Restore: `dotnet restore Client/Client.csproj`
- Build: `dotnet build Client/Client.csproj -c Debug`
- Run: `dotnet run --project Client/Client.csproj`

## 3. Networking & Packet Handling
- All network I/O is managed in `NetworkManager.cs`
- Packet routing: Register packet ID handlers in `PacketRouter.cs`
- Buffer management: Use `PacketBufferManager.cs` to split TCP stream into packets
- Packet definitions/constants: See `PacketDefine.cs`, `PacketData.cs`

## 4. UI & Events
- WinForms-based; always use async/event-driven code for UI
- UI forms: `MainForm`, `RoomEnterForm`, `OmokBoardForm`
- Network events must be handled safely on the UI thread (use `Invoke` if needed)

## 5. Testing
- Test project: `Client.Tests` (xUnit, coverlet)
- Target: non-UI logic (packet router, buffer, etc.)
- Run: `dotnet test Client.Tests/Client.Tests.csproj`
- Coverage: `dotnet test -p:CollectCoverage=true -p:CoverletOutputFormat=cobertura`

## 6. Coding Style
- C#: 4 spaces, UTF-8
- Naming: PascalCase (public), camelCase (locals/params), _camelCase (private fields)
- Never block the UI thread; all I/O must use async/await

## 7. PR & Commit Rules
- Commit prefixes: `feat:`, `fix:`, `refactor:`, `docs:`
- PRs: If UI changes, include screenshots/GIFs and describe impact


