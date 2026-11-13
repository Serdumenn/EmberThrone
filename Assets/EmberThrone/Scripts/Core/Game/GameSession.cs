using System;
using System.Collections.Generic;

namespace EmberThrone.Core
{
    public enum GameState
    {
        Playing,
        Won,
        Lost,
    }

    public enum BoosterType
    {
        Hammer,
        RowBlast,
        ColumnBlast,
        Shuffle,
        ColorBrush,
    }

    public sealed partial class GameSession
    {
        readonly GameRules _rules;
        readonly LevelDefinition _level;
        readonly Board _board;
        readonly Pcg32 _rng;
        readonly List<Goal> _goals;
        readonly SessionStats _stats;
        int _movesLeft;
        GameState _state;
        int _collectiblesToSpawn;
        bool _spreadDue;
        bool _finaleDone;
        bool _frostBroken;

        MoveOutcome _outcome;
        BoardStep _step;
        readonly List<GridPos> _strikeTargets = new List<GridPos>();
        readonly HashSet<int> _blockerHits = new HashSet<int>();

        public GameSession(LevelDefinition level, int seed, GameRules rules = null)
        {
            _level = level ?? throw new ArgumentNullException(nameof(level));
            _rules = rules ?? new GameRules();
            _rng = new Pcg32(unchecked((ulong)seed));
            _board = BoardFactory.Create(level, _rng, _rules);
            _goals = Goal.CreateAll(level, _board);
            _stats = new SessionStats();
            _movesLeft = level.Moves;
            _state = GameState.Playing;
            _collectiblesToSpawn = level.Collectibles?.Total ?? 0;

            Begin();
            Settle();
            InitialOutcome = Finish(true);
        }

        GameSession(GameSession other, ulong? reseed)
        {
            _rules = other._rules;
            _level = other._level;
            _board = other._board.Clone();
            _rng = reseed.HasValue ? new Pcg32(reseed.Value) : other._rng.Clone();
            _goals = new List<Goal>(other._goals.Count);
            foreach (var g in other._goals) _goals.Add(g.Clone());
            _stats = other._stats.Clone();
            _movesLeft = other._movesLeft;
            _state = other._state;
            _collectiblesToSpawn = other._collectiblesToSpawn;
            _spreadDue = other._spreadDue;
            _frostBroken = other._frostBroken;
            _finaleDone = other._finaleDone;
            InitialOutcome = new MoveOutcome { Accepted = true, MovesLeft = _movesLeft, State = _state };
        }

        public Board Board => _board;
        public LevelDefinition Level => _level;
        public GameRules Rules => _rules;
        public int MovesLeft => _movesLeft;
        public GameState State => _state;
        public IReadOnlyList<Goal> Goals => _goals;
        public SessionStats Stats => _stats;
        public Pcg32 Random => _rng;

        public MoveOutcome InitialOutcome { get; private set; }

        public bool AllGoalsComplete
        {
            get
            {
                foreach (var g in _goals)
                    if (!g.IsComplete) return false;
                return true;
            }
        }

        public GameSession Clone(ulong? reseed = null) => new GameSession(this, reseed);

        public List<Move> FindValidMoves() => MoveFinder.FindAllMoves(_board);

        public Move? FindHint() => MoveFinder.FindHint(_board, _rules);

        public bool IsValidSwap(GridPos a, GridPos b) => _state == GameState.Playing && MoveFinder.IsValidSwap(_board, a, b);

        public MoveOutcome Apply(Move move)
        {
            return move.Type == MoveType.Tap ? TryTap(move.A) : TrySwap(move.A, move.B);
        }

        public MoveOutcome TrySwap(GridPos a, GridPos b)
        {
            Begin();
            if (_state != GameState.Playing || !_board.InBounds(a) || !_board.InBounds(b) || !a.IsAdjacent(b)) return Finish(false);
            var ca = _board.CellAt(a);
            var cb = _board.CellAt(b);
            if (!MoveFinder.IsSwappable(ca) || !MoveFinder.IsSwappable(cb)) return Finish(false);

            var pa = ca.Piece;
            var pb = cb.Piece;
            NewStep(StepKind.Swap);
            ca.Piece = pb;
            cb.Piece = pa;

            if (pa.IsSpecial || pb.IsSpecial)
            {
                Emit(new SwapEvent(a, b, false));
                ConsumeMove();
                Activation act;
                if (pa.IsSpecial && pb.IsSpecial) act = Activation.Combo(pa, b, pb, a);
                else if (pa.IsSpecial) act = Activation.Single(pa, b, pb.IsGem ? pb.Color : GemColor.None);
                else act = Activation.Single(pb, a, pa.IsGem ? pa.Color : GemColor.None);
                RunWaves(next => Execute(act, next));
                Settle();
                return Finish(true);
            }

            var groups = MatchFinder.FindGroups(_board);
            bool touches = false;
            foreach (var g in groups)
            {
                if (g.Contains(a) || g.Contains(b)) { touches = true; break; }
            }
            if (!touches)
            {
                ca.Piece = pa;
                cb.Piece = pb;
                Emit(new SwapEvent(a, b, true));
                return Finish(false);
            }

            Emit(new SwapEvent(a, b, false));
            ConsumeMove();
            var preferred = new[] { b, a };
            RunWaves(next => ApplyMatches(groups, preferred, next));
            Settle();
            return Finish(true);
        }

