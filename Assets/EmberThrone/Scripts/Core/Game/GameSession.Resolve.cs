using System;
using System.Collections.Generic;

namespace EmberThrone.Core
{
    enum ActivationType
    {
        Single,
        Combo,
        Strike,
    }

    sealed class Activation
    {
        public ActivationType Type;
        public Piece Piece;
        public GridPos Pos;
        public Piece Partner;
        public GridPos PartnerPos;
        public int TargetColor = GemColor.None;
        public PieceKind? Payload;

        public static Activation Single(Piece piece, GridPos pos, int targetColor)
        {
            return new Activation { Type = ActivationType.Single, Piece = piece, Pos = pos, TargetColor = targetColor };
        }

        public static Activation Combo(Piece moved, GridPos center, Piece partner, GridPos partnerPos)
        {
            return new Activation { Type = ActivationType.Combo, Piece = moved, Pos = center, Partner = partner, PartnerPos = partnerPos };
        }

        public static Activation Strike(GridPos origin, PieceKind? payload)
        {
            return new Activation { Type = ActivationType.Strike, Pos = origin, Payload = payload };
        }
    }

    public sealed partial class GameSession
    {
        void ApplyMatches(List<MatchGroup> groups, GridPos[] preferred, List<Activation> next)
        {
            foreach (var g in groups)
            {
                var special = MatchFinder.DetermineSpecial(g, _rules);
                Emit(new MatchEvent(g.Color, g.Cells.ToArray(), special));
                var removed = new List<GridPos>(g.Cells.Count);
                foreach (var p in g.Cells)
                {
                    var cell = _board.CellAt(p);
                    if (cell.Piece == null || cell.Piece.Kind != PieceKind.Gem) continue;
                    if (cell.Cover > 0)
                    {
                        DamageCover(p, cell);
                        continue;
                    }
                    RemovePiece(p, cell, ClearReason.Match);
                    DamageFloor(p, cell);
                    removed.Add(p);
                }

                DamageAdjacentBlockers(g);

                if (special.HasValue && removed.Count > 0)
                {
                    var pos = PickSpecialPosition(g, special.Value, preferred, removed);
                    var sp = _board.CreatePiece(special.Value);
                    _board.CellAt(pos).Piece = sp;
                    _stats.SpecialsCreated++;
                    Emit(new SpecialCreatedEvent(sp.Id, pos, special.Value, g.Cells.ToArray()));
                }
            }
        }

        static GridPos PickSpecialPosition(MatchGroup g, PieceKind kind, GridPos[] preferred, List<GridPos> removed)
        {
            if (preferred != null)
            {
                foreach (var p in preferred)
                    if (g.Contains(p) && removed.Contains(p)) return p;
            }
            GridPos candidate;
            switch (kind)
            {
                case PieceKind.Bomb: candidate = g.Intersection; break;
                case PieceKind.Homing: candidate = g.SquareOrigin; break;
                default: candidate = g.LongestRunCenter; break;
            }
            if (candidate.IsValid && removed.Contains(candidate)) return candidate;
            return removed[0];
        }

        void DamageAdjacentBlockers(MatchGroup g)
        {
            var done = new HashSet<GridPos>();
            foreach (var p in g.Cells)
            {
                for (int d = 0; d < 4; d++)
                {
                    var n = Neighbor(p, d);
                    if (!_board.Exists(n) || g.Contains(n) || !done.Add(n)) continue;
                    var cell = _board.CellAt(n);
                    if (cell.Piece != null && cell.Piece.Kind == PieceKind.Box && cell.Cover == 0)
                        DamageBlocker(n, cell, ClearReason.Match);
                }
            }
        }

        static GridPos Neighbor(GridPos p, int direction)
        {
            switch (direction)
            {
                case 0: return p.Up;
                case 1: return p.Right;
                case 2: return p.Down;
                default: return p.Left;
            }
        }

        void RemovePiece(GridPos p, Cell cell, ClearReason reason)
        {
            var piece = cell.Piece;
            cell.Piece = null;
            Emit(new PieceClearedEvent(piece.Id, p, piece.Kind, piece.Color, reason));
            if (piece.Kind == PieceKind.Gem) Progress(GoalType.Color, piece.Color, p);
        }

        void DamageFloor(GridPos p, Cell cell)
        {
            if (cell.Floor <= 0) return;
            cell.Floor--;
            Emit(new FloorDamagedEvent(p, cell.Floor));
            if (cell.Floor == 0) Progress(GoalType.Floor, GemColor.None, p);
        }

        void DamageCover(GridPos p, Cell cell)
        {
            if (cell.Cover <= 0) return;
            cell.Cover--;
            Emit(new CoverDamagedEvent(p, cell.Cover));
            if (cell.Cover == 0) Progress(GoalType.Cover, GemColor.None, p);
        }

