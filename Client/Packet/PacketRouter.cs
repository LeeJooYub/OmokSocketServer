using System;
using System.Collections.Generic;
namespace Client.Packet;

public class PacketRouter
{
    private static PacketRouter? _instance;
    public static PacketRouter Instance => _instance ??= new PacketRouter();

    private readonly Dictionary<int, Action<byte[]>> _handlers = new();

    public void RegisterHandler(int packetId, Action<byte[]> handler)
    {
        _handlers[packetId] = handler;
    }

    public void UnregisterHandler(int packetId)
    {
        _handlers.Remove(packetId);
    }


    public void Route(byte[] packet)
    {
        if (packet == null || packet.Length < 5)
            return;
        short packetId = BitConverter.ToInt16(packet, 2);
        LogHelper.Write($"[PacketRouter] 패킷 ID: {packetId}");
        if (_handlers.TryGetValue(packetId, out var handler))
        {
            handler(packet);
        }
    }
}

