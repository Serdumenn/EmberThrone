using System;
using System.Collections;
using System.Collections.Generic;
using EmberThrone.Core;
using UnityEngine;

namespace EmberThrone.Game
{
    /// <summary>
    /// Draws a board and plays MoveOutcome steps. It never changes the Board: it reads the board once
    /// to build its visuals and afterwards follows the events alone, keeping its own record of which
    /// piece is where. <see cref="FindMismatch"/> compares that record with the engine to catch desyncs.
    /// One world unit is one cell and the board is centered on the world origin.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        const int SortGlow = -3;
        const int SortBack = -2;
        const int SortTile = -1;
        const int SortFloor = 1;
        const int SortExit = 2;
        const int SortPiece = 10;
        const int SortLifted = 15;
        const int SortCover = 20;
        const int SortSelection = 25;

        sealed class PieceView
        {
            public int Id;
            public PieceKind Kind;
            public int Color;
            public int Hp;
            public GridPos Pos;
            public Transform T;
            public SpriteRenderer R;
        }

        /// <summary>Raised when a goal counter should change on screen, in sync with the animation.</summary>
        public event Action<GoalProgressEvent> GoalProgressed;
        /// <summary>Raised when something on the board should be felt (shake, vibration), at the moment it shows.</summary>
        public event Action<Impact> Impacted;
        /// <summary>Raised as each clear wave of a move starts playing: 1 is the move itself, 2+ are cascades.</summary>
        public event Action<int> WaveStarted;
        /// <summary>Raised when two specials are swapped into a combo.</summary>
        public event Action<ComboKind> ComboFired;

        BoardFx _fx;
        int _w, _h;
        PieceView[] _grid;
        SpriteRenderer[] _floors;
        SpriteRenderer[] _covers;
        int[] _floorHp;
        int[] _coverHp;
        bool[] _exists;
        readonly Dictionary<int, PieceView> _pieces = new Dictionary<int, PieceView>();
        readonly Stack<PieceView> _pool = new Stack<PieceView>();
        SpriteRenderer _selection;
        readonly Dictionary<GridPos, float> _delays = new Dictionary<GridPos, float>();
        readonly Dictionary<GridPos, GridPos> _mergeTargets = new Dictionary<GridPos, GridPos>();
        Transform _pieceRoot;

        public int Width => _w;
        public int Height => _h;

        // ---------------------------------------------------------------- building

        public void Build(GameSession session)
        {
            var board = session.Board;
            _w = board.Width;
            _h = board.Height;
            int n = _w * _h;
            _grid = new PieceView[n];
            _floors = new SpriteRenderer[n];
            _covers = new SpriteRenderer[n];
            _floorHp = new int[n];
            _coverHp = new int[n];
            _exists = new bool[n];

            _pieceRoot = new GameObject("Pieces").transform;
            _pieceRoot.SetParent(transform, false);
            var fxRoot = new GameObject("Fx").transform;
            fxRoot.SetParent(transform, false);
            _fx = new BoardFx(fxRoot, Art.SpriteMaterial);

            bool hasCollectibleGoal = false;
            foreach (var g in session.Goals)
                if (g.Type == GoalType.Collectible) hasCollectibleGoal = true;

            for (int y = 0; y < _h; y++)
            {
                for (int x = 0; x < _w; x++)
                {
                    var cell = board.CellAt(x, y);
                    if (!cell.Exists) continue;
                    var pos = new GridPos(x, y);
                    int i = Index(pos);
                    _exists[i] = true;
                    // Every cell brings its own back and light, so boards with holes keep their shape (style frame v3).
                    NewSprite("Glow", Art.Glow(), Palette.BoardGlow, World(pos), SortGlow).transform.localScale = Vector3.one * 1.42f;
                    NewSprite("Back", Art.CellBack(), Palette.BoardBack, World(pos), SortBack).transform.localScale = Vector3.one * 1.2f;
                    NewSprite("Tile", Art.Tile(), (x + y) % 2 == 0 ? Palette.TileLight : Palette.TileDark, World(pos), SortTile);
                    SetFloor(pos, cell.Floor);
                    SetCover(pos, cell.Cover);
                    if (cell.Piece != null) CreatePiece(cell.Piece.Id, cell.Piece.Kind, cell.Piece.Color, cell.Piece.Hp, pos);
                    if (hasCollectibleGoal && board.IsExit(x, y))
                    {
                        var exit = NewSprite("Exit", Art.Chevron(), Palette.Gold, World(pos) + Vector3.down * 0.6f, SortExit);
                        exit.transform.localScale = Vector3.one * 0.3f;
                    }
                }
            }

            _selection = NewSprite("Selection", Art.Selection(), Palette.Select, Vector3.zero, SortSelection);
            _selection.gameObject.SetActive(false);
        }