        void DamageBlocker(GridPos p, Cell cell, ClearReason reason)
        {
            var piece = cell.Piece;
            piece.Hp--;
            if (piece.Hp > 0)
            {
                Emit(new PieceDamagedEvent(piece.Id, p, piece.Hp));
                return;
            }
            cell.Piece = null;
            Emit(new PieceClearedEvent(piece.Id, p, piece.Kind, piece.Color, reason));
            Progress(GoalType.Box, GemColor.None, p);
        }

        void Hit(GridPos p, ClearReason reason, List<Activation> next)
        {
            if (!_board.Exists(p)) return;
            var cell = _board.CellAt(p);
            if (cell.Cover > 0)
            {
                DamageCover(p, cell);
                return;
            }
            var piece = cell.Piece;
            if (piece == null)
            {
                DamageFloor(p, cell);
                return;
            }
            switch (piece.Kind)
            {
                case PieceKind.Gem:
                    RemovePiece(p, cell, reason);
                    DamageFloor(p, cell);
                    return;
                case PieceKind.Box:
                    DamageBlocker(p, cell, reason);
                    return;
                case PieceKind.Collectible:
                    return;
                default:
                    if (!piece.Triggered)
                    {
                        piece.Triggered = true;
                        next.Add(Activation.Single(piece, p, GemColor.None));
                    }
                    return;
            }
        }

        bool CanBeHit(GridPos p)
        {
            if (!_board.Exists(p)) return false;
            var cell = _board.CellAt(p);
            if (cell.Cover > 0) return true;
            if (cell.Piece == null) return cell.Floor > 0;
            return cell.Piece.Kind != PieceKind.Collectible;
        }

        Piece ConvertToSpecial(GridPos p, PieceKind kind, bool countAsCleared)
        {
            var cell = _board.CellAt(p);
            var old = cell.Piece;
            var sp = _board.CreatePiece(kind);
            cell.Piece = sp;
            Emit(new PieceConvertedEvent(old.Id, sp.Id, p, kind));
            if (countAsCleared && old.Kind == PieceKind.Gem) Progress(GoalType.Color, old.Color, p);
            return sp;
        }

        void Execute(Activation act, List<Activation> next)
        {
            switch (act.Type)
            {
                case ActivationType.Single: ExecuteSingle(act, next); break;
                case ActivationType.Combo: ExecuteCombo(act, next); break;
                case ActivationType.Strike: ExecuteStrike(act, next); break;
            }
        }

        void ExecuteSingle(Activation act, List<Activation> next)
        {
            var piece = act.Piece;
            var pos = act.Pos;
            var cell = _board.CellAt(pos);
            if (cell.Piece != piece) return;
            cell.Piece = null;
            piece.Triggered = false;
            _stats.SpecialsActivated++;

            List<GridPos> area;
            int color = GemColor.None;
            switch (piece.Kind)
            {
                case PieceKind.RocketH:
                    area = Line(pos, true);
                    break;
                case PieceKind.RocketV:
                    area = Line(pos, false);
                    break;
                case PieceKind.Bomb:
                    area = Square(pos, _rules.BombRadius);
                    break;
                case PieceKind.Homing:
                    area = Plus(pos);
                    next.Add(Activation.Strike(pos, null));
                    break;
                case PieceKind.ColorBomb:
                    color = act.TargetColor >= 0 ? act.TargetColor : MostCommonColor();
                    area = ColorCells(pos, color);
                    break;
                default:
                    return;
            }

            Emit(new SpecialActivatedEvent(piece.Id, -1, pos, piece.Kind, ComboKind.None, area.ToArray(), color));
            foreach (var p in area) Hit(p, ClearReason.Explosion, next);
        }

