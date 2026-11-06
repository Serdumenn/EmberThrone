using System.Collections.Generic;

namespace EmberThrone.Core
{
    public sealed class BoosterUsedEvent : BoardEvent
    {
        public readonly BoosterType Type;
        public readonly GridPos Target;
        public BoosterUsedEvent(BoosterType type, GridPos target) { Type = type; Target = target; }
    }

    public sealed partial class GameSession
    {
        public MoveOutcome UseBooster(BoosterType type, GridPos target)
        {
            Begin();
            if (_state != GameState.Playing) return Finish(false);

            switch (type)
            {
                case BoosterType.Hammer:
                    if (!CanBeHit(target)) return Finish(false);
                    NewStep(StepKind.Booster);
                    Emit(new BoosterUsedEvent(type, target));
                    RunWaves(next => Hit(target, ClearReason.Booster, next));
                    break;

                case BoosterType.RowBlast:
                case BoosterType.ColumnBlast:
                {
                    if (!_board.InBounds(target)) return Finish(false);
                    var line = Line(target, type == BoosterType.RowBlast);
                    if (!line.Exists(CanBeHit)) return Finish(false);
                    NewStep(StepKind.Booster);
                    Emit(new BoosterUsedEvent(type, target));
                    RunWaves(next =>
                    {
                        foreach (var p in line) Hit(p, ClearReason.Booster, next);
                    });
                    break;
                }

                case BoosterType.Shuffle:
                {
                    var ev = new ShuffleEvent();
                    NewStep(StepKind.Shuffle);
                    if (!Shuffler.Shuffle(_board, _rng, _rules, _level.ColorCount, ev)) return Finish(false);
                    Emit(new BoosterUsedEvent(type, target));
                    Emit(ev);
                    _stats.BoostersUsed++;
                    _stats.Shuffles++;
                    EndOfAction();
                    return Finish(true);
                }

                default:
                    return Finish(false);
            }

            _stats.BoostersUsed++;
            Settle();
            return Finish(true);
        }

        public MoveOutcome PlaceStartingSpecials(IList<PieceKind> kinds)
        {
            Begin();
            if (_state != GameState.Playing || kinds == null) return Finish(false);
            NewStep(StepKind.Booster);
            var gems = FreeGems();
            _rng.Shuffle(gems);
            int used = 0;
            foreach (var kind in kinds)
            {
                if (!kind.IsSpecial() || used >= gems.Count) continue;
                ConvertToSpecial(gems[used++], kind, false);
            }
            return Finish(used > 0);
        }
    }
}