        SpriteRenderer NewSprite(string name, Sprite sprite, Color color, Vector3 pos, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            var r = go.AddComponent<SpriteRenderer>();
            r.sharedMaterial = Art.SpriteMaterial;
            r.sprite = sprite;
            r.color = color;
            r.sortingOrder = order;
            return r;
        }

        PieceView CreatePiece(int id, PieceKind kind, int color, int hp, GridPos pos)
        {
            PieceView v;
            if (_pool.Count > 0)
            {
                v = _pool.Pop();
                v.T.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject("Piece");
                go.transform.SetParent(_pieceRoot, false);
                v = new PieceView { T = go.transform, R = go.AddComponent<SpriteRenderer>() };
                v.R.sharedMaterial = Art.SpriteMaterial;
            }
            v.Id = id;
            v.Kind = kind;
            v.Color = color;
            v.Hp = hp;
            v.Pos = pos;
            v.R.sprite = Art.Piece(kind, color, hp);
            v.R.color = Color.white;
            v.R.sortingOrder = SortPiece;
            v.T.localPosition = World(pos);
            v.T.localScale = Vector3.one;
            _pieces[id] = v;
            _grid[Index(pos)] = v;
            return v;
        }

        void Recycle(PieceView v)
        {
            v.T.gameObject.SetActive(false);
            _pool.Push(v);
        }

        void SetFloor(GridPos p, int hp)
        {
            int i = Index(p);
            _floorHp[i] = hp;
            if (hp > 0)
            {
                if (_floors[i] == null) _floors[i] = NewSprite("Floor", Art.Floor(hp), Color.white, World(p), SortFloor);
                _floors[i].sprite = Art.Floor(hp);
            }
            else if (_floors[i] != null)
            {
                Destroy(_floors[i].gameObject);
                _floors[i] = null;
            }
        }

        void SetCover(GridPos p, int hp)
        {
            int i = Index(p);
            _coverHp[i] = hp;
            if (hp > 0)
            {
                if (_covers[i] == null) _covers[i] = NewSprite("Cover", Art.Cover(hp), new Color(1f, 1f, 1f, Palette.CoverAlpha), World(p), SortCover);
                _covers[i].sprite = Art.Cover(hp);
            }
            else if (_covers[i] != null)
            {
                Destroy(_covers[i].gameObject);
                _covers[i] = null;
            }
        }

        // ---------------------------------------------------------------- queries

        int Index(GridPos p) => p.Y * _w + p.X;

        bool InBounds(GridPos p) => p.X >= 0 && p.Y >= 0 && p.X < _w && p.Y < _h;

        public Vector3 World(GridPos p) => new Vector3(p.X - (_w - 1) * 0.5f, p.Y - (_h - 1) * 0.5f, 0f);

        /// <summary>The existing cell under a world position, or GridPos.Invalid.</summary>
        public GridPos CellAtWorld(Vector3 world)
        {
            var p = new GridPos(Mathf.RoundToInt(world.x + (_w - 1) * 0.5f), Mathf.RoundToInt(world.y + (_h - 1) * 0.5f));
            return InBounds(p) && _exists[Index(p)] ? p : GridPos.Invalid;
        }

        public bool IsSpecialAt(GridPos p)
        {
            return InBounds(p) && _grid[Index(p)] != null && _grid[Index(p)].Kind.IsSpecial() && _coverHp[Index(p)] == 0;
        }

        public bool IsGrabbableAt(GridPos p)
        {
            return InBounds(p) && _grid[Index(p)] != null && _grid[Index(p)].Kind.IsSwappable() && _coverHp[Index(p)] == 0;
        }

        public void SetSelection(GridPos p)
        {
            if (_selection == null) return;
            _selection.gameObject.SetActive(p.IsValid);
            if (p.IsValid) _selection.transform.localPosition = World(p);
        }

