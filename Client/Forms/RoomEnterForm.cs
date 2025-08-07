using System;
using System.Windows.Forms;
using Client.Packet;
using MessagePack;
using System.Threading.Tasks;
using Microsoft.VisualBasic.Logging;

namespace Client.Forms;

public class RoomEnterForm : Form
{


    public Label lblInfo;
    public Label lblStatus;
    public ProgressBar progressBar;
    private System.Windows.Forms.Timer matchTimer;
    private int countdown = 3;
    private int roomNumber;
    private char myStone = 'B';
    private CancellationTokenSource? cts;


    public event Action<char>? RoomEnterMatched;
    public RoomEnterForm()
    {
        this.Text = "방 입장";
        this.Width = 300;
        this.Height = 200;

        lblInfo = new Label
        {
            Text = "입장 중",
            Left = 10,
            Top = 20,
            Width = 100
        };

        lblStatus = new Label
        {
            Text = "",
            Left = 10,
            Top = 90,
            Width = 250,
            Height = 30,
            Visible = false
        };

        progressBar = new ProgressBar
        {
            Left = 10,
            Top = 120,
            Width = 250,
            Height = 20,
            Style = ProgressBarStyle.Marquee,
            Visible = false
        };

        this.Controls.Add(lblInfo);
        this.Controls.Add(lblStatus);
        this.Controls.Add(progressBar);

        // 폼이 뜨자마자 매칭 로직 자동 실행
        this.Shown += (s, e) => Initiate();
    }

    private async void Initiate()
    {

        lblStatus.Text = "매치 매이킹 중...";
        lblStatus.Visible = true;
        progressBar.Visible = true;

        // 방 입장 패킷 전송
        var req = new PKTReqMatchMake { };
        var body = MessagePackSerializer.Serialize(req);
        string bodyHex = BitConverter.ToString(body).Replace("-", " ");
        var packet = PacketToBytes.Make(PacketId.REQ_MATCH_MAKE, body);
        await NetworkManager.Instance.SendAsync(packet);

        // 10초 타임아웃 대기
        // cts = new System.Threading.CancellationTokenSource();
        // var timeoutTask = Task.Delay(10000, cts.Token);
        // bool matched = false;

        //     // ...생략...
        // try
        // {
        //     await timeoutTask;
        // }
        // catch (TaskCanceledException)
        // {
        //     // 매칭 성공 등으로 취소된 경우, 예외 무시
        // }
        // if (!matched)
        // {
        //     lblStatus.Text = "매칭이 잡히지 않습니다! (Timeout)";
        //     progressBar.Visible = false;
        // }
    }


    // private void MatchTimer_Tick(object? sender, EventArgs e)
    // {
    //     countdown--;
    //     lblStatus.Text = $"오목판 입장중입니다... ({countdown})";
    //     if (countdown == 0)
    //     {
    //         matchTimer.Stop();
    //         RoomEnterMatched?.Invoke(myStone);
    //         this.DialogResult = DialogResult.OK;
    //         this.Close();
    //     }
    // }

    // 매칭 성공 시 호출 (Program.cs에서 사용)
    public void HandleMatchResult(char stone)
    {
        LogHelper.Write($"[RoomEnterForm] HandleMatchResult called");
        if (InvokeRequired)
        {
            LogHelper.Write($"[RoomEnterForm] Invoking HandleMatchResult on UI thread");
            Invoke(new Action(() => HandleMatchResult(stone)));
            return;
        }
        // 오목판 진입 시 매칭 관련 UI 숨김/비활성화
        cts?.Cancel();
        RoomEnterMatched?.Invoke(stone);
        this.DialogResult = DialogResult.OK;
        this.Close();
    }

    // 매칭 실패 시 호출 (Program.cs에서 사용)
    public void HandleMatchFail(string errorMsg)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => HandleMatchFail(errorMsg)));
            return;
        }
        lblStatus.Text = errorMsg;
        progressBar.Visible = false;
    }
}