        public MoveOutcome TryTap(GridPos p)
        {
            Begin();
            if (_state != GameState.Playing || !_board.InBounds(p) || !MoveFinder.IsTappable(_board.CellAt(p))) return Finish(false);
            var piece = _board.CellAt(p).Piece;
            ConsumeMove();
            RunWaves(next => Execute(Activation.Single(piece, p, GemColor.None), next));
            Settle();
            return Finish(true);
        }

        public void AddMoves(int count)
        {
            if (count <= 0) return;
            _movesLeft += count;
            if (_state == GameState.Lost)
            {
                _state = GameState.Playing;
                _stats.ContinuesUsed++;
            }
        }

        void Begin()
        {
            _outcome = new MoveOutcome();
            _step = null;
        }

        MoveOutcome Finish(bool accepted)
        {
            var o = _outcome;
            o.Accepted = accepted;
            o.MovesLeft = _movesLeft;
            o.State = _state;
            o.Steps.RemoveAll(s => s.Events.Count == 0);
            if (o.ClearRounds > _stats.MaxClearRounds) _stats.MaxClearRounds = o.ClearRounds;
            _outcome = null;
            _step = null;
            return o;
        }

        BoardStep NewStep(StepKind kind)
        {
            _step = new BoardStep(kind);
            _outcome.Steps.Add(_step);
            return _step;
        }

        void Emit(BoardEvent e)
        {
            if (_step == null) NewStep(StepKind.Clear);
            _step.Events.Add(e);
        }

        void ConsumeMove()
        {
            _movesLeft--;
            _stats.MovesMade++;
            _spreadDue = true;
            _frostBroken = false;
        }

        void RunWaves(Action<List<Activation>> firstWave)
        {
            _outcome.ClearRounds++;
            NewStep(StepKind.Clear);
            _strikeTargets.Clear();
            _blockerHits.Clear();
            var next = new List<Activation>();
            firstWave(next);
            int guard = 0;
            while (next.Count > 0 && guard++ < 1000)
            {
                var current = next;
                next = new List<Activation>();
                NewStep(StepKind.Clear);
                _strikeTargets.Clear();
                _blockerHits.Clear();
                foreach (var act in current) Execute(act, next);
            }
        }

        void Settle()
        {
            for (int round = 0; round < _rules.MaxCascadeRounds; round++)
            {
                RunGravity();
                if (CollectAtExits()) continue;
                var groups = MatchFinder.FindGroups(_board);
                if (groups.Count == 0) break;
                RunWaves(next => ApplyMatches(groups, null, next));
            }
            EndOfAction();
        }

        void EndOfAction()
        {
            if (_state != GameState.Playing) return;
            if (_spreadDue)
            {
                _spreadDue = false;
                if (!_frostBroken && _movesLeft > 0 && !AllGoalsComplete) SpreadFrost();
            }
            if (AllGoalsComplete)
            {
                _state = GameState.Won;
                NewStep(StepKind.End);
                Emit(new GameEndedEvent(true));
                return;
            }
            if (_movesLeft <= 0)
            {
                _state = GameState.Lost;
                NewStep(StepKind.End);
                Emit(new GameEndedEvent(false));
                return;
            }
            EnsureMovesAvailable();
        }

        void EnsureMovesAvailable()
        {
            if (!_rules.ShuffleWhenNoMoves || MoveFinder.HasAnyMove(_board)) return;
            NewStep(StepKind.Shuffle);
            var ev = new ShuffleEvent();
            if (Shuffler.Shuffle(_board, _rng, _rules, _level.ColorCount, ev))
            {
                Emit(ev);
                _stats.Shuffles++;
                return;
            }
            var gems = FreeGems();
            if (gems.Count > 0)
            {
                ConvertToSpecial(gems[_rng.Next(gems.Count)], PieceKind.Bomb, false);
                return;
            }
            _state = GameState.Lost;
            NewStep(StepKind.End);
            Emit(new GameEndedEvent(false));
        }