        /// <summary>Small lift while a finger holds a piece.</summary>
        public void SetLifted(GridPos p, bool lifted)
        {
            if (!InBounds(p)) return;
            var v = _grid[Index(p)];
            if (v == null) return;
            v.T.localScale = Vector3.one * (lifted ? 1.12f : 1f);
            v.R.sortingOrder = lifted ? SortLifted : SortPiece;
        }

        /// <summary>A little shake for a piece that cannot be moved.</summary>
        public void Nudge(GridPos p)
        {
            if (!InBounds(p)) return;
            var v = _grid[Index(p)];
            if (v == null) return;
            ShakePiece(v, 0f);
        }

        /// <summary>Null when the view matches the engine; otherwise a description of the first difference.</summary>
        public string FindMismatch(Board board)
        {
            for (int y = 0; y < _h; y++)
            {
                for (int x = 0; x < _w; x++)
                {
                    var cell = board.CellAt(x, y);
                    var p = new GridPos(x, y);
                    int i = Index(p);
                    var v = _grid[i];
                    var piece = cell.Piece;
                    if ((v == null) != (piece == null)) return "cell " + p + ": view " + Describe(v) + ", engine " + piece;
                    if (v != null && (v.Id != piece.Id || v.Kind != piece.Kind || v.Color != piece.Color || v.Hp != piece.Hp))
                        return "cell " + p + ": view " + Describe(v) + ", engine " + piece;
                    if (_floorHp[i] != cell.Floor) return "cell " + p + ": floor " + _floorHp[i] + " vs " + cell.Floor;
                    if (_coverHp[i] != cell.Cover) return "cell " + p + ": cover " + _coverHp[i] + " vs " + cell.Cover;
                }
            }
            return null;
        }

        static string Describe(PieceView v) => v == null ? "empty" : v.Kind + "#" + v.Id;

        // ---------------------------------------------------------------- playing an outcome

        public IEnumerator Play(MoveOutcome outcome)
        {
            int wave = 0;
            foreach (var step in outcome.Steps)
            {
                switch (step.Kind)
                {
                    case StepKind.Swap:
                        yield return PlaySwap(step);
                        break;
                    case StepKind.Gravity:
                        yield return PlayGravity(step);
                        break;
                    case StepKind.End:
                        break;
                    default:
                        WaveStarted?.Invoke(++wave);
                        yield return PlayEffects(step);
                        break;
                }
            }
        }

        void Signal(Impact impact, float delay)
        {
            if (Impacted == null) return;
            if (delay <= 0f) Impacted(impact);
            else Tweens.After(delay, () => Impacted?.Invoke(impact));
        }

        IEnumerator PlaySwap(BoardStep step)
        {
            foreach (var ev in step.Events)
            {
                if (!(ev is SwapEvent e)) continue;
                var a = _grid[Index(e.A)];
                var b = _grid[Index(e.B)];
                a.R.sortingOrder = SortLifted;
                a.T.localScale = Vector3.one;
                b.T.localScale = Vector3.one;
                MoveTo(a, World(e.B), Timing.Swap);
                MoveTo(b, World(e.A), Timing.Swap);
                yield return new WaitForSeconds(Timing.Swap);
                if (e.Reverted)
                {
                    MoveTo(a, World(e.A), Timing.Swap);
                    MoveTo(b, World(e.B), Timing.Swap);
                    yield return new WaitForSeconds(Timing.Swap);
                }
                else
                {
                    a.Pos = e.B;
                    b.Pos = e.A;
                    _grid[Index(e.A)] = b;
                    _grid[Index(e.B)] = a;
                }
                a.R.sortingOrder = SortPiece;
            }
        }

        void MoveTo(PieceView v, Vector3 to, float duration)
        {
            var from = v.T.localPosition;
            Tweens.Run(duration, t => v.T.localPosition = Vector3.LerpUnclamped(from, to, Ease.OutQuad(t)));
        }

        // ---------------------------------------------------------------- clears, specials, obstacles

