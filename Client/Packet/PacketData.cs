using MessagePack; //https://github.com/neuecc/MessagePack-CSharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Packet;

// 패킷 관련 상수 정의
public class PacketDef
{
    public const Int16 HeaderSize = 5;
    public const int MaxUserIDByteLength = 16;
    public const int MaxUserPWByteLength = 16;
    public const int InvalidRoomNumber = -1;
}

// 패킷 클래스 객체를 바이트로 변환
public class PacketToBytes
{
    public static byte[] Make(PacketId packetID, byte[] bodyData)
    {
        byte dummy = 0;
        var PacketId = (Int16)packetID;


        Int16 bodyDataSize = 0;
        if (bodyData != null)
        {
            bodyDataSize = (Int16)bodyData.Length;
        }

        var totalPacketSize = (Int16)(bodyDataSize + PacketDef.HeaderSize);
        var dataSource = new byte[totalPacketSize];

        // 헤더 설정
        Buffer.BlockCopy(BitConverter.GetBytes(totalPacketSize), 0, dataSource, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(PacketId), 0, dataSource, 2, 2);
        dataSource[4] = dummy; // 더미 값

        // 바디 데이터 설정
        if (bodyData != null)
        {
            Buffer.BlockCopy(bodyData, 0, dataSource, 5, bodyDataSize);
        }

        return dataSource;
    }


}



// 로그인 요청
[MessagePackObject]
public class PKTReqLogin
{
    [Key(0)]
    public string UserID;
    [Key(1)]
    public string AuthToken;
}

[MessagePackObject]
public class PKTResLogin
{
    [Key(0)]
    public short Result;
}


[MessagePackObject]
public class PKNtfMustClose
{
    [Key(0)]
    public short Result;
}

[MessagePackObject]
public class PKTReqMatchMake
{
}

[MessagePackObject]
public class PKTResMatchMake
{
    [Key(0)]
    public short Result;
    [Key(2)]
    public char Color; // 'B' for Black, 'W' for White
}


[MessagePackObject]
public class PKTReqRoomEnter
{
    [Key(0)]
    public int RoomNumber;
}

[MessagePackObject]
public class PKTResRoomEnter
{
    [Key(0)]
    public short Result;
}

[MessagePackObject]
public class PKTNtfRoomUserList
{
    [Key(0)]
    public List<string> UserIDList = new List<string>();
}

[MessagePackObject]
public class PKTNtfRoomNewUser
{
    [Key(0)]
    public string UserID;
}


[MessagePackObject]
public class PKTReqRoomLeave
{
}

[MessagePackObject]
public class PKTResRoomLeave
{
    [Key(0)]
    public short Result;
}

[MessagePackObject]
public class PKTNtfRoomLeaveUser
{
    [Key(0)]
    public string UserID;
}


[MessagePackObject]
public class PKTReqRoomChat
{
    [Key(0)]
    public string ChatMessage;
}


[MessagePackObject]
public class PKTNtfRoomChat
{
    [Key(0)]
    public string UserID;

    [Key(1)]
    public string ChatMessage;
}











// 오목 게임 관련 패킷 정의 (기존 코드 아래에 추가)

[MessagePackObject]
public class PKTReqPutStone
{
    [Key(0)]
    public int X;
    [Key(1)]
    public int Y;
}


[MessagePackObject]
public class PKTNtfGamePlayerMove
{
    [Key(0)]
    public int X;
    [Key(1)]
    public int Y;
    [Key(2)]
    public int Turn; // 현재 턴 번호
    [Key(3)]
    public bool IsGameEnd;
    [Key(4)]
    public char WinnerColor;
}

[MessagePackObject]
public class PKTNtfGameStart
{
    [Key(0)]
    public short Result;
    [Key(1)]
    public char? MyStoneColor; // "흑돌" 또는 "백돌"
}

[MessagePackObject]
public class PKTNtfGameEnd
{
    [Key(0)]
    public string? WinnerUserID;
    [Key(1)]
    public string? Reason; // "5목 완성", "상대 기권" 등
}
