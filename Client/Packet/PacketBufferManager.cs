using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Packet;

/// <summary>
/// TCP 수신 버퍼에서 패킷 단위로 데이터를 조립/분리하는 클래스
/// - 여러 패킷이 한 번에 오거나, 한 패킷이 여러 번에 나눠서 올 수 있는 TCP 특성 대응
/// - Write로 데이터 누적, Read/TryGetPacket으로 완성된 패킷만 추출
/// </summary>
class PacketBufferManager
{
    int BufferSize = 0; // 전체 버퍼 크기
    int ReadPos = 0;    // 읽기 위치
    int WritePos = 0;   // 쓰기 위치

    int HeaderSize = 0;     // 패킷 헤더 크기(길이 정보)
    int MaxPacketSize = 0;  // 최대 패킷 크기
    byte[] PacketData;      // 실제 데이터 버퍼
    byte[] PacketDataTemp;  // 임시 버퍼(버퍼 relocate용)

    /// <summary>
    /// 버퍼 초기화
    /// </summary>
    public bool Init(int size, int headerSize, int maxPacketSize)
    {
        if (size < (maxPacketSize * 2) || size < 1 || headerSize < 1 || maxPacketSize < 1)
        {
            return false;
        }

        BufferSize = size;
        PacketData = new byte[size];
        PacketDataTemp = new byte[size];
        HeaderSize = headerSize;
        MaxPacketSize = maxPacketSize;

        return true;
    }

    /// <summary>
    /// 수신 데이터 버퍼에 누적
    /// </summary>
    public bool Write(byte[] data, int pos, int size)
    {
        if (data == null || (data.Length < (pos + size)))
        {
            return false;
        }

        var remainBufferSize = BufferSize - WritePos;

        if (remainBufferSize < size)
        {
            return false;
        }

        Buffer.BlockCopy(data, pos, PacketData, WritePos, size);
        WritePos += size;

        if (NextFree() == false)
        {
            BufferRelocate();
        }
        return true;
    }

    /// <summary>
    /// 버퍼에서 완성된 패킷(헤더+바디) 추출 (여러 번 호출 가능)
    /// </summary>
    public ArraySegment<byte> Read()
    {
        var enableReadSize = WritePos - ReadPos;

        if (enableReadSize < HeaderSize)
        {
            return new ArraySegment<byte>();
        }

        var packetDataSize = BitConverter.ToInt16(PacketData, ReadPos);
        if (enableReadSize < packetDataSize)
        {
            return new ArraySegment<byte>();
        }

        var completePacketData = new ArraySegment<byte>(PacketData, ReadPos, packetDataSize);
        ReadPos += packetDataSize;
        return completePacketData;
    }

    /// <summary>
    /// 버퍼에 완성된 패킷이 있으면 out으로 반환, 없으면 false
    /// </summary>
    public bool TryGetPacket(out byte[] packet)
    {
        packet = null;
        try
        {
            var enableReadSize = WritePos - ReadPos;
            if (enableReadSize < HeaderSize)
            {
                LogHelper.Write($"[Buffer] 데이터 부족: enableReadSize={enableReadSize}, HeaderSize={HeaderSize}");
                return false;
            }
            var packetDataSize = BitConverter.ToInt16(PacketData, ReadPos);
            if (packetDataSize < HeaderSize || packetDataSize > MaxPacketSize)
            {
                LogHelper.Write($"[Buffer] 잘못된 패킷 길이: {packetDataSize} (HeaderSize={HeaderSize}, MaxPacketSize={MaxPacketSize})");
                // 비정상 패킷: 버퍼 리셋
                ReadPos = WritePos;
                return false;
            }
            if (enableReadSize < packetDataSize)
            {
                LogHelper.Write($"[Buffer] 패킷 미도착: enableReadSize={enableReadSize}, packetDataSize={packetDataSize}");
                return false;
            }
            packet = new byte[packetDataSize];
            Buffer.BlockCopy(PacketData, ReadPos, packet, 0, packetDataSize);
            ReadPos += packetDataSize;
            LogHelper.Write($"[Buffer] 패킷 추출: Size={packetDataSize}, ReadPos={ReadPos}, WritePos={WritePos}");
            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Write($"[Buffer] TryGetPacket 예외: {ex.Message}\n{ex.StackTrace}");
            ReadPos = WritePos; // 버퍼 리셋(비정상 상황)
            return false;
        }
    }

    /// <summary>
    /// 버퍼에 다음 패킷을 쓸 공간이 충분한지 체크
    /// </summary>
    bool NextFree()
    {
        var enableWriteSize = BufferSize - WritePos;

        if (enableWriteSize < MaxPacketSize)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 버퍼 공간이 부족할 때, 읽지 않은 데이터만 앞으로 당김
    /// </summary>
    void BufferRelocate()
    {
        var enableReadSize = WritePos - ReadPos;

        Buffer.BlockCopy(PacketData, ReadPos, PacketDataTemp, 0, enableReadSize);
        Buffer.BlockCopy(PacketDataTemp, 0, PacketData, 0, enableReadSize);

        ReadPos = 0;
        WritePos = enableReadSize;
    }
}