        IEnumerator PlayEffects(BoardStep step)
        {
            // First pass: when does each cell get hit? Blasts travel outward, so far cells react later.
            _delays.Clear();
            _mergeTargets.Clear();
            float strikeDelay = 0f;
            int converted = 0;
            foreach (var ev in step.Events)
            {
                switch (ev)
                {
                    case SpecialActivatedEvent s:
                        float perCell = PerCell(s);
                        foreach (var cell in s.Area) SetEarliest(cell, s.Pos.Chebyshev(cell) * perCell);
                        break;
                    case HomingStrikeEvent h:
                        strikeDelay = Timing.StrikeFlight;
                        SetEarliest(h.Target, Timing.StrikeFlight);
                        break;
                    case SpecialCreatedEvent c:
                        foreach (var cell in c.MergedFrom)
                            if (cell != c.Pos) _mergeTargets[cell] = c.Pos;
                        break;
                    case PieceConvertedEvent conv:
                        SetEarliest(conv.Pos, 0.2f + 0.03f * converted++);
                        break;
                }
            }

            float DelayAt(GridPos p) => _delays.TryGetValue(p, out var d) ? d : strikeDelay;

            foreach (var ev in step.Events)
            {
                if (ev is PieceClearedEvent c && c.Reason == ClearReason.Match)
                {
                    Signal(Impact.Match, 0f);
                    break;
                }
            }

            // Second pass: update the record immediately and schedule the visuals.
            float end = 0f;
            foreach (var ev in step.Events)
            {
                float e = 0f;
                switch (ev)
                {
                    case PieceClearedEvent c:
                        e = ClearPiece(c, DelayAt(c.Pos));
                        break;
                    case SpecialActivatedEvent s:
                        e = Activate(s);
                        break;
                    case SpecialCreatedEvent c:
                        e = FormSpecial(c);
                        break;
                    case PieceConvertedEvent c:
                        e = ConvertPiece(c, DelayAt(c.Pos));
                        break;
                    case HomingStrikeEvent h:
                        e = _fx.Fly(h.Payload.HasValue ? Art.Piece(h.Payload.Value, GemColor.None, 0) : Art.Homing(),
                            World(h.From), World(h.Target), 0f, Timing.StrikeFlight);
                        break;
                    case PieceDamagedEvent d:
                        e = DamageBox(d, DelayAt(d.Pos));
                        break;
                    case CoverDamagedEvent d:
                        e = DamageCover(d, DelayAt(d.Pos));
                        break;
                    case FloorDamagedEvent d:
                        e = DamageFloor(d, DelayAt(d.Pos));
                        break;
                    case PieceCollectedEvent c:
                        e = CollectPiece(c);
                        break;
                    case ShuffleEvent s:
                        e = ShufflePieces(s);
                        break;
                    case GoalProgressEvent g:
                        var goal = g;
                        Tweens.After(DelayAt(g.Source), () => GoalProgressed?.Invoke(goal));
                        break;
                }
                end = Mathf.Max(end, e);
            }
            if (end > 0f) yield return new WaitForSeconds(end);
        }

        void SetEarliest(GridPos p, float delay)
        {
            if (!_delays.TryGetValue(p, out var d) || delay < d) _delays[p] = delay;
        }

        static float PerCell(SpecialActivatedEvent s)
        {
            switch (s.Combo)
            {
                case ComboKind.None:
                    if (s.Kind.IsRocket()) return Timing.RocketPerCell;
                    if (s.Kind == PieceKind.Bomb) return Timing.BombPerCell;
                    if (s.Kind == PieceKind.Homing) return Timing.HomingPerCell;
                    return Timing.ColorPerCell;
                case ComboKind.RocketRocket:
                case ComboKind.RocketBomb:
                    return Timing.RocketPerCell;
                case ComboKind.BombBomb:
                    return Timing.BombPerCell * 0.8f;
                case ComboKind.ColorColor:
                    return Timing.ColorPerCell;
                default:
                    return Timing.HomingPerCell;
            }
        }

        float ClearPiece(PieceClearedEvent e, float delay)
        {
            if (!_pieces.TryGetValue(e.PieceId, out var v)) return 0f;
            Forget(v);

            if (e.Reason == ClearReason.Match && _mergeTargets.TryGetValue(e.Pos, out var target))
            {
                var from = v.T.localPosition;
                var to = World(target);
                Tweens.Run(Timing.MergeFlight, t =>
                {
                    v.T.localPosition = Vector3.LerpUnclamped(from, to, Ease.InQuad(t));
                    v.T.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, t);
                }, delay, () => Recycle(v));
                return delay + Timing.MergeFlight;
            }