        void ExecuteCombo(Activation act, List<Activation> next)
        {
            var a = act.Piece;
            var b = act.Partner;
            var center = act.Pos;
            var other = act.PartnerPos;
            var ca = _board.CellAt(center);
            var cb = _board.CellAt(other);
            if (ca.Piece != a || cb.Piece != b) return;
            ca.Piece = null;
            cb.Piece = null;
            _stats.SpecialsActivated += 2;
            _stats.Combos++;

            var combo = ClassifyCombo(a.Kind, b.Kind);
            List<GridPos> area;
            List<GridPos> convert = null;
            PieceKind convertKind = PieceKind.Gem;
            int color = GemColor.None;

            switch (combo)
            {
                case ComboKind.RocketRocket:
                    area = Union(Line(center, true), Line(center, false));
                    break;
                case ComboKind.RocketBomb:
                    area = Union(
                        Line(center, true), Line(center.Up, true), Line(center.Down, true),
                        Line(center, false), Line(center.Left, false), Line(center.Right, false));
                    break;
                case ComboKind.BombBomb:
                    area = Square(center, _rules.BombBombRadius);
                    break;
                case ComboKind.HomingHoming:
                    area = Plus(center);
                    for (int i = 0; i < _rules.HomingHomingStrikes; i++) next.Add(Activation.Strike(center, null));
                    break;
                case ComboKind.HomingRocket:
                    area = Plus(center);
                    next.Add(Activation.Strike(center, a.Kind.IsRocket() ? a.Kind : b.Kind));
                    break;
                case ComboKind.HomingBomb:
                    area = Plus(center);
                    next.Add(Activation.Strike(center, PieceKind.Bomb));
                    break;
                case ComboKind.ColorRocket:
                case ComboKind.ColorBomb:
                case ComboKind.ColorHoming:
                    area = new List<GridPos> { center, other };
                    color = MostCommonColor();
                    convertKind = a.Kind == PieceKind.ColorBomb ? b.Kind : a.Kind;
                    convert = color >= 0 ? FreeGemsOfColor(color) : new List<GridPos>();
                    break;
                default:
                    area = AllCells();
                    break;
            }

            Emit(new SpecialActivatedEvent(a.Id, b.Id, center, a.Kind, combo, area.ToArray(), color));

            if (convert != null)
            {
                foreach (var p in convert)
                {
                    var kind = convertKind;
                    if (kind.IsRocket()) kind = _rng.Next(2) == 0 ? PieceKind.RocketH : PieceKind.RocketV;
                    var sp = ConvertToSpecial(p, kind, true);
                    sp.Triggered = true;
                    next.Add(Activation.Single(sp, p, GemColor.None));
                }
            }

            foreach (var p in area) Hit(p, ClearReason.Combo, next);
        }

        static ComboKind ClassifyCombo(PieceKind a, PieceKind b)
        {
            int rockets = (a.IsRocket() ? 1 : 0) + (b.IsRocket() ? 1 : 0);
            bool HasKind(PieceKind k) => a == k || b == k;

            if (a == PieceKind.ColorBomb && b == PieceKind.ColorBomb) return ComboKind.ColorColor;
            if (HasKind(PieceKind.ColorBomb))
            {
                var partner = a == PieceKind.ColorBomb ? b : a;
                if (partner.IsRocket()) return ComboKind.ColorRocket;
                if (partner == PieceKind.Bomb) return ComboKind.ColorBomb;
                return ComboKind.ColorHoming;
            }
            if (rockets == 2) return ComboKind.RocketRocket;
            if (rockets == 1 && HasKind(PieceKind.Bomb)) return ComboKind.RocketBomb;
            if (a == PieceKind.Bomb && b == PieceKind.Bomb) return ComboKind.BombBomb;
            if (a == PieceKind.Homing && b == PieceKind.Homing) return ComboKind.HomingHoming;
            if (rockets == 1) return ComboKind.HomingRocket;
            return ComboKind.HomingBomb;
        }

        void ExecuteStrike(Activation act, List<Activation> next)
        {
            var target = SelectHomingTarget(act.Pos);
            if (!target.IsValid) return;
            _strikeTargets.Add(target);
            Emit(new HomingStrikeEvent(act.Pos, target, act.Payload));
            if (!act.Payload.HasValue)
            {
                Hit(target, ClearReason.Explosion, next);
                return;
            }
            var payload = act.Payload.Value;
            var area = payload == PieceKind.Bomb ? Square(target, _rules.BombRadius) : Line(target, payload == PieceKind.RocketH);
            foreach (var p in area) Hit(p, ClearReason.Explosion, next);
        }

        GridPos SelectHomingTarget(GridPos origin)
        {
            var best = GridPos.Invalid;
            int bestScore = 0, ties = 0;
            for (int y = 0; y < _board.Height; y++)
            {
                for (int x = 0; x < _board.Width; x++)
                {
                    var p = new GridPos(x, y);
                    if (p == origin || _strikeTargets.Contains(p)) continue;
                    var cell = _board.CellAt(x, y);
                    if (!cell.Exists) continue;
                    int score = HomingScore(cell);
                    if (score <= 0) continue;
                    if (score > bestScore)
                    {
                        best = p;
                        bestScore = score;
                        ties = 1;
                    }
                    else if (score == bestScore)
                    {
                        ties++;
                        if (_rng.Next(ties) == 0) best = p;
                    }
                }
            }
            return best;
        }

