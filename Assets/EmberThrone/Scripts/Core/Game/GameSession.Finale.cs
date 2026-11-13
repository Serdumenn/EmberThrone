using System;
using System.Collections.Generic;

namespace EmberThrone.Core
{
    public sealed partial class GameSession
    {
        public bool FinalePlayed => _finaleDone;

        public MoveOutcome PlayFinale()
        {
            Begin();
            if (_state != GameState.Won || _finaleDone) return Finish(false);
            _finaleDone = true;
            int count = Math.Min(_movesLeft, _rules.FinaleMaxSpecials);
            var gems = FreeGems();
            _rng.Shuffle(gems);
            var placed = new List<GridPos>();
            if (count > 0 && gems.Count > 0) NewStep(StepKind.Booster);
            for (int i = 0; i < count && i < gems.Count; i++)
            {
                ConvertToSpecial(gems[i], i % 2 == 0 ? PieceKind.RocketH : PieceKind.RocketV, false);
                placed.Add(gems[i]);
            }
            _stats.FinaleSpecials = placed.Count;
            if (placed.Count == 0) return Finish(true);
            RunWaves(next =>
            {
                foreach (var p in placed)
                {
                    var piece = _board.CellAt(p).Piece;
                    if (piece != null && piece.IsSpecial) Execute(Activation.Single(piece, p, GemColor.None), next);
                }
            });
            Settle();
            return Finish(true);
        }
    }
}
