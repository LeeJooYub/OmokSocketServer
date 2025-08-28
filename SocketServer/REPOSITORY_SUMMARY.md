# Repository Analysis: OmokSocketServer

## Overview
이 저장소는 온라인 오목 게임 서버를 구현한 프로젝트로, 실시간 멀티플레이어 게임 환경을 제공하기 위해 설계되었습니다. SuperSocketLite 기반의 네트워크 서버로, 방 관리, 유저 관리, 패킷 처리 등 게임 서버의 핵심 기능을 담당합니다.

## Architecture
- .NET 9.0 기반 콘솔 서버 애플리케이션
- SuperSocketLite를 활용한 TCP 네트워크 처리
- 주요 폴더 구조: GameLogic, Handlers, Managers, Packet, Rooms, Users
- 각 도메인별로 책임이 분리된 구조 (방, 유저, 패킷 등)
- 비동기 패킷 처리 및 이벤트 기반 네트워크 통신

## Key Components
- **MainServer**: 서버의 핵심 엔트리포인트, 네트워크 이벤트 및 패킷 분배 담당
- **RoomManager/Room**: 게임 방 생성, 관리, 입장/퇴장 등 방 관련 로직 처리
- **UserManager/User**: 유저 생성, 관리, 상태 추적 등 유저 관련 로직 처리
- **PacketProcessor**: 패킷 큐잉 및 핸들러 분배, 비동기 처리
- **Handlers**: 각 패킷ID별 실제 게임/방/유저 로직 처리
- **GameLogic**: 오목 게임 규칙 및 판정 로직 구현
- **ReceiveFilter**: SuperSocketLite용 패킷 파싱 필터
- **ServerOption**: 서버 설정 및 환경 변수 관리

## Technologies Used
- C# (.NET 9.0)
- SuperSocketLite (네트워크 프레임워크)
- MessagePack (패킷 직렬화)
- NLog (로깅)
- CommandLineParser (명령행 옵션)

## Data Flow
1. 클라이언트가 패킷 전송 → MainServer에서 수신
2. PacketProcessor에 패킷 큐잉 및 분배
3. 패킷ID별 핸들러에서 실제 게임/방/유저 로직 처리
4. 결과 패킷을 클라이언트에 전송

## Team and Ownership
- 주요 기여자: leejooyub, LeeJooYub, ljy
- 핵심 서버/네트워크 로직: leejooyub
- 방/유저/게임 로직: 공동 기여
- 유지보수 및 리팩토링: LeeJooYub