        int HomingScore(Cell cell)
        {
            var piece = cell.Piece;
            if (piece != null && piece.Triggered) return 0;
            if (cell.Cover > 0) return GoalActive(GoalType.Cover) ? 90 : 20;
            if (piece == null) return cell.Floor > 0 && GoalActive(GoalType.Floor) ? 80 : 0;
            switch (piece.Kind)
            {
                case PieceKind.Box:
                    return GoalActive(GoalType.Box) ? 100 : 30;
                case PieceKind.Collectible:
                    return 0;
                case PieceKind.Gem:
                    if (cell.Floor > 0 && GoalActive(GoalType.Floor)) return 80;
                    return ColorGoalActive(piece.Color) ? 50 : 10;
                default:
                    return 5;
            }
        }

        int MostCommonColor()
        {
            var counts = new int[GemColor.Count];
            for (int y = 0; y < _board.Height; y++)
                for (int x = 0; x < _board.Width; x++)
                {
                    var c = _board.CellAt(x, y);
                    if (c.Exists && c.Piece != null && c.Piece.Kind == PieceKind.Gem && !c.Piece.Triggered) counts[c.Piece.Color]++;
                }
            int best = GemColor.None, bestCount = 0;
            for (int i = 0; i < counts.Length; i++)
            {
                if (counts[i] > bestCount)
                {
                    best = i;
                    bestCount = counts[i];
                }
            }
            return best;
        }

        List<GridPos> Line(GridPos c, bool horizontal)
        {
            var list = new List<GridPos>();
            int len = horizontal ? _board.Width : _board.Height;
            int origin = horizontal ? c.X : c.Y;
            int fixedCoord = horizontal ? c.Y : c.X;
            if (horizontal ? (fixedCoord < 0 || fixedCoord >= _board.Height) : (fixedCoord < 0 || fixedCoord >= _board.Width)) return list;
            for (int d = 0; d < len; d++)
            {
                int lo = origin - d, hi = origin + d;
                if (lo >= 0 && lo < len) AddLine(list, horizontal, lo, fixedCoord);
                if (d > 0 && hi >= 0 && hi < len) AddLine(list, horizontal, hi, fixedCoord);
            }
            return list;
        }

        void AddLine(List<GridPos> list, bool horizontal, int v, int fixedCoord)
        {
            var p = horizontal ? new GridPos(v, fixedCoord) : new GridPos(fixedCoord, v);
            if (_board.Exists(p)) list.Add(p);
        }

        List<GridPos> Square(GridPos c, int radius)
        {
            var list = new List<GridPos>();
            for (int d = 0; d <= radius; d++)
                for (int y = c.Y - d; y <= c.Y + d; y++)
                    for (int x = c.X - d; x <= c.X + d; x++)
                    {
                        if (Math.Max(Math.Abs(x - c.X), Math.Abs(y - c.Y)) != d) continue;
                        if (_board.Exists(x, y)) list.Add(new GridPos(x, y));
                    }
            return list;
        }

        List<GridPos> Plus(GridPos c)
        {
            var list = new List<GridPos>(5);
            if (_board.Exists(c)) list.Add(c);
            for (int d = 0; d < 4; d++)
            {
                var n = Neighbor(c, d);
                if (_board.Exists(n)) list.Add(n);
            }
            return list;
        }

        List<GridPos> ColorCells(GridPos origin, int color)
        {
            var list = new List<GridPos> { origin };
            if (color < 0) return list;
            for (int y = 0; y < _board.Height; y++)
                for (int x = 0; x < _board.Width; x++)
                {
                    var c = _board.CellAt(x, y);
                    if (c.Exists && c.Piece != null && c.Piece.Kind == PieceKind.Gem && c.Piece.Color == color && !c.Piece.Triggered)
                        list.Add(new GridPos(x, y));
                }
            return list;
        }

        List<GridPos> FreeGemsOfColor(int color)
        {
            var list = new List<GridPos>();
            for (int y = 0; y < _board.Height; y++)
                for (int x = 0; x < _board.Width; x++)
                {
                    var c = _board.CellAt(x, y);
                    if (c.Exists && c.Cover == 0 && c.Piece != null && c.Piece.Kind == PieceKind.Gem && c.Piece.Color == color && !c.Piece.Triggered)
                        list.Add(new GridPos(x, y));
                }
            return list;
        }

        List<GridPos> AllCells()
        {
            var list = new List<GridPos>();
            for (int y = 0; y < _board.Height; y++)
                for (int x = 0; x < _board.Width; x++)
                    if (_board.Exists(x, y)) list.Add(new GridPos(x, y));
            return list;
        }

        static List<GridPos> Union(params List<GridPos>[] parts)
        {
            var seen = new HashSet<GridPos>();
            var list = new List<GridPos>();
            foreach (var part in parts)
                foreach (var p in part)
                    if (seen.Add(p)) list.Add(p);
            return list;
        }
    }
}
