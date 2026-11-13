using System;
using System.Collections;
using System.Collections.Generic;
using EmberThrone.Core;
using UnityEngine;

namespace EmberThrone.Game
{
    public sealed class BoardView : MonoBehaviour
    {
        const int SortBack = -2;
        const int SortFloor = 1;
        const int SortExit = 2;
        const int SortAura = 9;
        const int SortPiece = 10;
        const int SortLifted = 15;
        const int SortCover = 20;
        const int SortSelection = 25;

        const float AuraScale = 1.75f;
        const float AuraAlpha = 0.55f;
        const float PopSwell = 0.6f;
        const float RippleTime = 0.3f;
        const float RippleLag = 0.025f;
        const float TwinkleMin = 0.25f;
        const float TwinkleMax = 0.7f;
        const float HammerHit = 0.12f;
        const float LandHop = 0.12f;
        const float BrushDelay = 0.18f;
        static readonly Vector3 StatueOffset = new Vector3(0.5f, 0.5f, 0f);
        const float IntroDrop = 0.9f;
        const float IntroTime = 0.3f;
        const float IntroColumnLag = 0.025f;
        const float IntroRowLag = 0.035f;

        static readonly Color Warm = new Color(1f, 0.85f, 0.55f, 0.95f);
        static readonly Color Gold = new Color(1f, 0.9f, 0.55f, 0.9f);
        static readonly Color Cream = new Color(1f, 0.95f, 0.78f, 0.9f);
        static readonly Color RocketFire = new Color(1f, 0.6f, 0.22f, 1f);
        static readonly Color BombFire = new Color(1f, 0.45f, 0.18f, 1f);
        static readonly Color HomingFire = new Color(1f, 0.7f, 0.25f, 1f);
        static readonly Color OrbLight = new Color(0.85f, 0.7f, 1f, 1f);
        static readonly Color Twinkle = new Color(1f, 0.98f, 0.88f, 1f);
        static readonly Vector3 FallStretch = new Vector3(0.93f, 1.09f, 1f);

        static readonly int FlashId = Shader.PropertyToID("_Flash");
        static MaterialPropertyBlock _flashBlock;

        sealed class PieceView
        {
            public int Id;
            public PieceKind Kind;
            public int Color;
            public int Hp;
            public GridPos Pos;
            public Transform T;
            public Transform Body;
            public SpriteRenderer R;
            public SpriteRenderer Aura;
            public float Seed;
            public int Motion;
        }

        struct Ripple
        {
            public Vector3 Center;
            public float Radius;
            public float Strength;
            public float Delay;
        }

        public event Action<GoalProgressEvent> GoalProgressed;
        public event Action<FrostSpreadEvent> FrostSpread;
        public event Action<Impact> Impacted;
        public event Action<int> WaveStarted;
        public event Action<ComboKind> ComboFired;

        BoardFx _fx;
        int _w, _h;
        PieceView[] _grid;
        SpriteRenderer[] _floors;
        SpriteRenderer[] _covers;
        int[] _floorHp;
        int[] _coverHp;
        bool[] _frost;
        bool[] _exists;
        Texture2D _skin;
        readonly Dictionary<int, PieceView> _pieces = new Dictionary<int, PieceView>();
        readonly Stack<PieceView> _pool = new Stack<PieceView>();
        SpriteRenderer _selection;
        readonly Dictionary<GridPos, float> _delays = new Dictionary<GridPos, float>();
        readonly Dictionary<GridPos, GridPos> _mergeTargets = new Dictionary<GridPos, GridPos>();
        readonly List<Ripple> _ripples = new List<Ripple>();
        readonly List<PieceView> _hint = new List<PieceView>();
        float _hintTime;
        float _twinkleIn;
        GridPos _convertFrom = GridPos.Invalid;
        Transform _pieceRoot;

        public int Width => _w;
        public int Height => _h;
        public bool HintShown => _hint.Count > 0;
        public BoardFx Fx => _fx;

        public void ShowHint(Move move)
        {
            ClearHint();
            AddHint(move.A);
            if (move.Type == MoveType.Swap) AddHint(move.B);
            _hintTime = 0f;
        }

        void AddHint(GridPos p)
        {
            if (!InBounds(p)) return;
            var v = _grid[Index(p)];
            if (v != null) _hint.Add(v);
        }

        public void ClearHint()
        {
            foreach (var v in _hint)
                if (v.T != null) v.T.localScale = Vector3.one;
            _hint.Clear();
        }

        void Update()
        {
            if (_pieceRoot == null) return;
            AnimateSpecials(Time.time);
            TwinkleSomewhere();

            if (_hint.Count == 0) return;
            _hintTime += Time.deltaTime;
            float wave = 0.5f - 0.5f * Mathf.Cos(_hintTime * Mathf.PI * 2f / Timing.HintPulse);
            var scale = Vector3.one * (1f + Timing.HintScale * wave);
            foreach (var v in _hint) v.T.localScale = scale;
        }

