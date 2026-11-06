using System;
using System.Collections.Generic;

namespace EmberThrone.Core
{
    public static class Gravity
    {
        public static bool CanFall(Cell c)
        {
            return c.Exists && c.Piece != null && c.Cover == 0 && c.Piece.Kind.CanFall() && !c.Piece.Triggered;
        }

        public static bool HasVerticalSource(Board b, int x, int y)
        {
            for (int yy = y + 1; yy < b.Height; yy++)
            {
                var c = b.CellAt(x, yy);
                if (!c.Exists) return false;
                if (c.Piece != null) return CanFall(c);
                if (c.Spawner) return true;
            }
            return false;
        }

        public static bool Run(Board b, GameRules rules, Func<GridPos, Piece> spawn, List<BoardEvent> events)
        {
            int w = b.Width, h = b.Height;
            var arrivedTick = new int[w * h];
            var moves = new Dictionary<int, PieceMovedEvent>();
            var moveOrder = new List<PieceMovedEvent>();
            var spawns = new List<PieceSpawnedEvent>();
            bool any = false;

            void MovePiece(int fx, int fy, int tx, int ty, int tick)
            {
                var from = b.CellAt(fx, fy);
                var to = b.CellAt(tx, ty);
                var piece = from.Piece;
                from.Piece = null;
                to.Piece = piece;
                arrivedTick[ty * w + tx] = tick;
                if (!moves.TryGetValue(piece.Id, out var ev))
                {
                    ev = new PieceMovedEvent(piece.Id, new GridPos(fx, fy));
                    moves[piece.Id] = ev;
                    moveOrder.Add(ev);
                }
                ev.Path.Add(new PathStep(tick, new GridPos(tx, ty)));
            }

            for (int tick = 1; tick <= rules.MaxGravityTicks; tick++)
            {
                bool tickAny = false;

                for (int y = 0; y < h - 1; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        var c = b.CellAt(x, y);
                        if (!c.Exists || c.Piece != null) continue;
                        var above = b.CellAt(x, y + 1);
                        if (!CanFall(above) || arrivedTick[(y + 1) * w + x] == tick) continue;
                        MovePiece(x, y + 1, x, y, tick);
                        tickAny = true;
                    }
                }

                for (int x = 0; x < w; x++)
                {
                    for (int y = h - 1; y >= 0; y--)
                    {
                        var c = b.CellAt(x, y);
                        if (!c.Exists || !c.Spawner || c.Piece != null) continue;
                        var piece = spawn(new GridPos(x, y));
                        c.Piece = piece;
                        arrivedTick[y * w + x] = tick;
                        spawns.Add(new PieceSpawnedEvent(piece.Id, new GridPos(x, y), piece.Kind, piece.Color, tick));
                        tickAny = true;
                    }
                }

                bool leftFirst = (tick & 1) == 1;
                for (int y = 0; y < h - 1; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        var c = b.CellAt(x, y);
                        if (!c.Exists || c.Piece != null) continue;
                        if (HasVerticalSource(b, x, y)) continue;
                        for (int k = 0; k < 2; k++)
                        {
                            int dx = (k == 0) == leftFirst ? -1 : 1;
                            int sx = x + dx, sy = y + 1;
                            if (!b.InBounds(sx, sy)) continue;
                            var src = b.CellAt(sx, sy);
                            if (!CanFall(src) || arrivedTick[sy * w + sx] == tick) continue;
                            var below = b.CellAt(sx, y);
                            if (below.Exists && below.Piece == null) continue;
                            MovePiece(sx, sy, x, y, tick);
                            tickAny = true;
                            break;
                        }
                    }
                }

                if (!tickAny) break;
                any = true;
            }

            events.AddRange(spawns);
            events.AddRange(moveOrder);
            return any;
        }

        public static bool IsStable(Board b)
        {
            for (int y = 0; y < b.Height; y++)
            {
                for (int x = 0; x < b.Width; x++)
                {
                    var c = b.CellAt(x, y);
                    if (!c.Exists) continue;
                    if (c.Piece == null)
                    {
                        if (c.Spawner) return false;
                        if (y + 1 < b.Height && CanFall(b.CellAt(x, y + 1))) return false;
                        if (HasVerticalSource(b, x, y)) return false;
                        for (int dx = -1; dx <= 1; dx += 2)
                        {
                            int sx = x + dx, sy = y + 1;
                            if (!b.InBounds(sx, sy) || !CanFall(b.CellAt(sx, sy))) continue;
                            var below = b.CellAt(sx, y);
                            if (!(below.Exists && below.Piece == null)) return false;
                        }
                    }
                }
            }
            return true;
        }
    }
}
