using System.Collections.Generic;

namespace EmberThrone.Core
{
    /// <summary>
    /// Resolution output. The presentation layer plays steps in order; events inside one step
    /// can animate at the same time. Core never waits for animations.
    /// </summary>
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
        /// <summary>False when the move was rejected (no move consumed).</summary>
        public bool Accepted;
        public int MovesLeft;
        public GameState State;
        /// <summary>Number of clear rounds (1 = no cascade).</summary>
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
        /// <summary>True when the swap made no match and was animated back.</summary>
        public readonly bool Reverted;
        public SwapEvent(GridPos a, GridPos b, bool reverted) { A = a; B = b; Reverted = reverted; }
    }

    public sealed class MatchEvent : BoardEvent
    {
        public readonly int Color;
        public readonly GridPos[] Cells;
        /// <summary>Special created by this match, if any.</summary>
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

    /// <summary>A blocker lost HP but survived.</summary>
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
        /// <summary>Cells of the match that formed it (for "gems fly together" animation).</summary>
        public readonly GridPos[] MergedFrom;
        public SpecialCreatedEvent(int pieceId, GridPos pos, PieceKind kind, GridPos[] mergedFrom)
        {
            PieceId = pieceId; Pos = pos; Kind = kind; MergedFrom = mergedFrom;
        }
    }

    public sealed class SpecialActivatedEvent : BoardEvent
    {
        public readonly int PieceId;
        /// <summary>Second piece of a combo, or -1.</summary>
        public readonly int PartnerPieceId;
        public readonly GridPos Pos;
        public readonly PieceKind Kind;
        public readonly ComboKind Combo;
        /// <summary>Cells hit by this activation, in hit order.</summary>
        public readonly GridPos[] Area;
        /// <summary>Color used by color bombs, else -1.</summary>
        public readonly int TargetColor;
        public SpecialActivatedEvent(int pieceId, int partnerPieceId, GridPos pos, PieceKind kind, ComboKind combo, GridPos[] area, int targetColor)
        {
            PieceId = pieceId; PartnerPieceId = partnerPieceId; Pos = pos; Kind = kind; Combo = combo; Area = area; TargetColor = targetColor;
        }
    }

    /// <summary>A gem was turned into a special (color bomb combos, starting boosters).</summary>
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
        /// <summary>Special carried to the target (Homing + Rocket/Bomb combos), or null.</summary>
        public readonly PieceKind? Payload;
        public HomingStrikeEvent(GridPos from, GridPos target, PieceKind? payload) { From = from; Target = target; Payload = payload; }
    }

    public readonly struct PathStep
    {
        /// <summary>Gravity tick at which the piece arrives at <see cref="Pos"/>.</summary>
        public readonly int Tick;
        public readonly GridPos Pos;
        public PathStep(int tick, GridPos pos) { Tick = tick; Pos = pos; }
        public override string ToString() => Tick + ":" + Pos;
    }

    public sealed class PieceMovedEvent : BoardEvent
    {
        public readonly int PieceId;
        public readonly GridPos From;
        /// <summary>One entry per cell moved (vertical or diagonal), with timing.</summary>
        public readonly List<PathStep> Path = new List<PathStep>();
        public PieceMovedEvent(int pieceId, GridPos from) { PieceId = pieceId; From = from; }
        public GridPos To => Path.Count > 0 ? Path[Path.Count - 1].Pos : From;
    }

    /// <summary>A new piece entered the board at a spawner cell (from above) at the given tick.</summary>
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
        /// <summary>Where the progress happened (for "fly to goal panel" effects).</summary>
        public readonly GridPos Source;
        public GoalProgressEvent(int goalIndex, int remaining, GridPos source) { GoalIndex = goalIndex; Remaining = remaining; Source = source; }
    }

    public sealed class GameEndedEvent : BoardEvent
    {
        public readonly bool Won;
        public GameEndedEvent(bool won) { Won = won; }
    }
}
