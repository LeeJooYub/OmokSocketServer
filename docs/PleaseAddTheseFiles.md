# 추가로 대화에 올려주세요

아래 파일이 없어 현재 클라이언트 빌드 시 컴파일 오류가 발생합니다. 해당 파일을 대화에 추가해 주세요. 추가 후 패킷 직렬화/전송 로직 점검 및 필요한 수정안을 드리겠습니다.

- Client\Packet\PacketData.cs
  - 포함 내용: PacketToBytes.Make 구현, [MessagePackObject] 모델들(PKTReqLogin, PKTResLogin, PKTReqMatchMake, PKTResMatchMake, PKTReqRoomLeave, PKTReqPutStone, PKTNtfGamePlayerMove 등)

위 파일을 추가해 주시면 다음 단계 수정을 계속 진행하겠습니다.
