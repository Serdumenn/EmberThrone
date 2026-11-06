using System.Collections.Generic;

namespace EmberThrone.Core
{
    public static class Shuffler
    {
        public static bool Shuffle(Board board, Pcg32 rng, GameRules rules, int colorCount, ShuffleEvent ev)
        {
            var positions = new List<GridPos>();
            var pieces = new List<Piece>();
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var c = board.CellAt(x, y);
                    if (c.Exists && c.Cover == 0 && c.Piece != null && c.Piece.Kind == PieceKind.Gem)
                    {
                        positions.Add(new GridPos(x, y));
                        pieces.Add(c.Piece);
                    }
                }
            }
            if (positions.Count < 3) return false;

            var originalPos = new Dictionary<int, GridPos>();
            var originalColor = new Dictionary<int, int>();
            for (int i = 0; i < pieces.Count; i++)
            {
                originalPos[pieces[i].Id] = positions[i];
                originalColor[pieces[i].Id] = pieces[i].Color;
            }

            bool success = false;
            for (int attempt = 0; attempt < rules.MaxShuffleAttempts && !success; attempt++)
            {
                rng.Shuffle(pieces);
                for (int i = 0; i < pieces.Count; i++) board.CellAt(positions[i]).Piece = pieces[i];
                success = !MatchFinder.HasAnyMatch(board) && MoveFinder.HasAnyMove(board);
            }

            for (int attempt = 0; attempt < rules.MaxShuffleAttempts && !success; attempt++)
            {
                for (int i = 0; i < pieces.Count; i++)
                {
                    board.CellAt(positions[i]).Piece = pieces[i];
                    pieces[i].Color = -1;
                }
                for (int i = 0; i < pieces.Count; i++)
                {
                    int start = rng.Next(colorCount);
                    for (int k = 0; k < colorCount; k++)
                    {
                        pieces[i].Color = (start + k) % colorCount;
                        if (!MatchFinder.HasMatchAt(board, positions[i])) break;
                        if (k == colorCount - 1) pieces[i].Color = start;
                    }
                }
                success = !MatchFinder.HasAnyMatch(board) && MoveFinder.HasAnyMove(board);
            }

            if (!success)
            {
                foreach (var p in pieces)
                {
                    p.Color = originalColor[p.Id];
                    board.CellAt(originalPos[p.Id]).Piece = p;
                }
                return false;
            }

            for (int i = 0; i < pieces.Count; i++)
                ev.Entries.Add(new ShuffleEntry(pieces[i].Id, originalPos[pieces[i].Id], positions[i], pieces[i].Color));
            return true;
        }
    }
}
