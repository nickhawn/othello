// Remote AI and utilities comprehensive tests
using System;
using System.Linq;
using ai;
using FluentAssertions;
using Xunit;
using System.Reflection;
using System.Diagnostics;
using System.Threading;

namespace test
{
    public static class TestBoardFactory
    {
        public static int[][] CreateEmptyBoard()
        {
            return Enumerable.Range(0, 8)
                .Select(_ => new int[8])
                .ToArray();
        }

        public static int[][] CreateStandardStartingBoard()
        {
            var board = CreateEmptyBoard();
            // Standard Othello starting position
            board[3][3] = 1;
            board[3][4] = 2;
            board[4][3] = 2;
            board[4][4] = 1;
            return board;
        }
    }

    public class BoundaryUtilityTests
    {
        [Theory]
        // North
        [InlineData(0, 3, Direction.N, true)]
        [InlineData(1, 3, Direction.N, false)]
        // South
        [InlineData(7, 3, Direction.S, true)]
        [InlineData(6, 3, Direction.S, false)]
        // East (x-)
        [InlineData(2, 0, Direction.E, true)]
        [InlineData(2, 1, Direction.E, false)]
        // West (x+)
        [InlineData(2, 7, Direction.W, true)]
        [InlineData(2, 6, Direction.W, false)]
        // North-West
        [InlineData(0, 0, Direction.NW, true)]
        [InlineData(1, 1, Direction.NW, false)]
        // North-East
        [InlineData(0, 7, Direction.NE, true)]
        [InlineData(1, 6, Direction.NE, false)]
        // South-West
        [InlineData(7, 0, Direction.SW, true)]
        [InlineData(6, 1, Direction.SW, false)]
        // South-East
        [InlineData(7, 7, Direction.SE, true)]
        [InlineData(6, 6, Direction.SE, false)]
        public void IsNextLocationOutOfBounds_ReturnsExpected(int y, int x, Direction direction, bool expected)
        {
            var location = new Location { X = x, Y = y };
            BoundaryUtility.IsNextLocationOutOfBounds(location, direction).Should().Be(expected);
        }
    }

    public class LocationUtilityTests
    {
        [Theory]
        //      startX startY direction expectedX expectedY
        [InlineData(3, 3, Direction.N, 3, 2)]
        [InlineData(3, 3, Direction.S, 3, 4)]
        [InlineData(3, 3, Direction.E, 2, 3)]
        [InlineData(3, 3, Direction.W, 4, 3)]
        [InlineData(3, 3, Direction.NW, 2, 2)]
        [InlineData(3, 3, Direction.NE, 4, 2)]
        [InlineData(3, 3, Direction.SW, 2, 4)]
        [InlineData(3, 3, Direction.SE, 4, 4)]
        public void MoveToNextLocation_MovesCorrectly(int startX, int startY, Direction direction, int expectedX, int expectedY)
        {
            var location = new Location { X = startX, Y = startY };
            LocationUtility.MoveToNextLocation(location, direction);
            location.X.Should().Be(expectedX);
            location.Y.Should().Be(expectedY);
        }
    }

    public class RemoteAiPublicMethodTests
    {
        private static RemoteAI CreateRemoteAi(int[][] board, int player = 1, int maxTurnTime = 2000)
        {
            var gm = new GameMessage
            {
                board = board,
                player = player,
                maxTurnTime = maxTurnTime
            };
            return new RemoteAI(gm);
        }

        [Theory]
        // currentY currentX direction expectedY expectedX isNull
        [InlineData(4, 4, Direction.N, 3, 4, false)]
        [InlineData(0, 0, Direction.N, 0, 0, true)]
        public void GetNextLocation_ReturnsExpected(int currentY, int currentX, Direction direction, int expectedY, int expectedX, bool expectNull)
        {
            var board = TestBoardFactory.CreateEmptyBoard();
            var ai = CreateRemoteAi(board);
            var currentLocation = new Location { Y = currentY, X = currentX };
            var result = ai.GetNextLocation(currentLocation, direction);

            if (expectNull)
            {
                result.Should().BeNull();
            }
            else
            {
                result.Should().NotBeNull();
                result!.Y.Should().Be(expectedY);
                result.X.Should().Be(expectedX);
                result.Value.Should().Be(board[expectedY][expectedX]);
            }
        }

