using SocketServer.GameLogic;
using Xunit;

namespace SocketServer.Tests.GameLogic;

public class OmokRuleTests
{
    [Fact]
    public void CheckWinCondition_HorizontalFive_ReturnsPlayer1()
    {
        var board = new OmokBoard();
        var rule = new OmokRule();

        for (int x = 0; x < 5; x++)
            board.PlaceStone(x, 0, 1);

        var winner = rule.CheckWinCondition(board, 4, 0);
        Assert.Equal(1, winner);
    }

    [Fact]
    public void CheckWinCondition_DiagonalFive_ReturnsPlayer2()
    {
        var board = new OmokBoard();
        var rule = new OmokRule();

        for (int i = 0; i < 5; i++)
            board.PlaceStone(i, i, 2);

        var winner = rule.CheckWinCondition(board, 4, 4);
        Assert.Equal(2, winner);
    }
}

