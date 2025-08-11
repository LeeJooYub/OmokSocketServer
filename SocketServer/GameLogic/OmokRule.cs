using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SocketServer.GameLogic;

public class OmokRule
{
    // 오목판 크기(15x15 등)
    const int BoardSize = 19;

    // 승리 조건 체크: 0이면 승부가 나지 않음, 1이면 플레이어1(백)의 승리, 2이면 플레이어2(흑)의 승리
    public int CheckWinCondition(OmokBoard board, int lastX, int lastY)
    {
        // 4방향: (dx, dy)
        int[,] directions = new int[,] { {1,0}, {0,1}, {1,1}, {1,-1} };

        for (int d = 0; d < 4; d++)
        {
            int count = 1;
            int dx = directions[d, 0];
            int dy = directions[d, 1];

            // 한 방향(+)
            count += CountDirection(board, lastX, lastY, dx, dy);
            // 반대 방향(-)
            count += CountDirection(board, lastX, lastY, -dx, -dy);

            if (count >= 5)
                return board.Board[lastX, lastY];
        }
        return 0;
    }

    // 한 방향으로 연속된 돌 개수 세기
    int CountDirection(OmokBoard board, int x, int y, int dx, int dy)
    {
        int count = 0;
        for (int i = 1; i < 5; i++)
        {
            int nx = x + dx * i;
            int ny = y + dy * i;
            if (nx < 0 || ny < 0 || nx >= BoardSize || ny >= BoardSize)
                break;
            if (board.Board[nx, ny] == board.Board[x, y])
                count++;
            else
                break;
        }
        return count;
    }
}