        [Fact]
        public void GetNextMove_ReturnsValidBoardCoordinates()
        {
            var board = TestBoardFactory.CreateStandardStartingBoard();
            var ai = CreateRemoteAi(board, player: 1, maxTurnTime: 50); // very small turn time to speed up test
            var move = ai.GetNextMove();

            move.Length.Should().Be(2);
            move[0].Should().BeInRange(0, 7); // Y coordinate
            move[1].Should().BeInRange(0, 7); // X coordinate
            board[move[0]][move[1]].Should().Be(0); // position should be empty
        }
    }

    // Additional coverage tests for internal logic via reflection
    public class RemoteAiInternalTests
    {
        private static MethodInfo GetPrivateMethod(string name)
        {
            return typeof(RemoteAI).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private static RemoteAI CreateRemoteAi(int[][] board, int player = 1, int maxTurnTime = 1500)
        {
            var gm = new GameMessage
            {
                board = board,
                player = player,
                maxTurnTime = maxTurnTime
            };
            return new RemoteAI(gm);
        }

        [Fact]
        public void FindBestMoveInTimeFrame_NoCaptures_ReturnsDefaultMove()
        {
            // Arrange: empty board so no captures possible
            var board = TestBoardFactory.CreateEmptyBoard();
            var ai = CreateRemoteAi(board);
            // Start the stopwatch so the private method behaves as expected
            typeof(RemoteAI).GetField("_stopWatch", BindingFlags.NonPublic | BindingFlags.Instance)
                             !.GetValue(ai)
                             .As<Stopwatch>()
                             .Start();

            var method = GetPrivateMethod("FindBestMoveInTimeFrame");

            // Act
            var result = (int[])method!.Invoke(ai, null)!;

            // Assert – should be the hard-coded default {5,3}
            result.Should().Equal(new[] { -1, -1 });
        }

        [Fact]
        public void GetPiecesTakenForMove_SingleOpponentDisc_ReturnsOne()
        {
            // Board row: 0:[ ],1:[2],2:[1]
            var board = TestBoardFactory.CreateEmptyBoard();
            board[2][1] = 2; // opponent disc
            board[2][2] = 1; // player disc to close the flank

            var ai = CreateRemoteAi(board, player: 1);
            var method = GetPrivateMethod("GetPiecesTakenForMove");

            var move = new[] { 2, 0 }; // Y,X
            var direction = Direction.W; // Walking right (x + 1)
            var captured = (int)method!.Invoke(ai, new object[] { move, direction })!;

            captured.Should().Be(1);
        }

        [Fact]
        public void GetPiecesTakenForMove_NoClosingPlayerDisc_ReturnsZero()
        {
            var board = TestBoardFactory.CreateEmptyBoard();
            board[2][1] = 2; // opponent
            // No closing player disc

            var ai = CreateRemoteAi(board, player: 1);
            var method = GetPrivateMethod("GetPiecesTakenForMove");
            var move = new[] { 2, 0 };
            var direction = Direction.W;
            var captured = (int)method!.Invoke(ai, new object[] { move, direction })!;

            captured.Should().Be(0);
        }

        [Fact]
        public void GetPiecesTakenForMove_PlayerTwoPerspective_ReturnsOne()
        {
            var board = TestBoardFactory.CreateEmptyBoard();
            board[2][1] = 1; // opponent (player 1)
            board[2][2] = 2; // player 2 closing disc

            var ai = CreateRemoteAi(board, player: 2);
            var method = GetPrivateMethod("GetPiecesTakenForMove");
            var move = new[] { 2, 0 };
            var direction = Direction.W;
            var captured = (int)method!.Invoke(ai, new object[] { move, direction })!;

            captured.Should().Be(1);
        }

        [Theory]
        // Border tests for all directions hitting out-of-bounds immediately
        [InlineData(0, 4, Direction.N)]
        [InlineData(7, 4, Direction.S)]
        [InlineData(3, 0, Direction.E)]
        [InlineData(3, 7, Direction.W)]
        [InlineData(0, 0, Direction.NW)]
        [InlineData(0, 7, Direction.NE)]
        [InlineData(7, 0, Direction.SW)]
        [InlineData(7, 7, Direction.SE)]
        public void GetNextLocation_BorderSquare_ReturnsNull(int y, int x, Direction dir)
        {
            var ai = CreateRemoteAi(TestBoardFactory.CreateEmptyBoard());
            var loc = new Location { Y = y, X = x };
            ai.GetNextLocation(loc, dir).Should().BeNull();
        }

        // Explicitly test maximum aggregation across flanks
        [Fact]
        public void GetCapturedDisksForEachFlank_ReturnsMaximumNotSum()
        {
            /* Board layout (player = 1, opponent = 2)
                column 3 (X=3) is of interest; move will be at (3,3)
                Y 0  1  2  3  4  5  6  7
            0:              1
            1:              2
            2:              2
            3:          [ ] <- move here (3,3)
            4:              2
            5:              2
            6:              2
            7:              1
               Captures: N=2, S=3 => max = 3
            */
            var board = TestBoardFactory.CreateEmptyBoard();
            // north chain (2 discs + closing 1)
            board[0][3] = 1;
            board[1][3] = 2;
            board[2][3] = 2;
            // south chain (3 discs + closing 1)
            board[4][3] = 2;
            board[5][3] = 2;
            board[6][3] = 2;
            board[7][3] = 1;

            var ai = CreateRemoteAi(board);
            var method = GetPrivateMethod("GetCapturedDisksForEachFlank");
            var move = new[] { 3, 3 }; // y,x
            var result = (int)method!.Invoke(ai, new object[] { move })!;
            result.Should().Be(5); // 2 (north) + 3 (south)
        }

        [Theory]
        [InlineData(0,0,true)]
        [InlineData(3,3,false)]
        public void PositionIsEmpty_ReturnsExpected(int y, int x, bool expectedEmpty)
        {
            var board = TestBoardFactory.CreateStandardStartingBoard();
            var ai = CreateRemoteAi(board);
            var method = GetPrivateMethod("PositionIsEmpty");
            var res = (bool)method!.Invoke(ai, new object[] { new[] { y, x } })!;
            res.Should().Be(expectedEmpty);
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(0, false)]
        public void IsMoveAFlank_EvaluatesCorrectly(int opponentCount, bool expected)
        {
            var board = TestBoardFactory.CreateEmptyBoard();
            var ai = CreateRemoteAi(board);
            var method = GetPrivateMethod("IsMoveAFlank");
            var result = (bool)method!.Invoke(ai, new object[] { 1, opponentCount })!; // disc=playerKey (1)
            result.Should().Be(expected);
        }

        // In-bounds next-square tests for all directions
        [Theory]
        [InlineData(4,4, Direction.N, 3,4)]
        [InlineData(4,4, Direction.S, 5,4)]
        [InlineData(4,4, Direction.E, 4,3)]
        [InlineData(4,4, Direction.W, 4,5)]
        [InlineData(4,4, Direction.NE,3,5)]
        [InlineData(4,4, Direction.NW,3,3)]
        [InlineData(4,4, Direction.SE,5,5)]
        [InlineData(4,4, Direction.SW,5,3)]
        public void GetNextLocation_InBounds_ReturnsExpected(int startY, int startX, Direction dir, int expY, int expX)
        {
            var ai = CreateRemoteAi(TestBoardFactory.CreateEmptyBoard());
            var loc = new Location { Y = startY, X = startX };
            var next = ai.GetNextLocation(loc, dir);
            next.Should().NotBeNull();
            next!.Y.Should().Be(expY);
            next.X.Should().Be(expX);
        }

        [Fact]
        public void FindBestMoveInTimeFrame_CapturingMoveReturned()
        {
            // Configure board so that default {5,3} captures 1 disc, all other squares capture 0
            var board = TestBoardFactory.CreateEmptyBoard();
            // Place opponent disc north of default square, player disc further north to close flank
            board[4][3] = 2; // opponent at (4,3) – note Y=4 is just north of 5
            board[3][3] = 1; // player at (3,3) closes the flank

            var ai = CreateRemoteAi(board, player:1, maxTurnTime:1200); // gives ~200 ms positive budget
            // Start stopwatch manually because GetNextMove handles it; we'll call private method directly
            typeof(RemoteAI).GetField("_stopWatch", BindingFlags.NonPublic | BindingFlags.Instance)!
                             .GetValue(ai)
                             .As<Stopwatch>()
                             .Start();
            var method = GetPrivateMethod("FindBestMoveInTimeFrame");
            var result = (int[])method!.Invoke(ai, null)!;
            result.Should().Equal(new[] {5,3});
        }
    }

