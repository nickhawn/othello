namespace ai
{
    public static class BoundaryUtility
    {
        public static bool IsNextLocationOutOfBounds(Location currentPiece, Direction direction)
        {
            var nextLocationIsOutOfBounds = false;

            switch (direction)
            {
                case Direction.N:
                    if (currentPiece.Y == Boundaries.LowerLimit)
                        nextLocationIsOutOfBounds = true;
                    break;
                case Direction.S:
                    if (currentPiece.Y == Boundaries.UpperLimit)
                        nextLocationIsOutOfBounds = true;
                    break;
                case Direction.E:
                    if (currentPiece.X == Boundaries.LowerLimit)
                        nextLocationIsOutOfBounds = true;
                    break;
                case Direction.W:
                    if (currentPiece.X == Boundaries.UpperLimit)
                        nextLocationIsOutOfBounds = true;
                    break;
                case Direction.NW:
                    if (currentPiece.X == Boundaries.LowerLimit || currentPiece.Y == Boundaries.LowerLimit)
                        nextLocationIsOutOfBounds = true;
                    break;
                case Direction.NE:
                    if (currentPiece.X == Boundaries.UpperLimit || currentPiece.Y == Boundaries.LowerLimit)
                        nextLocationIsOutOfBounds = true;
                    break;
                case Direction.SW:
                    if (currentPiece.X == Boundaries.LowerLimit || currentPiece.Y == Boundaries.UpperLimit)
                        nextLocationIsOutOfBounds = true;
                    break;
                case Direction.SE:
                    if (currentPiece.X == Boundaries.UpperLimit || currentPiece.Y == Boundaries.UpperLimit)
                        nextLocationIsOutOfBounds = true;
                    break;
            }

            return nextLocationIsOutOfBounds;
        }
    }
}
