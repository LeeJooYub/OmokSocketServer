using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Client.Packet;
using Microsoft.VisualBasic.Logging;

namespace Client;

public class NetworkManager
{
    private static NetworkManager? _instance;
    public static NetworkManager Instance => _instance ??= new NetworkManager();
    public String SessionId = null;

    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;
    public bool IsConnected => _client?.Connected ?? false;

    // 연결/해제 이벤트만 유지
    public event Action? Disconnected;
    public event Action? Connected;

    private PacketBufferManager _bufferManager = new();
    private PacketRouter _router = PacketRouter.Instance;

    private NetworkManager()
    {
        Console.WriteLine("NetworkManager 생성"); LogHelper.Write("NetworkManager 생성");
    _bufferManager.Init(4096, 5, 1024);
}

    public async Task<bool> ConnectAsync(string host, int port)
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(host, port);
            _stream = _client.GetStream();
            _cts = new CancellationTokenSource();
            StartReceiveLoop(_cts.Token);
            Connected?.Invoke();
            return true;
        }
        catch
        {
            _client = null;
            _stream = null;
            return false;
        }
    }

    public void Disconnect()
    {
        _cts?.Cancel();
        _stream?.Close();
        _client?.Close();
        _client = null;
        _stream = null;
        Disconnected?.Invoke();
    }

    public async Task SendAsync(byte[] data)
    {
        if (_stream != null && _client != null && _client.Connected)
        {
            await _stream.WriteAsync(data, 0, data.Length);
            LogHelper.Write($"[NetworkManager] 패킷 전송 ({data.Length} bytes): {BitConverter.ToString(data).Replace("-", " ")}");
        }
    }

    private async void StartReceiveLoop(CancellationToken token)
    {
        var buffer = new byte[4096];
        try
        {
            while (!token.IsCancellationRequested && _stream != null && _client != null && _client.Connected)
            {
                LogHelper.Write($"[NetworkManager] 패킷 수신 대기 중...");
                int read = await _stream.ReadAsync(buffer, 0, buffer.Length, token);
                LogHelper.Write($"[NetworkManager] 패킷 수신 ({read} bytes)");
                if (read > 0)
                {
                    try
                    {
                        _bufferManager.Write(buffer, 0, read);
                        LogHelper.Write($"[NetworkManager] 버퍼에 패킷 쓰기 성공");
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Write($"[NetworkManager] 버퍼 쓰기 오류: {ex.Message}");
                        continue;
                    }
                    while (_bufferManager.TryGetPacket(out var packet))
                    {
                        // 패킷 Hex 로그 출력
                        string hex = BitConverter.ToString(packet).Replace("-", " ");
                        Console.WriteLine($"[NetworkManager] PacketReceived ({packet.Length} bytes): {hex}");
                        LogHelper.Write($"[NetworkManager] PacketReceived ({packet.Length} bytes): {hex}");
                        _router.Route(packet); // 패킷 라우터로만 분배
                    }
                }
                else
                {
                    Console.WriteLine("서버 연결이 끊어졌습니다.");
                    LogHelper.Write("서버 연결이 끊어졌습니다.");
                    Disconnect();
                    break;
                }
            }
        }
        catch
        {
            Console.WriteLine("네트워크 오류 발생");
            LogHelper.Write("네트워크 오류 발생");
            Disconnect();
        }
    }
}
