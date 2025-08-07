using System.Windows.Forms;
using Client.Packet;
using MessagePack;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace Client;

public static class LogHelper
{
    private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client.log");
    public static void Write(string message)
    {
        var log = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";
        File.AppendAllText(LogFilePath, log);
        Console.Write(log); // 터미널에도 출력
    }
}

public class MainForm : Form
{
    private Button btnRoomEnter;
    private Label label;
    private string userId;
    private Label lblLoginStatus;
    private CancellationTokenSource? loginCts;

    public MainForm()
    {
        this.Text = "Omok Client";
        this.Width = 600;
        this.Height = 600;

        label = new Label
        {
            Text = "오목 클라이언트 GUI (WinForms)",
            Left = 0,
            Top = 10,
            Width = this.ClientSize.Width,
            Height = 60,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            Font = new System.Drawing.Font("맑은 고딕", 16),
            Dock = DockStyle.None
        };
        this.Controls.Add(label);

        lblLoginStatus = new Label
        {
            Text = "",
            Left = 0,
            Top = 70,
            Width = this.ClientSize.Width,
            Height = 30,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            Font = new System.Drawing.Font("맑은 고딕", 12),
            Dock = DockStyle.None
        };
        this.Controls.Add(lblLoginStatus);

        btnRoomEnter = new Button
        {
            Text = "매칭",
            Width = 120,
            Height = 40,
            Top = 120,
            Left = (this.ClientSize.Width - 120) / 2,
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        btnRoomEnter.Click += BtnRoomEnter_Click;
        this.Controls.Add(btnRoomEnter);

        // 자동 로그인
        this.Load += MainForm_Load;
    }

    private async void MainForm_Load(object? sender, EventArgs e)
    {
        lblLoginStatus.Text = "로그인 중...";
        btnRoomEnter.Enabled = false;
        loginCts = new CancellationTokenSource();
        // 로그인 상태 공유 객체
        NetworkHandlerRegistrar.LoginSuccess = false;
        NetworkHandlerRegistrar.ErrorMsg = "";
        NetworkHandlerRegistrar.LoginStatusLabel = lblLoginStatus;
        NetworkHandlerRegistrar.LoginCts = loginCts;
        lblLoginStatus.Text = "서버 연결 및 로그인 시도";
        LogHelper.Write("서버 연결 및 로그인 시도");
        // 서버 연결 (이미 연결되어 있으면 생략)
        if (!NetworkManager.Instance.IsConnected)
        {
            await NetworkManager.Instance.ConnectAsync("127.0.0.1", 5000);
        }

        // 로그인 응답 핸들러는 Program.cs에서 등록된 RegisterNetworkHandlers를 통해 처리


        // 임시 UserID (8자리 랜덤)
        userId = Guid.NewGuid().ToString().Substring(0, 8);
        var req = new PKTReqLogin { UserID = userId, AuthToken = "" };
        var body = MessagePackSerializer.Serialize(req);
        var packet = PacketToBytes.Make(PacketId.REQ_LOGIN, body);
        await NetworkManager.Instance.SendAsync(packet);
        lblLoginStatus.Text = $"로그인 패킷 전송: {userId}";
        LogHelper.Write($"로그인 패킷 전송: {userId}");


        try
        {
            await Task.Delay(10000, loginCts.Token); // 10초 타임아웃
            if (!NetworkHandlerRegistrar.LoginSuccess)
            {
                NetworkHandlerRegistrar.ErrorMsg = "로그인 응답 없음 (타임아웃)";
                lblLoginStatus.Text = NetworkHandlerRegistrar.ErrorMsg;
            }
        }
        catch (TaskCanceledException) { }

        if (!NetworkHandlerRegistrar.LoginSuccess)
        {
            lblLoginStatus.Text = NetworkHandlerRegistrar.ErrorMsg;
            MessageBox.Show(NetworkHandlerRegistrar.ErrorMsg, "로그인 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
            LogHelper.Write("클라이언트 종료");
            Application.Exit();
        }
        else
        {
            lblLoginStatus.Text = "로그인 성공!";
            btnRoomEnter.Enabled = true;
        }
    }

    private void BtnRoomEnter_Click(object sender, System.EventArgs e)
    {
        var roomEnterForm = new Forms.RoomEnterForm();
        char? matchedStone = null;
        roomEnterForm.RoomEnterMatched += (stone) => matchedStone = stone;
        roomEnterForm.RoomEnterMatched += HideMainformUIWhenMatching;
        roomEnterForm.StartPosition = FormStartPosition.CenterParent;
        roomEnterForm.ShowDialog(this);
        // // 이벤트 핸들러 해제 (폼이 닫힌 뒤 반복 호출 방지)
        // roomEnterForm.RoomEnterMatched -= OnRoomEnterMatched;
        if (matchedStone.HasValue)
        {
            LogHelper.Write($"매칭 성공! 내 돌: {matchedStone.Value}");
            var boardForm = new Forms.OmokBoardForm(matchedStone.Value);
            boardForm.ShowDialog(this);

            if (boardForm.DialogResult == DialogResult.OK)
            {
                ShowMainformUIWhenMatching();
            }
        }
    }

    // private void OnRoomEnterMatched(char myStone)
    // {
    //     LogHelper.Write($"매칭 성공! 내 돌: {myStone}");
    //     var boardForm = new Forms.OmokBoardForm(myStone);
    //     boardForm.ShowDialog(this);
    // }
    // 매칭 관련 UI(버튼, 프로그레스바 등) 숨김/비활성화
    public void HideMainformUIWhenMatching(char myStone)
    {
        if (btnRoomEnter != null) btnRoomEnter.Visible = false;
    }

    public void ShowMainformUIWhenMatching()
    {
        if (btnRoomEnter != null) btnRoomEnter.Visible = true;
    }
    
}
