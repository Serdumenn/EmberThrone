using System.Collections.Generic;

namespace EmberThrone.Core
{
    public static class BoardFactory
    {
        public static Board Create(LevelDefinition level, Pcg32 rng, GameRules rules)
        {
            var board = new Board(level.Width, level.Height);
            var randomCells = new List<GridPos>();

            for (int y = 0; y < level.Height; y++)
            {
                for (int x = 0; x < level.Width; x++)
                {
                    var spec = level.GetCell(x, y);
                    var cell = board.CellAt(x, y);
                    cell.Exists = spec.Exists;
                    if (!spec.Exists) continue;
                    cell.Spawner = spec.Spawner;
                    cell.Floor = spec.Floor;
                    cell.Cover = spec.Cover;
                    switch (spec.Content)
                    {
                        case CellContent.Random:
                            randomCells.Add(new GridPos(x, y));
                            break;
                        case CellContent.Fixed:
                            cell.Piece = board.CreatePiece(spec.Kind, spec.Color, spec.Kind == PieceKind.Box ? System.Math.Max(1, spec.Hp) : 0);
                            break;
                    }
                }
            }
            board.FinalizeLayout();

            FillRandom(board, randomCells, level.ColorCount, rng);
            for (int attempt = 1; attempt < rules.MaxInitialFillAttempts && randomCells.Count > 0 && !MoveFinder.HasAnyMove(board); attempt++)
                FillRandom(board, randomCells, level.ColorCount, rng);

            return board;
        }

        public static void FillRandom(Board board, List<GridPos> cells, int colorCount, Pcg32 rng)
        {
            foreach (var p in cells) board.CellAt(p).Piece = null;
            foreach (var p in cells)
            {
                var cell = board.CellAt(p);
                var piece = board.CreatePiece(PieceKind.Gem, 0);
                cell.Piece = piece;
                int start = rng.Next(colorCount);
                for (int k = 0; k < colorCount; k++)
                {
                    piece.Color = (start + k) % colorCount;
                    if (!MatchFinder.HasMatchAt(board, p)) break;
                    if (k == colorCount - 1) piece.Color = start;
                }
            }
        }
    }
}
