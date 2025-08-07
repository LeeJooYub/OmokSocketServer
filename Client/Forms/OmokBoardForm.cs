using System;
using System.Drawing;
using System.Windows.Forms;
using Client.Packet;
using MessagePack;
using Microsoft.VisualBasic.Logging;

namespace Client.Forms;

public class OmokBoardForm : Form
{
    private const int BoardSize = 19;
    private const int CellSize = 32;
    private const int Margin = 40;
    private char playerStone;
    private Brush playerBrush;
    private char opponentStone;
    private Brush opponentBrush;
    private int[,] board = new int[BoardSize, BoardSize]; // 0: 없음, 1: 흑, 2: 백
    private int Turn = 1; // 0일 경우, 게임 시작 전. 1부터 시작. 홀수는 백, 짝수는 흑 차례


    public OmokBoardForm(char myStone)
    {
        this.Text = $"오목판";
        this.Width = BoardSize * CellSize + Margin * 2;
        this.Height = BoardSize * CellSize + Margin * 2 + 40;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.DoubleBuffered = true;
        this.MouseClick += OmokBoardForm_MouseClick;
        this.FormClosing += OmokBoardForm_FormClosing;
        


        playerStone = myStone;
        if (playerStone == 'B')
        {
            playerBrush = Brushes.Black;
            opponentStone = 'W';
            opponentBrush = Brushes.White;
        }
        else
        {
            playerBrush = Brushes.White;
            opponentStone = 'B';
            opponentBrush = Brushes.Black;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        var boardLeft = Margin;
        var boardTop = Margin;
        var boardRight = boardLeft + (BoardSize - 1) * CellSize;
        var boardBottom = boardTop + (BoardSize - 1) * CellSize;

        // 바둑판 그리기
        using (var pen = new Pen(Color.Black, 2))
        {
            for (int i = 0; i < BoardSize; i++)
            {
                g.DrawLine(pen, boardLeft, boardTop + i * CellSize, boardRight, boardTop + i * CellSize);
                g.DrawLine(pen, boardLeft + i * CellSize, boardTop, boardLeft + i * CellSize, boardBottom);
            }
        }
        // 플레이어 돌 색상 표시
        using (var font = new Font("맑은 고딕", 12, FontStyle.Bold))
        {
            g.DrawString($"내 돌: {playerStone}", font, playerStone == 'B' ? Brushes.Black : Brushes.Gray, 150, 10);
            g.FillEllipse(playerBrush, 230, 10, 20, 20);
            g.DrawEllipse(Pens.Black, 230, 10, 20, 20);
            g.DrawString($"상대 돌: {opponentStone}", font, opponentStone == 'B' ? Brushes.Black : Brushes.Gray, 270, 10);
            g.FillEllipse(opponentBrush, 360, 10, 20, 20);
            g.DrawEllipse(Pens.Black, 360, 10, 20, 20);
        }
        // 돌 그리기
        for (int y = 0; y < BoardSize; y++)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                if (board[x, y] == 1)
                {
                    g.FillEllipse(Brushes.Black, Margin + x * CellSize - 12, Margin + y * CellSize - 12, 24, 24);
                }
                else if (board[x, y] == 2)
                {
                    g.FillEllipse(Brushes.White, Margin + x * CellSize - 12, Margin + y * CellSize - 12, 24, 24);
                    g.DrawEllipse(Pens.Black, Margin + x * CellSize - 12, Margin + y * CellSize - 12, 24, 24);
                }
            }
        }
    }

    private void OmokBoardForm_MouseClick(object sender, MouseEventArgs e)
    {
        if (!IsMyTurn())
        {
            MessageBox.Show("상대의 차례입니다. 기다려주세요.");
            return;
        }
        var boardLeft = Margin;
        var boardTop = Margin;
        int x = (e.X - boardLeft + CellSize / 2) / CellSize;
        int y = (e.Y - boardTop + CellSize / 2) / CellSize;
        if (x >= 0 && x < BoardSize && y >= 0 && y < BoardSize)
        {
            // 서버에 착수 요청 전송
            SendPutStoneRequest(x, y);
            Invalidate();
        }
    }

    // 서버에 착수 요청 패킷 전송
    public async void SendPutStoneRequest(int x, int y)
    {
        var req = new PKTReqPutStone
        {
            X = x,
            Y = y
        };
        var body = MessagePackSerializer.Serialize(req);
        var packet = PacketToBytes.Make(PacketId.REQ_PUT_STONE, body);
        await NetworkManager.Instance.SendAsync(packet);
    }

    // 서버에서 착수 응답이 오면 호출
    public void HandleMoveStone(byte[] data)
    {
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
        if (res.IsGameEnd == false)
        {
            int x = res.X;
            int y = res.Y;
            //턴은 이미 서버에서 증가시켜서 넘어옴
            board[x,y]=Turn % 2 == 1 ? 2 : 1; // 백이면 1, 흑이면 2
            Turn = res.Turn;
            Invalidate();
        }
        else
        {
            int x = res.X;
            int y = res.Y;
            //턴은 이미 서버에서 증가시켜서 넘어옴
            board[x,y]=Turn % 2 == 1 ? 2 : 1; // 백이면 1, 흑이면 2
            Turn = res.Turn;
            Invalidate();
            GameEnd(res.WinnerColor);
        }
        Invalidate();
    }

    
    public void GameEnd(char winnerColor)
    {
        LogHelper.Write($"[OmokBoardForm] GameEnd called: WinnerColor={winnerColor}");
        string message = winnerColor == 'B' ? "흑이 승리하였습니다." : "백이 승리하였습니다.";
        MessageBox.Show(message);
        this.DialogResult = DialogResult.OK; // 반드시 추가!
        this.Close();
    }

    private async void OmokBoardForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        // 방 나가기 패킷 전송
        var req = new PKTReqRoomLeave();
        var body = MessagePackSerializer.Serialize(req);
        var packet = PacketToBytes.Make(PacketId.REQ_ROOM_LEAVE, body);
        await NetworkManager.Instance.SendAsync(packet);
    }

    // 내 차례인지 확인
    private bool IsMyTurn()
    {
        if (playerStone == 'W')
            return Turn % 2 == 1;
        else
            return Turn % 2 == 0 && Turn > 0;
    }

}