        void RunGravity()
        {
            var step = new BoardStep(StepKind.Gravity);
            if (!Gravity.Run(_board, _rules, SpawnPiece, step.Events)) return;
            _outcome.Steps.Add(step);
            _step = step;
        }

        Piece SpawnPiece(GridPos at)
        {
            var settings = _level.Collectibles;
            if (settings != null && _collectiblesToSpawn > 0 &&
                _board.CountPieces(PieceKind.Collectible) < settings.MaxOnBoard &&
                _rng.NextDouble() < settings.SpawnChance)
            {
                _collectiblesToSpawn--;
                return _board.CreatePiece(PieceKind.Collectible);
            }
            return _board.CreatePiece(PieceKind.Gem, _rng.Next(_level.ColorCount));
        }

        bool CollectAtExits()
        {
            bool any = false;
            for (int x = 0; x < _board.Width; x++)
            {
                for (int y = 0; y < _board.Height; y++)
                {
                    if (!_board.IsExit(x, y)) continue;
                    var cell = _board.CellAt(x, y);
                    if (cell.Piece == null || cell.Piece.Kind != PieceKind.Collectible || cell.Cover > 0) continue;
                    if (!any) NewStep(StepKind.Collect);
                    any = true;
                    var pos = new GridPos(x, y);
                    Emit(new PieceCollectedEvent(cell.Piece.Id, pos));
                    cell.Piece = null;
                    Progress(GoalType.Collectible, GemColor.None, pos);
                }
            }
            return any;
        }

        void Progress(GoalType type, int color, GridPos source)
        {
            for (int i = 0; i < _goals.Count; i++)
            {
                var g = _goals[i];
                if (g.Type != type || g.IsComplete) continue;
                if (type == GoalType.Color && g.Color != color) continue;
                g.Remaining--;
                Emit(new GoalProgressEvent(i, g.Remaining, source));
                return;
            }
        }

        bool GoalActive(GoalType type)
        {
            foreach (var g in _goals)
                if (g.Type == type && !g.IsComplete) return true;
            return false;
        }

        bool ColorGoalActive(int color)
        {
            foreach (var g in _goals)
                if (g.Type == GoalType.Color && g.Color == color && !g.IsComplete) return true;
            return false;
        }

        static readonly GridPos[] Around = { new GridPos(0, 1), new GridPos(1, 0), new GridPos(0, -1), new GridPos(-1, 0) };

        void SpreadFrost()
        {
            var from = new List<GridPos>();
            var to = new List<GridPos>();
            for (int y = 0; y < _board.Height; y++)
                for (int x = 0; x < _board.Width; x++)
                {
                    if (!_board.CellAt(x, y).Frost) continue;
                    var source = new GridPos(x, y);
                    foreach (var d in Around)
                    {
                        var target = new GridPos(x + d.X, y + d.Y);
                        if (!_board.Exists(target)) continue;
                        var c = _board.CellAt(target);
                        if (c.Cover > 0 || c.Piece == null || c.Piece.Kind != PieceKind.Gem || c.Piece.Triggered) continue;
                        from.Add(source);
                        to.Add(target);
                    }
                }
            if (to.Count == 0) return;
            int pick = _rng.Next(to.Count);
            var cell = _board.CellAt(to[pick]);
            cell.Cover = 1;
            cell.Frost = true;
            _stats.FrostSpreads++;
            int goalIndex = -1, remaining = 0;
            for (int i = 0; i < _goals.Count; i++)
            {
                var g = _goals[i];
                if (g.Type != GoalType.Cover || !g.CountsAll) continue;
                g.Remaining++;
                goalIndex = i;
                remaining = g.Remaining;
                break;
            }
            NewStep(StepKind.Clear);
            Emit(new FrostSpreadEvent(from[pick], to[pick], goalIndex, remaining));
        }

        List<GridPos> FreeGems()
        {
            var list = new List<GridPos>();
            for (int y = 0; y < _board.Height; y++)
                for (int x = 0; x < _board.Width; x++)
                {
                    var c = _board.CellAt(x, y);
                    if (c.Exists && c.Cover == 0 && c.Piece != null && c.Piece.Kind == PieceKind.Gem && !c.Piece.Triggered)
                        list.Add(new GridPos(x, y));
                }
            return list;
        }
    }
}