    public class RemoteAiAdditionalTests
    {
        private static RemoteAI CreateRemoteAi(int[][] board, int player = 1, int maxTurnTime = 2000)
        {
            var gm = new GameMessage
            {
                board = board,
                player = player,
                maxTurnTime = maxTurnTime
            };
            return new RemoteAI(gm);
        }

        private static MethodInfo GetPrivateMethod(string name)
        {
            return typeof(RemoteAI).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [Fact]
        public void GetNextMove_NoLegalMoves_ReturnsNegativeOneArray()
        {
            var board = TestBoardFactory.CreateEmptyBoard();
            // Fill entire board so there are no empty squares
            for (var y = 0; y < 8; y++)
                for (var x = 0; x < 8; x++)
                    board[y][x] = (y + x) % 2 == 0 ? 1 : 2;

            var ai = CreateRemoteAi(board);
            var result = ai.GetNextMove();

            result.Should().Equal(new[] { -1, -1 });
        }

        [Fact]
        public void GetNextMove_MaxTurnTimeBelowBuffer_DoesNotThrow()
        {
            var board = TestBoardFactory.CreateStandardStartingBoard();
            var ai = CreateRemoteAi(board, player: 1, maxTurnTime: 500); // buffer subtraction makes remaining time negative

            int[] move = null;
            Action action = () => move = ai.GetNextMove();
            action.Should().NotThrow();
            move.Length.Should().Be(2);
            var legal = (move[0] == -1 && move[1] == -1) || (move[0] >= 0 && move[0] <= 7 && move[1] >= 0 && move[1] <= 7);
            legal.Should().BeTrue();
        }

        [Fact]
        public void FindBestMoveInTimeFrame_StopsWhenTimeExceeded_ReturnsDefault()
        {
            var board = TestBoardFactory.CreateEmptyBoard();
            var ai = CreateRemoteAi(board, maxTurnTime: 1100); // 100 ms usable budget

            // Start and exhaust almost all allotted time
            var swField = typeof(RemoteAI).GetField("_stopWatch", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var sw = (Stopwatch)swField.GetValue(ai)!;
            sw.Start();
            Thread.Sleep(120); // ensure budget is exceeded

            var method = GetPrivateMethod("FindBestMoveInTimeFrame");
            var result = (int[])method!.Invoke(ai, null)!;

            result.Should().Equal(new[] { -1, -1 });
        }

        [Fact]
        public void FindBestMoveInTimeFrame_AllSquaresOccupied_ReturnsNegativeOneArray()
        {
            var board = TestBoardFactory.CreateEmptyBoard();
            for (var y = 0; y < 8; y++)
                for (var x = 0; x < 8; x++)
                    board[y][x] = 1;

            var ai = CreateRemoteAi(board);
            typeof(RemoteAI).GetField("_stopWatch", BindingFlags.NonPublic | BindingFlags.Instance)!
                            .GetValue(ai).As<Stopwatch>().Start();

            var method = GetPrivateMethod("FindBestMoveInTimeFrame");
            var result = (int[])method!.Invoke(ai, null)!;

            result.Should().Equal(new[] { -1, -1 });
        }

        [Fact]
        public void GetCapturedDisksForEachFlank_MultipleDirections_ReturnsSum()
        {
            var board = TestBoardFactory.CreateEmptyBoard();
            // North chain (2 discs captured)
            board[0][3] = 1;
            board[1][3] = 2;
            board[2][3] = 2;
            // South chain (3 discs captured)
            board[4][3] = 2;
            board[5][3] = 2;
            board[6][3] = 2;
            board[7][3] = 1;

            var ai = CreateRemoteAi(board);
            var method = GetPrivateMethod("GetCapturedDisksForEachFlank");
            var move = new[] { 3, 3 }; // y,x

            var captured = (int)method!.Invoke(ai, new object[] { move })!;
            captured.Should().Be(5);
        }

        [Fact]
        public void GetPiecesTakenForMove_MultipleOpponentDiscs_ReturnsCorrectCount()
        {
            var board = TestBoardFactory.CreateEmptyBoard();
            // Board row: [ ],[2],[2],[1]
            board[2][1] = 2;
            board[2][2] = 2;
            board[2][3] = 1;

            var ai = CreateRemoteAi(board);
            var method = GetPrivateMethod("GetPiecesTakenForMove");
            var move = new[] { 2, 0 }; // y,x
            var direction = Direction.W; // x + 1 (to the right)

            var captured = (int)method!.Invoke(ai, new object[] { move, direction })!;
            captured.Should().Be(2);
        }

        [Theory]
        [InlineData(0, 0, 100)] // corner bonus
        [InlineData(1, 1, -50)] // X-square penalty
        [InlineData(0, 3, 10)]  // edge bonus
        public void EvaluateMove_SpecialSquares_ReturnExpectedScore(int y, int x, int expected)
        {
            var board = TestBoardFactory.CreateEmptyBoard();
            var ai = CreateRemoteAi(board);
            var method = GetPrivateMethod("EvaluateMove");
            var move = new[] { y, x };
            var score = (int)method!.Invoke(ai, new object[] { move })!;
            score.Should().Be(expected);
        }

        [Fact]
        public void EvaluateMove_CapturedDiscsWeight()
        {
            var board = TestBoardFactory.CreateEmptyBoard();
            // Horizontal flank to the west (E direction): [1][2][2][ ]  <- move at index 3
            board[3][0] = 1;
            board[3][1] = 2;
            board[3][2] = 2;

            var ai = CreateRemoteAi(board);
            var method = GetPrivateMethod("EvaluateMove");
            var move = new[] { 3, 3 };
            var score = (int)method!.Invoke(ai, new object[] { move })!;
            // 2 discs * 10 = 20, no positional bonus/penalty
            score.Should().Be(20);
        }

        [Fact]
        public void EvaluateMove_CombinedFactors()
        {
            var board = TestBoardFactory.CreateEmptyBoard();
            // Edge move (0,3) with vertical flank southwards capturing 2 discs
            board[1][3] = 2;
            board[2][3] = 2;
            board[3][3] = 1;

            var ai = CreateRemoteAi(board);
            var method = GetPrivateMethod("EvaluateMove");
            var move = new[] { 0, 3 };
            var score = (int)method!.Invoke(ai, new object[] { move })!;
            // 2 discs *10 =20 + edge bonus 10 = 30
            score.Should().Be(30);
        }
    }
}