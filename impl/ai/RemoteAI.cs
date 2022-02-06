using System;
using System.Diagnostics;
using System.Linq;

namespace ai
{
    public class RemoteAI
    {
        private readonly int[][] _board;
        private readonly int _playerKey;
        private readonly int _opponentKey;

        private readonly Stopwatch _stopWatch;
        private readonly int _maxTurnTimeInMilliseconds;

        public RemoteAI(GameMessage gameMessage)
        {
            _board = gameMessage.board;
            _playerKey = gameMessage.player;
            _opponentKey = _playerKey == 1 ? 2 : 1;
            _stopWatch = new Stopwatch();

            var bufferInMilliseconds = 1000;
            _maxTurnTimeInMilliseconds = gameMessage.maxTurnTime - bufferInMilliseconds;
        }

        public int[] GetNextMove()
        {
            _stopWatch.Start();
            return FindBestMoveInTimeFrame();
        }

        private int[] FindBestMoveInTimeFrame()
        {
            int[] bestMove = null;
            var bestScore = int.MinValue;

            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 8; x++)
                {
                    if (_maxTurnTimeInMilliseconds > 0 && _stopWatch.ElapsedMilliseconds >= _maxTurnTimeInMilliseconds)
                        break;

                    var move = new[] { y, x };

                    if (!PositionIsEmpty(move))
                        continue;

                    var captured = GetCapturedDisksForEachFlank(move);

                    // Only legal moves capture >= 1 opponent discs.
                    if (captured == 0)
                        continue;

                    var score = EvaluateMove(move);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = move;
                    }
                }
            }

            return bestMove ?? new[] { -1, -1 };
        }

        private int GetCapturedDisksForEachFlank(int[] move)
            => Enum.GetValues(typeof(Direction))
                   .Cast<Direction>()
                   .Sum(direction => GetPiecesTakenForMove(move, direction));

        private int GetPiecesTakenForMove(int[] move, Direction direction)
        {
            var nextPosition = GetNextLocation(new Location { Y = move[0], X = move[1] }, direction);
            var opponentsPieces = 0;

            while (nextPosition?.Value == _opponentKey)
            {
                nextPosition = GetNextLocation(nextPosition, direction);
                opponentsPieces++;
            }

            return IsMoveAFlank(nextPosition?.Value, opponentsPieces) ? opponentsPieces : 0;
        }

        public Location? GetNextLocation(Location currentPiece, Direction direction)
        {
            if (BoundaryUtility.IsNextLocationOutOfBounds(currentPiece, direction))
                return null;

            LocationUtility.MoveToNextLocation(currentPiece, direction);

            return GetBoardPosition(currentPiece.Y, currentPiece.X);
        }

        private bool PositionIsEmpty(int[] move)
            => GetBoardPosition(move[0], move[1]).Value == 0;

        private Location GetBoardPosition(int yPosition, int xPosition)
            => new Location
            {
                X = xPosition,
                Y = yPosition,
                Value = _board[yPosition][xPosition]
            };

        private bool IsMoveAFlank(int? disc, int opponentDiscCount)
           => disc == _playerKey && opponentDiscCount != 0;

        // Evaluates move quality using weighted scoring:
        // - Captured disks: +10 points each (immediate material gain)
        // - Corner positions: +100 points (stable, can't be flipped)
        // - X-squares (diagonal to corners): -50 points (risky, enable opponent corner access)
        // - Edge positions: +10 points (more stable than center)
        private int EvaluateMove(int[] move)
        {
            var y = move[0];
            var x = move[1];

            var score = 0;

            score += GetCapturedDisksForEachFlank(move) * 10;

            if (IsCornerPosition(y, x))
                score += 100;
            else if (IsXSquarePosition(y, x))
                score -= 50;
            else if (IsEdgePosition(y, x))
                score += 10;

            return score;
        }

        private bool IsCornerPosition(int y, int x)
            => (y == 0 || y == 7) && (x == 0 || x == 7);

        private bool IsXSquarePosition(int y, int x)
            => (y == 1 || y == 6) && (x == 1 || x == 6);

        private bool IsEdgePosition(int y, int x)
            => y == 0 || y == 7 || x == 0 || x == 7;
    }
}
