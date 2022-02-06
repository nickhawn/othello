namespace ai
{
    public class LocationUtility
    {
        public static void MoveToNextLocation(Location currentPiece, Direction direction)
        {
            switch (direction)
            {
                case Direction.N:
                    currentPiece.Y -= 1;
                    break;
                case Direction.S:
                    currentPiece.Y += 1;
                    break;
                case Direction.E:
                    currentPiece.X -= 1;
                    break;
                case Direction.W:
                    currentPiece.X += 1;
                    break;
                case Direction.NW:
                    currentPiece.X -= 1;
                    currentPiece.Y -= 1;
                    break;
                case Direction.NE:
                    currentPiece.X += 1;
                    currentPiece.Y -= 1;
                    break;
                case Direction.SW:
                    currentPiece.X -= 1;
                    currentPiece.Y += 1;
                    break;
                case Direction.SE:
                    currentPiece.X += 1;
                    currentPiece.Y += 1;
                    break;
            }
        }
    }
}
