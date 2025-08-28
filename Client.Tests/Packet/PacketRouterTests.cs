using Client.Packet;
using Xunit;

namespace Client.Tests.Packet;

public class PacketRouterTests
{
    [Fact]
    public void Route_InvokesRegisteredHandler()
    {
        var router = new PacketRouter();
        bool handled = false;
        const short packetId = 1234;
        router.RegisterHandler(packetId, _ => handled = true);

        // Craft a minimal packet: [len(2)][id(2)][payload...]
        byte[] packet = new byte[8];
        System.BitConverter.GetBytes((short)8).CopyTo(packet, 0);
        System.BitConverter.GetBytes(packetId).CopyTo(packet, 2);

        router.Route(packet);

        Assert.True(handled);
    }
}