        void AnimateSpecials(float time)
        {
            foreach (var v in _pieces.Values)
            {
                if (!v.Kind.IsSpecial()) continue;
                float phase = time * 4.2f + v.Seed;
                float beat = Mathf.Sin(phase);
                switch (v.Kind)
                {
                    case PieceKind.RocketH:
                        v.Body.localPosition = new Vector3(beat * 0.035f, 0f, 0f);
                        break;
                    case PieceKind.RocketV:
                        v.Body.localPosition = new Vector3(0f, beat * 0.035f, 0f);
                        break;
                    case PieceKind.Bomb:
                        v.Body.localScale = Vector3.one * (1f + 0.05f * Mathf.Abs(beat));
                        break;
                    case PieceKind.ColorBomb:
                        v.Body.localScale = Vector3.one * (1f + 0.04f * beat);
                        v.Body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase * 0.5f) * 7f);
                        break;
                    default:
                        v.Body.localPosition = new Vector3(0f, beat * 0.03f, 0f);
                        v.Body.localRotation = Quaternion.Euler(0f, 0f, beat * 5f);
                        break;
                }
                var glow = v.Aura.color;
                glow.a = AuraAlpha * (0.65f + 0.35f * Mathf.Sin(phase * 1.3f));
                v.Aura.color = glow;
            }
        }

        void TwinkleSomewhere()
        {
            _twinkleIn -= Time.deltaTime;
            if (_twinkleIn > 0f || _grid == null) return;
            _twinkleIn = UnityEngine.Random.Range(TwinkleMin, TwinkleMax);
            for (int tries = 0; tries < 4; tries++)
            {
                var v = _grid[UnityEngine.Random.Range(0, _grid.Length)];
                if (v == null || v.Kind.IsBlocker()) continue;
                var at = World(v.Pos) + new Vector3(UnityEngine.Random.Range(-0.22f, 0.22f), UnityEngine.Random.Range(0f, 0.28f), 0f);
                _fx.Particles.Glints(at, Twinkle, 1, 0.02f, 0.42f);
                return;
            }
        }

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
            _frost = new bool[n];
            _exists = new bool[n];

            _pieceRoot = new GameObject("Pieces").transform;
            _pieceRoot.SetParent(transform, false);
            var fxRoot = new GameObject("Fx").transform;
            fxRoot.SetParent(transform, false);
            _fx = new BoardFx(fxRoot);

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
                    SetFloor(pos, cell.Floor);
                    SetCover(pos, cell.Cover, cell.Frost);
                    if (cell.Piece != null && _pieces.TryGetValue(cell.Piece.Id, out var block)) _grid[i] = block;
                    else if (cell.Piece != null) CreatePiece(cell.Piece.Id, cell.Piece.Kind, cell.Piece.Color, cell.Piece.Hp, pos);
                    if (hasCollectibleGoal && board.IsExit(x, y))
                    {
                        var exit = NewSprite("Exit", Art.Chevron(), Palette.Gold, World(pos) + Vector3.down * 0.6f, SortExit);
                        exit.transform.localScale = Vector3.one * 0.3f;
                    }
                }
            }

            _skin = BoardSkin.Bake(_exists, _w, _h, BoardSkin.Tone);
            NewSprite("Board", BoardSkin.SpriteOf(_skin), Color.white, Vector3.zero, SortBack);

            _selection = NewSprite("Selection", Art.Selection(), Palette.Select, Vector3.zero, SortSelection);
            _selection.gameObject.SetActive(false);
            PlayIntro();
        }

        void OnDestroy()
        {
            if (_skin != null) Destroy(_skin);
        }

        void PlayIntro()
        {
            foreach (var piece in _pieces.Values)
            {
                var v = piece;
                var home = Home(v);
                var from = home + Vector3.up * IntroDrop;
                int motion = ++v.Motion;
                v.T.localPosition = from;
                v.T.localScale = Vector3.zero;
                Tweens.Run(IntroTime, t =>
                {
                    if (v.Motion != motion) return;
                    v.T.localPosition = Vector3.LerpUnclamped(from, home, Ease.OutBackSoft(t));
                    v.T.localScale = Vector3.one * Mathf.Min(1f, 0.3f + t * 2.2f);
                }, v.Pos.X * IntroColumnLag + v.Pos.Y * IntroRowLag, () =>
                {
                    if (v.Motion != motion) return;
                    v.T.localPosition = home;
                    v.T.localScale = Vector3.one;
                });
            }
        }

        public float IntroLength => (_w - 1) * IntroColumnLag + (_h - 1) * IntroRowLag + IntroTime;

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
                var aura = new GameObject("Aura");
                aura.transform.SetParent(go.transform, false);
                var body = new GameObject("Body");
                body.transform.SetParent(go.transform, false);
                v = new PieceView
                {
                    T = go.transform,
                    Body = body.transform,
                    R = body.AddComponent<SpriteRenderer>(),
                    Aura = aura.AddComponent<SpriteRenderer>(),
                };
                v.R.sharedMaterial = FxArt.Piece;
                v.Aura.sharedMaterial = FxArt.Additive;
                v.Aura.sprite = FxArt.Glow();
                v.Aura.sortingOrder = SortAura;
                aura.transform.localScale = Vector3.one * AuraScale;
            }
            v.Id = id;
            v.Kind = kind;
            v.Color = color;
            v.Hp = hp;
            v.Pos = pos;
            v.Motion++;
            v.Seed = UnityEngine.Random.value * 10f;
            v.R.sprite = Art.Piece(kind, color, hp);
            v.R.color = Color.white;
            v.R.sortingOrder = SortPiece;
            SetFlash(v, 0f);
            v.Body.localPosition = Vector3.zero;
            v.Body.localScale = Vector3.one;
            v.Body.localRotation = Quaternion.identity;
            v.Aura.gameObject.SetActive(kind.IsSpecial());
            v.Aura.color = AuraColor(kind);
            v.T.localPosition = Home(v);
            v.T.localScale = Vector3.one;
            _pieces[id] = v;
            _grid[Index(pos)] = v;
            return v;
        }

        Vector3 Home(PieceView v) => World(v.Pos) + (v.Kind == PieceKind.Statue ? StatueOffset : Vector3.zero);

        static Color AuraColor(PieceKind kind)
        {
            Color c;
            switch (kind)
            {
                case PieceKind.Bomb: c = BombFire; break;
                case PieceKind.ColorBomb: c = OrbLight; break;
                case PieceKind.Homing: c = HomingFire; break;
                default: c = RocketFire; break;
            }
            c.a = AuraAlpha;
            return c;
        }

        static void SetFlash(PieceView v, float amount)
        {
            if (amount <= 0f)
            {
                v.R.SetPropertyBlock(null);
                return;
            }
            if (_flashBlock == null) _flashBlock = new MaterialPropertyBlock();
            _flashBlock.SetFloat(FlashId, amount);
            v.R.SetPropertyBlock(_flashBlock);
        }

        void Recycle(PieceView v)
        {
            v.Motion++;
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

        void SetCover(GridPos p, int hp, bool frost)
        {
            int i = Index(p);
            _coverHp[i] = hp;
            _frost[i] = hp > 0 && frost;
            if (hp > 0)
            {
                var sprite = _frost[i] ? Art.Frost() : Art.Cover(hp);
                if (_covers[i] == null) _covers[i] = NewSprite("Cover", sprite, new Color(1f, 1f, 1f, Palette.CoverAlpha), World(p), SortCover);
                _covers[i].sprite = sprite;
            }
            else if (_covers[i] != null)
            {
                Destroy(_covers[i].gameObject);
                _covers[i] = null;
            }
        }

        int Index(GridPos p) => p.Y * _w + p.X;

        bool InBounds(GridPos p) => p.X >= 0 && p.Y >= 0 && p.X < _w && p.Y < _h;

        public Vector3 World(GridPos p) => new Vector3(p.X - (_w - 1) * 0.5f, p.Y - (_h - 1) * 0.5f, 0f);

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

        public void SetLifted(GridPos p, bool lifted)
        {
            if (!InBounds(p)) return;
            var v = _grid[Index(p)];
            if (v == null) return;
            v.T.localScale = Vector3.one * (lifted ? 1.12f : 1f);
            v.R.sortingOrder = lifted ? SortLifted : SortPiece;
        }

        public void Nudge(GridPos p)
        {
            if (!InBounds(p)) return;
            var v = _grid[Index(p)];
            if (v == null) return;
            ShakePiece(v, 0f);
        }

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
                    if (_frost[i] != cell.Frost) return "cell " + p + ": frost " + _frost[i] + " vs " + cell.Frost;
                }
            }
            return null;
        }

        static string Describe(PieceView v) => v == null ? "empty" : v.Kind + "#" + v.Id;

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
                Sound.Play(Sfx.Swap);
                a.R.sortingOrder = SortLifted;
                Glide(a, World(e.B), Timing.Swap, 0.16f);
                Glide(b, World(e.A), Timing.Swap, -0.1f);
                yield return new WaitForSeconds(Timing.Swap);
                if (e.Reverted)
                {
                    Sound.Play(Sfx.SwapBack);
                    Glide(a, World(e.A), Timing.Swap, 0.08f);
                    Glide(b, World(e.B), Timing.Swap, -0.05f);
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

        void Glide(PieceView v, Vector3 to, float duration, float lift)
        {
            var from = v.T.localPosition;
            int motion = ++v.Motion;
            Tweens.Run(duration, t =>
            {
                if (v.Motion != motion) return;
                v.T.localPosition = Vector3.LerpUnclamped(from, to, Ease.OutBackSoft(t));
                v.T.localScale = Vector3.one * (1f + lift * Mathf.Sin(t * Mathf.PI));
            }, 0f, () =>
            {
                if (v.Motion != motion) return;
                v.T.localPosition = to;
                v.T.localScale = Vector3.one;
            });
        }

        IEnumerator PlayEffects(BoardStep step)
        {
            _delays.Clear();
            _mergeTargets.Clear();
            _ripples.Clear();
            _convertFrom = GridPos.Invalid;
            float strikeDelay = 0f;
            int converted = 0;
            foreach (var ev in step.Events)
            {
                switch (ev)
                {
                    case SpecialActivatedEvent s:
                        float perCell = PerCell(s);
                        foreach (var cell in s.Area) SetEarliest(cell, s.Pos.Chebyshev(cell) * perCell);
                        if (s.Combo == ComboKind.ColorRocket || s.Combo == ComboKind.ColorBomb || s.Combo == ComboKind.ColorHoming)
                            _convertFrom = s.Pos;
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
                    case BoosterUsedEvent b:
                        DelayBooster(b);
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
                        e = _fx.Strike(h.Payload.HasValue ? Art.Piece(h.Payload.Value, GemColor.None, 0) : Art.Homing(),
                            World(h.From), World(h.Target), HomingFire, Timing.StrikeFlight);
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
                    case FrostSpreadEvent f:
                        e = SpreadFrost(f);
                        break;
                    case PieceCollectedEvent c:
                        e = CollectPiece(c);
                        break;
                    case ShuffleEvent s:
                        Sound.Play(Sfx.Shuffle);
                        e = ShufflePieces(s);
                        break;
                    case BoosterUsedEvent b:
                        e = UseBooster(b);
                        break;
                    case GoalProgressEvent g:
                        var goal = g;
                        Tweens.After(DelayAt(g.Source), () => GoalProgressed?.Invoke(goal));
                        break;
                }
                end = Mathf.Max(end, e);
            }
            end = Mathf.Max(end, PlayRipples());
            if (end > 0f) yield return new WaitForSeconds(end);
        }

        void DelayBooster(BoosterUsedEvent b)
        {
            if (!InBounds(b.Target)) return;
            if (b.Type == BoosterType.Hammer) SetEarliest(b.Target, HammerHit);
            else if (b.Type == BoosterType.RowBlast)
                for (int x = 0; x < _w; x++) SetEarliest(new GridPos(x, b.Target.Y), Mathf.Abs(x - b.Target.X) * Timing.RocketPerCell);
            else if (b.Type == BoosterType.ColumnBlast)
                for (int y = 0; y < _h; y++) SetEarliest(new GridPos(b.Target.X, y), Mathf.Abs(y - b.Target.Y) * Timing.RocketPerCell);
            else if (b.Type == BoosterType.ColorBrush)
                for (int y = 0; y < _h; y++)
                    for (int x = 0; x < _w; x++)
                    {
                        var p = new GridPos(x, y);
                        SetEarliest(p, BrushDelay + b.Target.Chebyshev(p) * Timing.ColorPerCell);
                    }
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

        float UseBooster(BoosterUsedEvent b)
        {
            if (b.Type == BoosterType.ColorBrush) return BrushFx(b);
            if (b.Type != BoosterType.Hammer)
            {
                if (b.Type != BoosterType.Shuffle) Sound.Play(Sfx.LineBlast);
                if (b.Type == BoosterType.RowBlast || b.Type == BoosterType.ColumnBlast)
                    return InBounds(b.Target) ? RocketFx(b.Target, b.Type == BoosterType.RowBlast) : 0f;
                return 0f;
            }
            Sound.Play(Sfx.Hammer);
            if (!InBounds(b.Target)) return 0f;
            var at = World(b.Target);
            _fx.Glow(at, Cream, 2.2f, HammerHit, 0.22f);
            _fx.Shockwave(at, Warm, 0.2f, 1.3f, HammerHit, 0.3f);
            Tweens.After(HammerHit, () => _fx.Particles.Sparks(at, Warm, 14, 6f));
            AddRipple(at, 2.4f, 0.14f, HammerHit);
            Signal(Impact.Rocket, HammerHit);
            return HammerHit + 0.3f;
        }

        float BrushFx(BoosterUsedEvent b)
        {
            Sound.Play(Sfx.ColorBomb);
            if (!InBounds(b.Target)) return 0f;
            var at = World(b.Target);
            _fx.Glow(at, Cream, 2.6f, 0f, 0.3f);
            _fx.Rays(at, Gold, 4.2f, 0f, 0.55f);
            _fx.Sparkles(at, 10, 0f, 1.1f, 0.8f);
            _fx.Shockwave(at, OrbLight, 0.3f, Mathf.Max(_w, _h), BrushDelay * 0.5f, 0.5f);
            AddRipple(at, Mathf.Max(_w, _h) + 2f, 0.14f, BrushDelay * 0.5f);
            Signal(Impact.ColorBomb, 0f);
            return BrushDelay;
        }

        float ClearPiece(PieceClearedEvent e, float delay)
        {
            if (!_pieces.TryGetValue(e.PieceId, out var v)) return 0f;
            Forget(v);
            v.Motion++;
            v.T.localScale = Vector3.one;

            if (e.Reason == ClearReason.Match && _mergeTargets.TryGetValue(e.Pos, out var target))
            {
                var from = v.T.localPosition;
                var to = World(target);
                Tweens.Run(Timing.MergeFlight, t =>
                {
                    v.T.localPosition = Vector3.LerpUnclamped(from, to, Ease.InQuad(t));
                    v.T.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, t);
                    SetFlash(v, t * 0.65f);
                }, delay, () => Recycle(v));
                return delay + Timing.MergeFlight;
            }

            if (e.Kind == PieceKind.Lamp)
            {
                var at = Home(v);
                _fx.Glow(at, Alpha(Warm, 0.95f), 2.2f, delay, 0.3f);
                Tweens.After(delay, () =>
                {
                    _fx.Particles.Sparks(at, Gold, 10, 4.5f);
                    Sound.Play(BreakSound(PieceKind.Lamp, true));
                });
                Tweens.Run(Timing.Pop * 1.5f, t =>
                {
                    v.T.localScale = Vector3.one * Mathf.Lerp(1f, 1.35f, Ease.OutQuad(t));
                    v.R.color = new Color(1f, 1f, 1f, 1f - t);
                }, delay, () => Recycle(v));
                return delay + Timing.ObstacleBreak;
            }

            if (e.Kind.IsBlocker())
            {
                var at = Home(v);
                var sprite = v.R.sprite;
                bool statue = e.Kind == PieceKind.Statue;
                if (e.Kind == PieceKind.Box) _fx.WoodBurst(at, delay, true);
                else if (e.Kind == PieceKind.Planter) _fx.AshBurst(at, delay);
                else
                {
                    _fx.IceBurst(at, delay);
                    _fx.Glow(at, Alpha(Cream, 0.9f), 3.2f, delay, 0.35f);
                    Tweens.After(delay, () => _fx.Particles.Glints(at, Twinkle, 8, 0.9f, 0.6f));
                }
                _fx.Shatter(sprite, at, Color.white, delay, statue ? 12 : 8, statue ? 1.2f : 1f);
                var kind = e.Kind;
                Tweens.After(delay, () =>
                {
                    Recycle(v);
                    Sound.Play(BreakSound(kind, true));
                });
                if (statue) Signal(Impact.Bomb, delay);
                return delay + Timing.ObstacleBreak + 0.06f;
            }

            var color = e.Kind == PieceKind.Gem ? Palette.Gem(e.Color) : Warm;
            float swell = Timing.Pop * PopSwell;
            float burst = delay + swell;
            Tweens.Run(swell, t =>
            {
                v.T.localScale = Vector3.one * Mathf.Lerp(1f, 1.24f, Ease.OutQuad(t));
                SetFlash(v, t * 0.6f);
            }, delay);
            Tweens.Run(Timing.Pop - swell, t => v.T.localScale = Vector3.one * Mathf.Lerp(1.24f, 0.2f, t), burst, () => Recycle(v));
            float fx = _fx.GemBurst(World(e.Pos), color, burst, e.Reason == ClearReason.Match ? 6 : 4);
            return Mathf.Max(delay + Timing.Pop, fx - 0.12f);
        }

        void Forget(PieceView v)
        {
            _pieces.Remove(v.Id);
            int i = Index(v.Pos);
            if (_grid[i] == v) _grid[i] = null;
            if (v.Kind != PieceKind.Statue) return;
            for (int k = 0; k < _grid.Length; k++)
                if (_grid[k] == v) _grid[k] = null;
        }

        float FormSpecial(SpecialCreatedEvent e)
        {
            var v = CreatePiece(e.PieceId, e.Kind, GemColor.None, 0, e.Pos);
            v.T.localScale = Vector3.zero;
            float delay = Timing.MergeFlight * 0.7f;
            var at = World(e.Pos);
            _fx.Rays(at, Gold, 3.4f, delay, 0.55f);
            _fx.Glow(at, Cream, 2.3f, delay, Timing.SpecialForm);
            _fx.Shockwave(at, Gold, 0.3f, 1.25f, delay, 0.32f);
            _fx.Sparkles(at, 6, delay, 0.7f, 0.6f);
            Tweens.After(delay, () => _fx.Particles.Sparks(at, Gold, 12, 5.5f));
            Signal(Impact.SpecialMade, delay);
            Tweens.Run(Timing.SpecialForm, t =>
            {
                v.T.localScale = Vector3.one * Ease.OutBack(t);
                SetFlash(v, 1f - t);
            }, delay, () => SetFlash(v, 0f));
            return delay + Timing.SpecialForm;
        }

        float ConvertPiece(PieceConvertedEvent e, float delay)
        {
            if (_pieces.TryGetValue(e.OldPieceId, out var old))
            {
                Forget(old);
                old.Motion++;
                Tweens.Run(Timing.Pop, t => old.T.localScale = Vector3.one * (1f - t), delay, () => Recycle(old));
            }
            var v = CreatePiece(e.NewPieceId, e.NewKind, GemColor.None, 0, e.Pos);
            v.T.localScale = Vector3.zero;
            var at = World(e.Pos);
            if (_convertFrom.IsValid && _convertFrom != e.Pos)
                _fx.Lightning(World(_convertFrom), at, OrbLight, Mathf.Max(0f, delay - 0.12f), 0.2f);
            _fx.Glow(at, new Color(1f, 1f, 1f, 0.85f), 1.7f, delay, 0.24f);
            _fx.Sparkles(at, 2, delay, 0.3f, 0.5f);
            Tweens.Run(0.2f, t =>
            {
                v.T.localScale = Vector3.one * Ease.OutBack(t);
                SetFlash(v, 1f - t);
            }, delay, () => SetFlash(v, 0f));
            return delay + 0.2f;
        }

        float Activate(SpecialActivatedEvent e)
        {
            if (_pieces.TryGetValue(e.PieceId, out var main)) Consume(main);
            if (e.PartnerPieceId >= 0 && _pieces.TryGetValue(e.PartnerPieceId, out var partner)) Consume(partner);

            var center = World(e.Pos);
            float perCell = PerCell(e);
            float reach = 0f;
            foreach (var cell in e.Area) reach = Mathf.Max(reach, e.Pos.Chebyshev(cell));
            float sweep = reach * perCell;
            float end = sweep + 0.1f;

            if (e.Combo != ComboKind.None)
            {
                ComboFired?.Invoke(e.Combo);
                bool big = e.Combo == ComboKind.BombBomb || e.Combo == ComboKind.ColorColor || e.Combo == ComboKind.ColorBomb;
                Signal(big ? Impact.BigCombo : Impact.Combo, 0f);
                _fx.Rays(center, Gold, 4.5f, 0f, 0.6f);
                _fx.Sparkles(center, 8, 0f, 1.2f, 0.8f);
            }
            else if (e.Kind == PieceKind.Bomb) Signal(Impact.Bomb, 0f);
            else if (e.Kind == PieceKind.ColorBomb) Signal(Impact.ColorBomb, 0f);
            else Signal(Impact.Rocket, 0f);

            switch (e.Combo)
            {
                case ComboKind.None:
                    if (e.Kind.IsRocket()) end = Mathf.Max(end, RocketFx(e.Pos, e.Kind == PieceKind.RocketH));
                    else if (e.Kind == PieceKind.Bomb) end = Mathf.Max(end, BombFx(center, reach));
                    else if (e.Kind == PieceKind.Homing) end = Mathf.Max(end, LaunchFx(center));
                    else end = Mathf.Max(end, ColorFx(e, perCell));
                    break;
                case ComboKind.RocketRocket:
                    RocketFx(e.Pos, true);
                    end = Mathf.Max(end, RocketFx(e.Pos, false));
                    break;
                case ComboKind.RocketBomb:
                    _fx.Explosion(center, 1.2f, 0f);
                    AddRipple(center, 3.5f, 0.18f, 0f);
                    for (int i = -1; i <= 1; i++)
                    {
                        var row = e.Pos.Offset(0, i);
                        var column = e.Pos.Offset(i, 0);
                        if (InBounds(row)) end = Mathf.Max(end, RocketFx(row, true));
                        if (InBounds(column)) end = Mathf.Max(end, RocketFx(column, false));
                    }
                    break;
                case ComboKind.BombBomb:
                    end = Mathf.Max(end, BombFx(center, reach));
                    _fx.Shockwave(center, Warm, 0.5f, reach + 2.5f, 0.12f, 0.45f);
                    break;
                case ComboKind.ColorColor:
                    _fx.Glow(center, new Color(1f, 1f, 1f, 0.9f), reach * 2.6f, 0f, sweep + 0.3f, 0.2f);
                    _fx.Shockwave(center, OrbLight, 0.4f, reach + 1f, 0f, sweep + 0.25f);
                    _fx.Shockwave(center, Gold, 0.4f, reach + 1f, 0.12f, sweep + 0.25f);
                    end = Mathf.Max(end, _fx.Shockwave(center, Cream, 0.4f, reach + 1f, 0.24f, sweep + 0.25f));
                    AddRipple(center, reach + 3f, 0.16f, 0f);
                    break;
                default:
                    _fx.Glow(center, Cream, 2.6f, 0f, 0.3f);
                    end = Mathf.Max(end, _fx.Shockwave(center, OrbLight, 0.3f, 2.2f, 0f, 0.32f));
                    _fx.Particles.Sparks(center, Gold, 16, 6.5f);
                    break;
            }
            return end;
        }

        void Consume(PieceView v)
        {
            Forget(v);
            v.Motion++;
            Tweens.Run(Timing.Pop, t =>
            {
                v.T.localScale = Vector3.one * (1f + t * 0.45f);
                SetFlash(v, t);
            }, 0f, () => Recycle(v));
        }

        void AddRipple(Vector3 center, float radius, float strength, float delay)
        {
            _ripples.Add(new Ripple { Center = center, Radius = radius, Strength = strength, Delay = delay });
        }

        float PlayRipples()
        {
            float end = 0f;
            foreach (var ripple in _ripples)
            {
                for (int i = 0; i < _grid.Length; i++)
                {
                    var v = _grid[i];
                    if (v == null || _coverHp[i] > 0) continue;
                    var home = Home(v);
                    var offset = home - ripple.Center;
                    float distance = offset.magnitude;
                    if (distance < 0.01f || distance > ripple.Radius) continue;
                    var push = offset / distance * (ripple.Strength * (1f - distance / ripple.Radius));
                    float delay = ripple.Delay + distance * RippleLag;
                    int id = v.Id;
                    var view = v;
                    Tweens.Run(RippleTime, t =>
                    {
                        if (view.Id != id || !_pieces.ContainsKey(id)) return;
                        view.T.localPosition = home + push * (Mathf.Sin(t * Mathf.PI) * (1f - t * 0.5f));
                    }, delay, () =>
                    {
                        if (view.Id == id && _pieces.ContainsKey(id)) view.T.localPosition = home;
                    });
                    end = Mathf.Max(end, delay + RippleTime);
                }
            }
            _ripples.Clear();
            return end;
        }

        float RocketFx(GridPos origin, bool horizontal)
        {
            var center = World(origin);
            float end = 0f;
            _fx.Glow(center, Cream, 2.2f, 0f, 0.22f);
            _fx.Shockwave(center, Warm, 0.2f, 0.95f, 0f, 0.24f);
            for (int dir = -1; dir <= 1; dir += 2)
            {
                int cells = horizontal
                    ? (dir < 0 ? origin.X : _w - 1 - origin.X)
                    : (dir < 0 ? origin.Y : _h - 1 - origin.Y);
                var step = horizontal ? Vector3.right * dir : Vector3.up * dir;
                var to = center + step * (cells + 1.5f);
                float flight = Mathf.Max(0.08f, (cells + 1) * Timing.RocketPerCell);
                end = Mathf.Max(end, _fx.Rocket(Art.Rocket(horizontal), center, to, RocketFire, flight));
            }
            return end;
        }

        float BombFx(Vector3 center, float reach)
        {
            float radius = Mathf.Max(1.5f, reach + 0.5f);
            AddRipple(center, radius + 2.2f, 0.2f, 0.02f);
            return _fx.Explosion(center, radius, 0f);
        }

        float LaunchFx(Vector3 center)
        {
            _fx.Glow(center, Alpha(HomingFire, 0.85f), 2f, 0f, 0.24f);
            _fx.Particles.Sparks(center, HomingFire, 10, 4.5f);
            return _fx.Shockwave(center, HomingFire, 0.2f, 1.3f, 0f, 0.24f);
        }

        float ColorFx(SpecialActivatedEvent e, float perCell)
        {
            var center = World(e.Pos);
            var color = Palette.Gem(e.TargetColor);
            float end = 0f;
            float longest = 0f;
            foreach (var cell in e.Area)
            {
                if (cell == e.Pos) continue;
                float hit = e.Pos.Chebyshev(cell) * perCell;
                longest = Mathf.Max(longest, hit);
                var at = World(cell);
                end = Mathf.Max(end, _fx.Lightning(center, at, color, Mathf.Max(0f, hit - 0.08f), 0.2f));
                _fx.Glow(at, Alpha(color, 0.8f), 1.5f, Mathf.Max(0f, hit - 0.06f), 0.2f);
            }
            _fx.Rays(center, Color.Lerp(color, Color.white, 0.4f), 4f, 0f, longest + 0.35f);
            _fx.Glow(center, new Color(1f, 1f, 1f, 0.9f), 2.6f, 0f, longest + 0.25f, 0.15f);
            _fx.Shockwave(center, color, 0.3f, 1.6f, 0f, 0.3f);
            _fx.Sparkles(center, 6, 0f, 0.8f, 0.7f);
            return end;
        }

        static Color Alpha(Color c, float a)
        {
            c.a *= a;
            return c;
        }

        float DamageBox(PieceDamagedEvent e, float delay)
        {
            if (!_pieces.TryGetValue(e.PieceId, out var v)) return 0f;
            v.Hp = e.HpLeft;
            int hp = e.HpLeft;
            var before = v.R.sprite;
            var at = Home(v);
            var kind = v.Kind;
            Tweens.After(delay, () =>
            {
                v.R.sprite = Art.Piece(kind, GemColor.None, hp);
                Sound.Play(BreakSound(kind, false));
            });
            switch (kind)
            {
                case PieceKind.Lamp:
                    _fx.Glow(at, Alpha(Warm, 0.9f), 1.8f, delay, 0.35f);
                    Tweens.After(delay, () => _fx.Particles.Glints(at, Gold, 5, 0.4f, 0.5f));
                    break;
                case PieceKind.Planter:
                    _fx.AshBurst(at, delay);
                    Tweens.After(delay, () => _fx.Particles.Glints(at + Vector3.up * 0.3f, Warm, 3, 0.25f, 0.45f));
                    break;
                case PieceKind.Statue:
                    _fx.IceBurst(at, delay);
                    _fx.Shatter(before, at, Color.white, delay, 10, 0.6f, 3);
                    break;
                default:
                    _fx.WoodBurst(at, delay, false);
                    _fx.Shatter(before, at, Color.white, delay, 8, 0.7f, 2);
                    break;
            }
            Tweens.Run(0.12f, t => SetFlash(v, 0.7f * (1f - t)), delay, () => SetFlash(v, 0f));
            ShakePiece(v, delay);
            return delay + Timing.ObstacleBreak;
        }

        static Sfx BreakSound(PieceKind kind, bool broken)
        {
            switch (kind)
            {
                case PieceKind.Lamp: return broken ? Sfx.Collect : Sfx.Star;
                case PieceKind.Planter: return Sfx.BreakAsh;
                case PieceKind.Statue: return Sfx.BreakIce;
                default: return Sfx.BreakWood;
            }
        }

        void ShakePiece(PieceView v, float delay)
        {
            var home = Home(v);
            int motion = ++v.Motion;
            Tweens.Run(Timing.ObstacleBreak, t =>
            {
                if (v.Motion != motion) return;
                float offset = Mathf.Sin(t * Mathf.PI * 4f) * 0.07f * (1f - t);
                v.T.localPosition = home + Vector3.right * offset;
            }, delay);
        }

        float DamageCover(CoverDamagedEvent e, float delay)
        {
            int hp = e.HpLeft;
            var p = e.Pos;
            int i = Index(p);
            bool frost = _frost[i] && hp > 0;
            var before = _covers[i] != null ? _covers[i].sprite : null;
            _coverHp[i] = hp;
            _frost[i] = frost;
            _fx.IceBurst(World(p), delay);
            _fx.Shatter(before, World(p), new Color(1f, 1f, 1f, Palette.CoverAlpha), delay, 7, hp > 0 ? 0.7f : 0.95f, hp > 0 ? 2 : 0);
            Tweens.After(delay, () =>
            {
                SetCover(p, hp, frost);
                Sound.Play(Sfx.BreakIce);
            });
            return delay + Timing.ObstacleBreak;
        }

        float SpreadFrost(FrostSpreadEvent e)
        {
            var spread = e;
            int i = Index(e.To);
            _coverHp[i] = 1;
            _frost[i] = true;
            float arrive = _fx.FrostCreep(World(e.From), World(e.To), Timing.FrostCreep);
            Tweens.After(arrive, () =>
            {
                SetCover(spread.To, 1, true);
                GrowCover(spread.To);
                Sound.Play(Sfx.Freeze);
                FrostSpread?.Invoke(spread);
            });
            return arrive + Timing.FrostGrow;
        }

        void GrowCover(GridPos p)
        {
            var cover = _covers[Index(p)];
            if (cover == null) return;
            Tweens.Run(Timing.FrostGrow, t =>
            {
                if (cover == null) return;
                cover.transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.3f, 1f, Ease.OutBack(t));
                cover.color = new Color(1f, 1f, 1f, Palette.CoverAlpha * Mathf.Clamp01(t * 2.5f));
            }, 0f, () =>
            {
                if (cover == null) return;
                cover.transform.localScale = Vector3.one;
                cover.color = new Color(1f, 1f, 1f, Palette.CoverAlpha);
            });
        }

        float DamageFloor(FloorDamagedEvent e, float delay)
        {
            int hp = e.HpLeft;
            var p = e.Pos;
            int i = Index(p);
            var before = _floors[i] != null ? _floors[i].sprite : null;
            _floorHp[i] = hp;
            _fx.AshBurst(World(p), delay);
            _fx.Shatter(before, World(p), Color.white, delay, 6, 0.55f, hp > 0 ? 2 : 0);
            Tweens.After(delay, () =>
            {
                SetFloor(p, hp);
                Sound.Play(Sfx.BreakAsh);
            });
            return delay + Timing.ObstacleBreak;
        }

        float CollectPiece(PieceCollectedEvent e)
        {
            if (!_pieces.TryGetValue(e.PieceId, out var v)) return 0f;
            Forget(v);
            v.Motion++;
            var from = v.T.localPosition;
            var to = from + Vector3.down * 0.9f;
            v.R.sortingOrder = SortLifted;
            _fx.Rays(from, Gold, 3.2f, 0f, 0.5f);
            _fx.Glow(from, new Color(1f, 0.85f, 0.3f, 0.9f), 2.2f, 0f, Timing.Collect);
            _fx.Shockwave(from, Gold, 0.2f, 1.1f, 0f, 0.28f);
            _fx.Sparkles(from, 6, 0f, 0.6f, 0.7f);
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
            foreach (var entry in e.Entries)
            {
                var v = _pieces[entry.PieceId];
                if (_grid[Index(entry.From)] == v) _grid[Index(entry.From)] = null;
            }
            foreach (var entry in e.Entries)
            {
                var v = _pieces[entry.PieceId];
                v.Pos = entry.To;
                v.Motion++;
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

            float halfWidth = _w * 0.5f, halfHeight = _h * 0.5f;
            _fx.Confetti(new Vector3(-halfWidth - 0.4f, -halfHeight - 0.6f, 0f), new Vector3(0.45f, 1f, 0f).normalized, 40, 0.05f);
            _fx.Confetti(new Vector3(halfWidth + 0.4f, -halfHeight - 0.6f, 0f), new Vector3(-0.45f, 1f, 0f).normalized, 40, 0.05f);
            _fx.Confetti(new Vector3(-halfWidth - 0.4f, -halfHeight - 0.6f, 0f), new Vector3(0.3f, 1f, 0f).normalized, 30, end * 0.5f);
            _fx.Confetti(new Vector3(halfWidth + 0.4f, -halfHeight - 0.6f, 0f), new Vector3(-0.3f, 1f, 0f).normalized, 30, end * 0.5f);
            return end;
        }

        sealed class FallTrack
        {
            public PieceView View;
            public readonly List<Vector3> Points = new List<Vector3>();
            public readonly List<float> Times = new List<float>();
            public bool FadesIn;
            public bool Landed;
            public int Segment;
        }

        static float Fall(float u) => u * (0.45f + 0.55f * u);

        IEnumerator PlayGravity(BoardStep step)
        {
            var tracks = new List<FallTrack>();
            var byPiece = new Dictionary<int, FallTrack>();

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
                    v.Motion++;
                }
                foreach (var p in m.Path)
                {
                    float start = (p.Tick - 1) * Timing.GravityTick;
                    var last = track.Points[track.Points.Count - 1];
                    if (start > track.Times[track.Times.Count - 1])
                    {
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
                float warped = total * Fall(Mathf.Clamp01(time / total));
                foreach (var t in tracks) Advance(t, warped);
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
            var from = t.Points[t.Segment];
            var to = t.Points[t.Segment + 1];
            t.View.T.localPosition = Vector3.LerpUnclamped(from, to, s);
            if (t.FadesIn && t.Segment == 0)
            {
                t.View.R.color = new Color(1f, 1f, 1f, s);
                t.View.T.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, s);
                return;
            }
            if (t.FadesIn) t.View.R.color = Color.white;
            t.View.T.localScale = to.y < from.y - 0.01f ? FallStretch : Vector3.one;
        }

        static void Land(FallTrack t)
        {
            t.Landed = true;
            var v = t.View;
            v.R.color = Color.white;
            var home = t.Points[t.Points.Count - 1];
            v.T.localPosition = home;
            int motion = ++v.Motion;
            Tweens.Run(Timing.LandSquash * 3f, s =>
            {
                if (v.Motion != motion) return;
                float k = Mathf.Sin(s * Mathf.PI * 3f) * (1f - s) * 0.17f;
                v.T.localScale = new Vector3(1f + k, 1f - k, 1f);
                float hop = Mathf.Max(0f, Mathf.Sin((s - 0.2f) * Mathf.PI * 1.25f)) * LandHop * (1f - s);
                v.T.localPosition = home + Vector3.up * hop;
            }, 0f, () =>
            {
                if (v.Motion != motion) return;
                v.T.localScale = Vector3.one;
                v.T.localPosition = home;
            });
        }
    }
}
