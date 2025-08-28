# Repository Guidelines

## Project Structure & Module Organization
- Client: WinForms client app (entry: `Client/Program.cs`, forms under `Client/Forms`).
- SocketServer: Game server and core logic (entry: `SocketServer/Program.cs`; gameplay in `SocketServer/GameLogic`; rooms in `SocketServer/Rooms`; packets in `SocketServer/Packet`).
- Docs & assets: Root `ReadMe.md` and PNGs for UI; repo notes under `SocketServer/THE_STORY_OF_THIS_REPO.md` and `REPOSITORY_SUMMARY.md`.

## Other guides
- Follow the [Client](Client/AGENTS.md) for frontend work .
- Follow the [SocketServer](SocketServer/AGENTS.md) for backend work .


## Commit & Pull Request Guidelines
- Commits: imperative, scoped type prefixes seen in history: `refactor:`, `docs:`, `chore:`, `fix:`, `feat:`. Example: `refactor: reduce MainServer coupling in handler`.
- PRs: include summary, rationale, linked issue, and screenshots/GIFs for client UI. Describe how to run the server/client to validate.
- Size: keep PRs focused (<300 LOC when possible); include notes on risks and rollback.

## Security & Configuration Tips
- Server config: adjust ports/timeouts in `SocketServer/ServerOption.cs`; avoid hardcoding secrets.
- Logging: add non-PII logs around connection/room events; never log credentials or tokens.
