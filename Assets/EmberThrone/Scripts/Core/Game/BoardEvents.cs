using System.Collections.Generic;

namespace EmberThrone.Core
{
    public enum StepKind
    {
        Swap,
        Clear,
        Gravity,
        Collect,
        Shuffle,
        Booster,
        End,
    }

    public enum ClearReason
    {
        Match,
        Explosion,
        Combo,
        Booster,
    }

    public enum ComboKind
    {
        None,
        RocketRocket,
        RocketBomb,
        BombBomb,
        HomingHoming,
        HomingRocket,
        HomingBomb,
        ColorRocket,
        ColorBomb,
        ColorHoming,
        ColorColor,
    }

    public sealed class BoardStep
    {
        public readonly StepKind Kind;
        public readonly List<BoardEvent> Events = new List<BoardEvent>();

        public BoardStep(StepKind kind) { Kind = kind; }

        public override string ToString() => Kind + "[" + Events.Count + "]";
    }

    public sealed class MoveOutcome
    {
        public bool Accepted;
        public int MovesLeft;
        public GameState State;
        public int ClearRounds;
        public readonly List<BoardStep> Steps = new List<BoardStep>();

        public IEnumerable<T> EventsOf<T>() where T : BoardEvent
        {
            foreach (var step in Steps)
                foreach (var e in step.Events)
                    if (e is T t) yield return t;
        }

        public int Count<T>() where T : BoardEvent
        {
            int n = 0;
            foreach (var unused in EventsOf<T>()) n++;
            return n;
        }
    }

    public abstract class BoardEvent { }

    public sealed class SwapEvent : BoardEvent
    {
        public readonly GridPos A, B;
        public readonly bool Reverted;
        public SwapEvent(GridPos a, GridPos b, bool reverted) { A = a; B = b; Reverted = reverted; }
    }

    public sealed class MatchEvent : BoardEvent
    {
        public readonly int Color;
        public readonly GridPos[] Cells;
        public readonly PieceKind? Special;
        public MatchEvent(int color, GridPos[] cells, PieceKind? special) { Color = color; Cells = cells; Special = special; }
    }

    public sealed class PieceClearedEvent : BoardEvent
    {
        public readonly int PieceId;
        public readonly GridPos Pos;
        public readonly PieceKind Kind;
        public readonly int Color;
        public readonly ClearReason Reason;
        public PieceClearedEvent(int pieceId, GridPos pos, PieceKind kind, int color, ClearReason reason)
        {
            PieceId = pieceId; Pos = pos; Kind = kind; Color = color; Reason = reason;
        }
    }

    public sealed class PieceDamagedEvent : BoardEvent
    {
        public readonly int PieceId;
        public readonly GridPos Pos;
        public readonly int HpLeft;
        public PieceDamagedEvent(int pieceId, GridPos pos, int hpLeft) { PieceId = pieceId; Pos = pos; HpLeft = hpLeft; }
    }

    public sealed class CoverDamagedEvent : BoardEvent
    {
        public readonly GridPos Pos;
        public readonly int HpLeft;
        public CoverDamagedEvent(GridPos pos, int hpLeft) { Pos = pos; HpLeft = hpLeft; }
    }

    public sealed class FrostSpreadEvent : BoardEvent
    {
        public readonly GridPos From;
        public readonly GridPos To;
        public readonly int GoalIndex;
        public readonly int Remaining;
        public FrostSpreadEvent(GridPos from, GridPos to, int goalIndex, int remaining) { From = from; To = to; GoalIndex = goalIndex; Remaining = remaining; }
    }

    public sealed class FloorDamagedEvent : BoardEvent
    {
        public readonly GridPos Pos;
        public readonly int HpLeft;
        public FloorDamagedEvent(GridPos pos, int hpLeft) { Pos = pos; HpLeft = hpLeft; }
    }

