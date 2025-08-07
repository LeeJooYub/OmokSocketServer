
namespace SocketServer.GameLogic;

public class OmokBoard
{
    // 오목판의 크기
    public const int BoardSize = 19;
    // 오목판을 나타내는 2차원 배열
    private int[,] _board = new int[BoardSize, BoardSize]; // 0: 빈 칸, 1: 플레이어 1의 돌(백), 2: 플레이어 2의 돌(흑)

    // 생성자
    public OmokBoard()
    {
        Initialize();
    }


    // 보드 초기화
    public void Initialize()
    {
        for (int i = 0; i < BoardSize; i++)
        {
            for (int j = 0; j < BoardSize; j++)
            {
                _board[i, j] = 0; // 빈 칸은 0으로 초기화
            }
        }
    }

    public int[,] Board
    {
        get { return _board; }
    }

    public void PlaceStone(int x, int y, int player)
    {
        if (x < 0 || x >= BoardSize || y < 0 || y >= BoardSize)
            throw new ArgumentOutOfRangeException("좌표가 오목판 범위를 벗어났습니다.");

        if (_board[x, y] != 0)
            throw new InvalidOperationException("이미 돌이 놓인 자리입니다.");

        _board[x, y] = player; // 플레이어의 돌을 놓는다
    }

    
}