            if (e.Kind == PieceKind.Box)
            {
                _fx.Splinters(World(e.Pos), delay);
                Tweens.Run(Timing.ObstacleBreak, t => v.T.localScale = Vector3.one * (1f - t), delay, () => Recycle(v));
                return delay + Timing.ObstacleBreak + 0.06f;
            }

            var color = e.Kind == PieceKind.Gem ? Palette.Gem(e.Color) : Color.white;
            float fx = _fx.Burst(World(e.Pos), color, delay);
            Tweens.Run(Timing.Pop, t =>
            {
                float s = t < 0.35f ? Mathf.Lerp(1f, 1.18f, t / 0.35f) : Mathf.Lerp(1.18f, 0f, (t - 0.35f) / 0.65f);
                v.T.localScale = Vector3.one * s;
            }, delay, () => Recycle(v));
            return Mathf.Max(delay + Timing.Pop, fx - 0.1f);
        }

        void Forget(PieceView v)
        {
            _pieces.Remove(v.Id);
            int i = Index(v.Pos);
            if (_grid[i] == v) _grid[i] = null;
        }

        float FormSpecial(SpecialCreatedEvent e)
        {
            var v = CreatePiece(e.PieceId, e.Kind, GemColor.None, 0, e.Pos);
            v.T.localScale = Vector3.zero;
            float delay = Timing.MergeFlight * 0.7f;
            _fx.Flash(World(e.Pos), new Color(1f, 0.93f, 0.7f, 0.9f), 1.5f, delay, Timing.SpecialForm);
            _fx.Ring(World(e.Pos), new Color(1f, 0.9f, 0.55f, 0.9f), 0.3f, 1.1f, delay, 0.3f);
            _fx.Sparkles(World(e.Pos), 4, delay, 0.6f, 0.6f);
            Signal(Impact.SpecialMade, delay);
            Tweens.Run(Timing.SpecialForm, t => v.T.localScale = Vector3.one * Ease.OutBack(t), delay);
            return delay + Timing.SpecialForm;
        }

        float ConvertPiece(PieceConvertedEvent e, float delay)
        {
            if (_pieces.TryGetValue(e.OldPieceId, out var old))
            {
                Forget(old);
                Tweens.Run(Timing.Pop, t => old.T.localScale = Vector3.one * (1f - t), delay, () => Recycle(old));
            }
            var v = CreatePiece(e.NewPieceId, e.NewKind, GemColor.None, 0, e.Pos);
            v.T.localScale = Vector3.zero;
            _fx.Flash(World(e.Pos), new Color(1f, 1f, 1f, 0.8f), 1.2f, delay, 0.2f);
            Tweens.Run(0.2f, t => v.T.localScale = Vector3.one * Ease.OutBack(t), delay);
            return delay + 0.2f;
        }

        float Activate(SpecialActivatedEvent e)
        {
            // The engine removes activated specials without a clear event.
            if (_pieces.TryGetValue(e.PieceId, out var main)) Consume(main);
            if (e.PartnerPieceId >= 0 && _pieces.TryGetValue(e.PartnerPieceId, out var partner)) Consume(partner);

            var center = World(e.Pos);
            float perCell = PerCell(e);
            float reach = 0f;
            foreach (var cell in e.Area) reach = Mathf.Max(reach, e.Pos.Chebyshev(cell));
            float sweep = reach * perCell;
            var warm = new Color(1f, 0.85f, 0.55f, 0.95f);
            float end = sweep + 0.1f;

            if (e.Combo != ComboKind.None)
            {
                ComboFired?.Invoke(e.Combo);
                bool big = e.Combo == ComboKind.BombBomb || e.Combo == ComboKind.ColorColor || e.Combo == ComboKind.ColorBomb;
                Signal(big ? Impact.BigCombo : Impact.Combo, 0f);
                _fx.Sparkles(center, 6, 0f, 1.2f, 0.8f);
            }
            else if (e.Kind == PieceKind.Bomb) Signal(Impact.Bomb, 0f);
            else if (e.Kind == PieceKind.ColorBomb) Signal(Impact.ColorBomb, 0f);
            else Signal(Impact.Rocket, 0f);

            switch (e.Combo)
            {
                case ComboKind.None:
                    if (e.Kind.IsRocket()) end = Mathf.Max(end, RocketFx(e.Pos, e.Kind == PieceKind.RocketH, warm));
                    else if (e.Kind == PieceKind.Bomb) end = Mathf.Max(end, _fx.Ring(center, warm, 0.3f, reach + 0.6f, 0f, sweep + 0.14f));
                    else if (e.Kind == PieceKind.Homing) end = Mathf.Max(end, _fx.Ring(center, new Color(0.45f, 0.9f, 1f, 0.9f), 0.2f, 1.5f, 0f, 0.2f));
                    else end = Mathf.Max(end, ColorBeams(e, perCell));
                    break;
                case ComboKind.RocketRocket:
                    RocketFx(e.Pos, true, warm);
                    end = Mathf.Max(end, RocketFx(e.Pos, false, warm));
                    break;
                case ComboKind.RocketBomb:
                    for (int i = -1; i <= 1; i++)
                    {
                        var row = e.Pos.Offset(0, i);
                        var column = e.Pos.Offset(i, 0);
                        if (InBounds(row)) end = Mathf.Max(end, RocketFx(row, true, warm));
                        if (InBounds(column)) end = Mathf.Max(end, RocketFx(column, false, warm));
                    }
                    break;
                case ComboKind.BombBomb:
                case ComboKind.ColorColor:
                    _fx.Flash(center, warm, reach * 1.4f, 0f, sweep + 0.1f);
                    end = Mathf.Max(end, _fx.Ring(center, warm, 0.4f, reach + 0.8f, 0f, sweep + 0.2f));
                    break;
                default:
                    end = Mathf.Max(end, _fx.Ring(center, new Color(0.7f, 0.9f, 1f, 0.9f), 0.3f, 2f, 0f, 0.3f));
                    break;
            }
            return end;
        }

        void Consume(PieceView v)
        {
            Forget(v);
            Tweens.Run(Timing.Pop, t => v.T.localScale = Vector3.one * (1f + t * 0.4f), 0f, () => Recycle(v));
        }

        float RocketFx(GridPos origin, bool horizontal, Color color)
        {
            var center = World(origin);
            float end = 0f;
            for (int dir = -1; dir <= 1; dir += 2)
            {
                int cells = horizontal
                    ? (dir < 0 ? origin.X : _w - 1 - origin.X)
                    : (dir < 0 ? origin.Y : _h - 1 - origin.Y);
                var step = horizontal ? Vector3.right * dir : Vector3.up * dir;
                var to = center + step * (cells + 1);
                float flight = Mathf.Max(0.06f, (cells + 1) * Timing.RocketPerCell);
                _fx.Beam(center, to, color, 0.5f, 0f, flight + 0.12f);
                end = Mathf.Max(end, _fx.Fly(Art.Rocket(horizontal), center, to, 0f, flight, 0.8f));
            }
            return end;
        }

        float ColorBeams(SpecialActivatedEvent e, float perCell)
        {
            var center = World(e.Pos);
            var color = Palette.Gem(e.TargetColor);
            color.a = 0.85f;
            float end = 0f;
            foreach (var cell in e.Area)
            {
                if (cell == e.Pos) continue;
                end = Mathf.Max(end, _fx.Beam(center, World(cell), color, 0.22f, 0f, e.Pos.Chebyshev(cell) * perCell + 0.12f));
            }
            return end;
        }

        float DamageBox(PieceDamagedEvent e, float delay)
        {
            if (!_pieces.TryGetValue(e.PieceId, out var v)) return 0f;
            v.Hp = e.HpLeft;
            int hp = e.HpLeft;
            Tweens.After(delay, () => v.R.sprite = Art.Box(hp));
            _fx.Splinters(World(v.Pos), delay, 3);
            ShakePiece(v, delay);
            return delay + Timing.ObstacleBreak;
        }

        void ShakePiece(PieceView v, float delay)
        {
            var home = World(v.Pos);
            Tweens.Run(Timing.ObstacleBreak, t =>
            {
                float offset = Mathf.Sin(t * Mathf.PI * 4f) * 0.07f * (1f - t);
                v.T.localPosition = home + Vector3.right * offset;
            }, delay);
        }

        float DamageCover(CoverDamagedEvent e, float delay)
        {
            int hp = e.HpLeft;
            var p = e.Pos;
            _coverHp[Index(p)] = hp; // keep the record exact even while the visual change waits
            _fx.IceShards(World(p), delay);
            Tweens.After(delay, () => SetCover(p, hp));
            return delay + Timing.ObstacleBreak;
        }

        float DamageFloor(FloorDamagedEvent e, float delay)
        {
            int hp = e.HpLeft;
            var p = e.Pos;
            _floorHp[Index(p)] = hp;
            _fx.AshPuff(World(p), delay);
            Tweens.After(delay, () => SetFloor(p, hp));
            return delay + Timing.ObstacleBreak;
        }

        float CollectPiece(PieceCollectedEvent e)
        {
            if (!_pieces.TryGetValue(e.PieceId, out var v)) return 0f;
            Forget(v);
            var from = v.T.localPosition;
            var to = from + Vector3.down * 0.9f;
            v.R.sortingOrder = SortLifted;
            _fx.Flash(from, new Color(1f, 0.85f, 0.3f, 0.9f), 1.4f, 0f, Timing.Collect);
            _fx.Sparkles(from, 5, 0f, 0.6f, 0.7f);
            Signal(Impact.SpecialMade, 0f);
            Tweens.Run(Timing.Collect, t =>
            {
                v.T.localPosition = Vector3.LerpUnclamped(from, to, Ease.InQuad(t));
                v.T.localScale = Vector3.one * (1f - t * 0.6f);
            }, 0f, () => Recycle(v));
            return Timing.Collect;
        }

        float ShufflePieces(ShuffleEvent e)
        {
            // Engine positions after the shuffle overwrite the record in one go.
            foreach (var entry in e.Entries)
            {
                var v = _pieces[entry.PieceId];
                if (_grid[Index(entry.From)] == v) _grid[Index(entry.From)] = null;
            }
            foreach (var entry in e.Entries)
            {
                var v = _pieces[entry.PieceId];
                v.Pos = entry.To;
                _grid[Index(entry.To)] = v;
                if (v.Color != entry.Color)
                {
                    v.Color = entry.Color;
                    v.R.sprite = Art.Gem(entry.Color);
                }
                var from = v.T.localPosition;
                var to = World(entry.To);
                Tweens.Run(Timing.Shuffle, t =>
                {
                    float arc = Mathf.Sin(t * Mathf.PI);
                    v.T.localPosition = Vector3.LerpUnclamped(from, to, Ease.InOutSine(t));
                    v.T.localScale = Vector3.one * (1f - arc * 0.25f);
                });
            }
            return Timing.Shuffle;
        }

        // ---------------------------------------------------------------- end of level

        /// <summary>
        /// The win celebration: one spark per move left flies from <paramref name="from"/> to a cell and bursts into a
        /// firework. Cosmetic only, the board does not change. <paramref name="launched"/> gets the spark's index as it
        /// leaves, so the move counter can count down with it. Returns the time until the last firework fades.
        /// </summary>
        public float Celebrate(Vector3 from, int sparks, Action<int> launched)
        {
            var cells = new List<GridPos>();
            for (int y = 0; y < _h; y++)
                for (int x = 0; x < _w; x++)
                    if (_exists[y * _w + x]) cells.Add(new GridPos(x, y));
            if (cells.Count == 0) return 0f;

            float end = 0f;
            for (int i = 0; i < sparks; i++)
            {
                // Golden-ratio steps spread the targets over the board without repeating soon.
                var cell = cells[(int)(Mathf.Repeat(i * 0.618034f + 0.31f, 1f) * cells.Count)];
                var color = Palette.Gem(i % GemColor.Count);
                float delay = i * Timing.WinSparkInterval;
                int index = i;
                Tweens.After(delay, () => launched?.Invoke(index));
                _fx.Spark(from, World(cell), color, delay, Timing.WinSparkFlight);
                float boom = delay + Timing.WinSparkFlight;
                _fx.Firework(World(cell), color, boom);
                Signal(Impact.Firework, boom);
                end = Mathf.Max(end, boom + 0.4f);
            }
            return end;
        }

        // ---------------------------------------------------------------- gravity

        sealed class FallTrack
        {
            public PieceView View;
            public readonly List<Vector3> Points = new List<Vector3>();
            public readonly List<float> Times = new List<float>();
            public bool FadesIn;
            public bool Landed;
            public int Segment;
        }

        IEnumerator PlayGravity(BoardStep step)
        {
            var tracks = new List<FallTrack>();
            var byPiece = new Dictionary<int, FallTrack>();

            // Update the record: first free every source cell, then fill every destination,
            // because one piece's destination can be another piece's source.
            foreach (var ev in step.Events)
                if (ev is PieceMovedEvent m && _pieces.TryGetValue(m.PieceId, out var moved) && _grid[Index(m.From)] == moved)
                    _grid[Index(m.From)] = null;

            foreach (var ev in step.Events)
            {
                if (!(ev is PieceSpawnedEvent s)) continue;
                var v = CreatePiece(s.PieceId, s.Kind, s.Color, 0, s.Pos);
                var track = new FallTrack { View = v, FadesIn = true };
                var above = World(s.Pos) + Vector3.up;
                track.Points.Add(above);
                track.Times.Add((s.Tick - 1) * Timing.GravityTick);
                track.Points.Add(World(s.Pos));
                track.Times.Add(s.Tick * Timing.GravityTick);
                v.T.localPosition = above;
                v.R.color = new Color(1f, 1f, 1f, 0f);
                tracks.Add(track);
                byPiece[s.PieceId] = track;
            }

            foreach (var ev in step.Events)
            {
                if (!(ev is PieceMovedEvent m)) continue;
                var v = _pieces[m.PieceId];
                if (_grid[Index(m.From)] == v) _grid[Index(m.From)] = null;
                if (!byPiece.TryGetValue(m.PieceId, out var track))
                {
                    track = new FallTrack { View = v };
                    track.Points.Add(World(m.From));
                    track.Times.Add(0f);
                    tracks.Add(track);
                    byPiece[m.PieceId] = track;
                }
                foreach (var p in m.Path)
                {
                    float start = (p.Tick - 1) * Timing.GravityTick;
                    var last = track.Points[track.Points.Count - 1];
                    if (start > track.Times[track.Times.Count - 1])
                    {
                        // it waited here for the cell below to free up
                        track.Points.Add(last);
                        track.Times.Add(start);
                    }
                    track.Points.Add(World(p.Pos));
                    track.Times.Add(p.Tick * Timing.GravityTick);
                }
                v.Pos = m.To;
            }

            foreach (var t in tracks) _grid[Index(t.View.Pos)] = t.View;

            float total = 0f;
            foreach (var t in tracks) total = Mathf.Max(total, t.Times[t.Times.Count - 1]);

            float time = 0f;
            while (time < total)
            {
                time += Time.deltaTime;
                foreach (var t in tracks) Advance(t, time);
                yield return null;
            }
        }

        static void Advance(FallTrack t, float time)
        {
            var times = t.Times;
            int last = times.Count - 1;
            if (time >= times[last])
            {
                if (!t.Landed) Land(t);
                return;
            }
            while (t.Segment < last - 1 && time >= times[t.Segment + 1]) t.Segment++;
            float s = Mathf.InverseLerp(times[t.Segment], times[t.Segment + 1], time);
            t.View.T.localPosition = Vector3.LerpUnclamped(t.Points[t.Segment], t.Points[t.Segment + 1], s);
            if (t.FadesIn && t.Segment == 0)
            {
                t.View.R.color = new Color(1f, 1f, 1f, s);
                t.View.T.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, s);
            }
            else if (t.FadesIn)
            {
                t.View.R.color = Color.white;
                t.View.T.localScale = Vector3.one;
            }
        }

        static void Land(FallTrack t)
        {
            t.Landed = true;
            var v = t.View;
            v.R.color = Color.white;
            v.T.localPosition = t.Points[t.Points.Count - 1];
            Tweens.Run(Timing.LandSquash, s =>
            {
                float k = Mathf.Sin(s * Mathf.PI) * 0.12f;
                v.T.localScale = new Vector3(1f + k, 1f - k, 1f);
            }, 0f, () => v.T.localScale = Vector3.one);
        }
    }
}