    public sealed class SpecialCreatedEvent : BoardEvent
    {
        public readonly int PieceId;
        public readonly GridPos Pos;
        public readonly PieceKind Kind;
        public readonly GridPos[] MergedFrom;
        public SpecialCreatedEvent(int pieceId, GridPos pos, PieceKind kind, GridPos[] mergedFrom)
        {
            PieceId = pieceId; Pos = pos; Kind = kind; MergedFrom = mergedFrom;
        }
    }

    public sealed class SpecialActivatedEvent : BoardEvent
    {
        public readonly int PieceId;
        public readonly int PartnerPieceId;
        public readonly GridPos Pos;
        public readonly PieceKind Kind;
        public readonly ComboKind Combo;
        public readonly GridPos[] Area;
        public readonly int TargetColor;
        public SpecialActivatedEvent(int pieceId, int partnerPieceId, GridPos pos, PieceKind kind, ComboKind combo, GridPos[] area, int targetColor)
        {
            PieceId = pieceId; PartnerPieceId = partnerPieceId; Pos = pos; Kind = kind; Combo = combo; Area = area; TargetColor = targetColor;
        }
    }

    public sealed class PieceConvertedEvent : BoardEvent
    {
        public readonly int OldPieceId;
        public readonly int NewPieceId;
        public readonly GridPos Pos;
        public readonly PieceKind NewKind;
        public PieceConvertedEvent(int oldPieceId, int newPieceId, GridPos pos, PieceKind newKind)
        {
            OldPieceId = oldPieceId; NewPieceId = newPieceId; Pos = pos; NewKind = newKind;
        }
    }

    public sealed class HomingStrikeEvent : BoardEvent
    {
        public readonly GridPos From;
        public readonly GridPos Target;
        public readonly PieceKind? Payload;
        public HomingStrikeEvent(GridPos from, GridPos target, PieceKind? payload) { From = from; Target = target; Payload = payload; }
    }

    public readonly struct PathStep
    {
        public readonly int Tick;
        public readonly GridPos Pos;
        public PathStep(int tick, GridPos pos) { Tick = tick; Pos = pos; }
        public override string ToString() => Tick + ":" + Pos;
    }

    public sealed class PieceMovedEvent : BoardEvent
    {
        public readonly int PieceId;
        public readonly GridPos From;
        public readonly List<PathStep> Path = new List<PathStep>();
        public PieceMovedEvent(int pieceId, GridPos from) { PieceId = pieceId; From = from; }
        public GridPos To => Path.Count > 0 ? Path[Path.Count - 1].Pos : From;
    }

    public sealed class PieceSpawnedEvent : BoardEvent
    {
        public readonly int PieceId;
        public readonly GridPos Pos;
        public readonly PieceKind Kind;
        public readonly int Color;
        public readonly int Tick;
        public PieceSpawnedEvent(int pieceId, GridPos pos, PieceKind kind, int color, int tick)
        {
            PieceId = pieceId; Pos = pos; Kind = kind; Color = color; Tick = tick;
        }
    }

    public sealed class PieceCollectedEvent : BoardEvent
    {
        public readonly int PieceId;
        public readonly GridPos Pos;
        public PieceCollectedEvent(int pieceId, GridPos pos) { PieceId = pieceId; Pos = pos; }
    }

    public readonly struct ShuffleEntry
    {
        public readonly int PieceId;
        public readonly GridPos From;
        public readonly GridPos To;
        public readonly int Color;
        public ShuffleEntry(int pieceId, GridPos from, GridPos to, int color) { PieceId = pieceId; From = from; To = to; Color = color; }
    }

    public sealed class ShuffleEvent : BoardEvent
    {
        public readonly List<ShuffleEntry> Entries = new List<ShuffleEntry>();
    }

    public sealed class GoalProgressEvent : BoardEvent
    {
        public readonly int GoalIndex;
        public readonly int Remaining;
        public readonly GridPos Source;
        public GoalProgressEvent(int goalIndex, int remaining, GridPos source) { GoalIndex = goalIndex; Remaining = remaining; Source = source; }
    }

    public sealed class GameEndedEvent : BoardEvent
    {
        public readonly bool Won;
        public GameEndedEvent(bool won) { Won = won; }
    }
}
