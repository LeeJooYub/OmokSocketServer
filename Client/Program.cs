using System;
using System.Windows.Forms;
using Client.Forms;
using Client.Packet;
using MessagePack;
using Microsoft.VisualBasic.Logging;
namespace Client;

// 프로그램 진입점
static class Program
{
    [STAThread]
    static void Main()
    {
        var _ = NetworkManager.Instance;
        NetworkHandlerRegistrar.RegisterNetworkHandlers();

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}


public static class NetworkHandlerRegistrar
{
    public static string? UserId;
    public static Label? LoginStatusLabel;
    public static CancellationTokenSource? LoginCts;
    public static bool LoginSuccess;
    public static string ErrorMsg = "";

    public static RoomEnterForm? RoomEnterFormInstance;
    public static OmokBoardForm? OmokBoardFormInstance;

    public static void RegisterNetworkHandlers()
    {
        // 로그인 응답 패킷 핸들러 등록
        PacketRouter.Instance.RegisterHandler((short)PacketId.RES_LOGIN, OnLoginResponse);
        // 매칭 응답 패킷 핸들러 등록
        PacketRouter.Instance.RegisterHandler((short)PacketId.RES_MATCH_MAKE, OnMatchMakeResponse);

        PacketRouter.Instance.RegisterHandler((short)PacketId.NTF_GAME_PLAYER_MOVE, OnPlayerMove);
    }


    private static void OnPlayerMove(byte[] data)
    {
        LogHelper.Write($"[OmokBoardForm] 플레이어 움직임 핸들러 호출");
        // OmokBoardForm 인스턴스 찾기
        foreach (Form f in Application.OpenForms)
        {
            if (f is OmokBoardForm refForm)
            {
                OmokBoardFormInstance = refForm;
                break;
            }
        }
        if (OmokBoardFormInstance == null)
            return;

        string hex = BitConverter.ToString(data).Replace("-", " ");
        string logMsg = $"[OmokBoardForm][PacketReceived] ({data.Length} bytes): {hex}";
        LogHelper.Write(logMsg);

        if (BitConverter.ToInt16(data, 2) != (short)PacketId.NTF_GAME_PLAYER_MOVE)
        {
            logMsg = $"[OmokBoardForm] 잘못된 패킷 ID: {BitConverter.ToInt16(data, 2)}";
            return;
        }
        var res = MessagePackSerializer.Deserialize<PKTNtfGamePlayerMove>(data.Skip(5).ToArray());
        LogHelper.Write($"[OmokBoardForm] OnPlayerMove called: X={res.X}, Y={res.Y}, Turn={res.Turn}, IsGameEnd={res.IsGameEnd}, WinnerColor={res.WinnerColor}");
        OmokBoardFormInstance?.HandleMoveStone(data);
    }

    // 매칭 응답 핸들러
    private static void OnMatchMakeResponse(byte[] data)
    {
        LogHelper.Write($"[RoomEnterForm] 매칭 응답 패킷 수신");
        // RoomEnterForm 인스턴스 찾기
        foreach (Form f in Application.OpenForms)
        {
            if (f is RoomEnterForm refForm)
            {
                RoomEnterFormInstance = refForm;
                break;
            }
        }
        if (RoomEnterFormInstance == null)
            return;

        // 패킷 Hex 로그
        string hex = BitConverter.ToString(data).Replace("-", " ");
        LogHelper.Write($"[RoomEnterForm] 매칭 응답 패킷: {hex}");
        // 헤더 5바이트 이후 바디 역직렬화
        var res = MessagePackSerializer.Deserialize<PKTResMatchMake>(data.Skip(5).ToArray());
        if (res.Result == 0) // 성공
        {
            LogHelper.Write($"[RoomEnterForm] 매칭 성공: Color={res.Color}");
            char stone = res.Color == 'B' ? 'B' : 'W';
            RoomEnterFormInstance.HandleMatchResult(stone);
        }
        else
        {
            LogHelper.Write($"[RoomEnterForm] 매칭 실패: {res.Result}");
            RoomEnterFormInstance.HandleMatchFail($"매칭 실패: {res.Result}");
        }
    }

    private static void OnLoginResponse(byte[] data)
    {
        string hex = BitConverter.ToString(data).Replace("-", " ");
        string logMsg = $"[PacketReceived] ({data.Length} bytes): {hex}";
        LogHelper.Write(logMsg);
        MessageBox.Show(logMsg, "패킷 수신 (로그인 응답)", MessageBoxButtons.OK, MessageBoxIcon.Information);
        LogHelper.Write(logMsg);

        if (BitConverter.ToInt16(data, 2) != (short)PacketId.RES_LOGIN)
            return;
        var res = MessagePackSerializer.Deserialize<PKTResLogin>(data.Skip(5).ToArray());
        if (res.Result == 0)
        {
            LoginSuccess = true;
            if (LoginStatusLabel != null)
                LoginStatusLabel.Text = $"로그인 성공";
            LogHelper.Write($"로그인 성공");
        }
        else
        {
            ErrorMsg = $"로그인 실패 (에러코드: {res.Result})";
            if (LoginStatusLabel != null)
                LoginStatusLabel.Text = ErrorMsg;
            LogHelper.Write(ErrorMsg);
        }
        LoginCts?.Cancel();
    }
}

