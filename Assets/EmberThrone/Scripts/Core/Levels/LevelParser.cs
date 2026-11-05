using System;
using System.Collections.Generic;
using System.Text;

namespace EmberThrone.Core
{
    public sealed class LevelFormatException : Exception
    {
        public LevelFormatException(string message) : base(message) { }
    }

    /// <summary>
    /// Level JSON format (rows are written top row first; spaces inside rows are ignored):
    /// <code>
    /// {
    ///   "id": 12, "name": "Ash Gate", "moves": 24, "colors": 5, "difficulty": "hard",
    ///   "goals": [ {"type":"box"}, {"type":"color","color":"R","count":20} ],
    ///   "board":    [ "..1111..", ... ],     // required
    ///   "floor":    [ "00111100", ... ],     // optional, floor HP per cell
    ///   "cover":    [ "00000000", ... ],     // optional, cover HP per cell
    ///   "spawners": [ "SSSSSSSS", ... ],     // optional, default = top cell of every column
    ///   "collectibles": {"total": 3, "maxOnBoard": 2, "spawnChance": 0.12}   // optional
    /// }
    /// </code>
    /// Board tokens: . random gem | # hole | _ empty | R G B Y P O gem | 1-9 box HP |
    /// - rocket(row) | '|' rocket(column) | * bomb | ^ homing | @ color bomb | $ collectible.
    /// Goal types: color, box, floor, cover, collectible. Count -1 / omitted = all on the board.
    /// </summary>
    public static class LevelParser
    {
        public const int MinSize = 3;
        public const int MaxWidth = 12;
        public const int MaxHeight = 14;

        public static LevelDefinition FromJson(string json)
        {
            Dictionary<string, object> root;
            try
            {
                root = MiniJson.AsObject(MiniJson.Parse(json), "level");
            }
            catch (FormatException e)
            {
                throw new LevelFormatException(e.Message);
            }

            try
            {
                var level = FromLayers(
                    MiniJson.GetStringArray(root, "board") ?? throw new LevelFormatException("'board' is required"),
                    MiniJson.GetStringArray(root, "floor"),
                    MiniJson.GetStringArray(root, "cover"),
                    MiniJson.GetStringArray(root, "spawners"));

                level.Id = MiniJson.GetInt(root, "id", 0);
                level.Name = MiniJson.GetString(root, "name", "");
                level.Moves = MiniJson.GetInt(root, "moves", 0);
                level.ColorCount = MiniJson.GetInt(root, "colors", 5);
                level.Difficulty = MiniJson.GetString(root, "difficulty", "normal");

                if (MiniJson.TryGet(root, "collectibles", out var col))
                {
                    var c = MiniJson.AsObject(col, "collectibles");
                    level.Collectibles = new CollectibleSettings
                    {
                        Total = MiniJson.GetInt(c, "total", 0),
                        MaxOnBoard = MiniJson.GetInt(c, "maxOnBoard", 2),
                        SpawnChance = MiniJson.GetDouble(c, "spawnChance", 0.12),
                    };
                }

                if (MiniJson.TryGet(root, "goals", out var goals))
                {
                    foreach (var item in MiniJson.AsArray(goals, "goals"))
                    {
                        var g = MiniJson.AsObject(item, "goal");
                        var def = new GoalDefinition
                        {
                            Type = ParseGoalType(MiniJson.GetString(g, "type", "")),
                            Count = MiniJson.GetInt(g, "count", -1),
                        };
                        string color = MiniJson.GetString(g, "color", null);
                        if (color != null)
                        {
                            if (color.Length != 1 || GemColor.FromChar(color[0]) < 0)
                                throw new LevelFormatException("Unknown goal color '" + color + "'");
                            def.Color = GemColor.FromChar(color[0]);
                        }
                        level.Goals.Add(def);
                    }
                }

                Validate(level);
                return level;
            }
            catch (FormatException e)
            {
                throw new LevelFormatException(e.Message);
            }
        }

        /// <summary>Builds the cell grid from text layers (top row first). Metadata is left at defaults.</summary>
        public static LevelDefinition FromLayers(string[] board, string[] floor = null, string[] cover = null, string[] spawners = null)
        {
            var rows = Normalize(board, "board");
            int height = rows.Length;
            int width = rows[0].Length;
            if (width < MinSize || height < MinSize || width > MaxWidth || height > MaxHeight)
                throw new LevelFormatException("Board size " + width + "x" + height + " is out of range");

            var floorRows = floor == null ? null : Normalize(floor, "floor");
            var coverRows = cover == null ? null : Normalize(cover, "cover");
            var spawnRows = spawners == null ? null : Normalize(spawners, "spawners");
            CheckSize(floorRows, width, height, "floor");
            CheckSize(coverRows, width, height, "cover");
            CheckSize(spawnRows, width, height, "spawners");

            var level = new LevelDefinition { Width = width, Height = height, Cells = new CellSpec[width * height] };
            for (int row = 0; row < height; row++)
            {
                int y = height - 1 - row;
                for (int x = 0; x < width; x++)
                {
                    var spec = ParseBoardToken(rows[row][x], x, y);
                    if (floorRows != null) spec.Floor = ParseDigit(floorRows[row][x], "floor", x, y);
                    if (coverRows != null) spec.Cover = ParseDigit(coverRows[row][x], "cover", x, y);
                    if (spawnRows != null) spec.Spawner = ParseSpawner(spawnRows[row][x], x, y);
                    if (!spec.Exists && (spec.Floor > 0 || spec.Cover > 0 || spec.Spawner))
                        throw new LevelFormatException("Layer data on a hole at " + new GridPos(x, y));
                    if (spec.Cover > 0 && !CanHaveCover(spec))
                        throw new LevelFormatException("Cover is only allowed on gems/specials at " + new GridPos(x, y));
                    level.Cells[y * width + x] = spec;
                }
            }

            if (spawnRows == null)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = height - 1; y >= 0; y--)
                    {
                        if (!level.Cells[y * width + x].Exists) continue;
                        level.Cells[y * width + x].Spawner = true;
                        break;
                    }
                }
            }
            return level;
        }

        static bool CanHaveCover(CellSpec s)
        {
            if (!s.Exists) return false;
            if (s.Content == CellContent.Random) return true;
            if (s.Content == CellContent.Empty) return false;
            return s.Kind == PieceKind.Gem || s.Kind.IsSpecial();
        }

        static string[] Normalize(string[] rows, string layer)
        {
            if (rows.Length == 0) throw new LevelFormatException("'" + layer + "' is empty");
            var result = new string[rows.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                result[i] = (rows[i] ?? "").Replace(" ", "");
                if (result[i].Length != result[0].Length)
                    throw new LevelFormatException("'" + layer + "' row " + i + " has a different width");
            }
            return result;
        }

        static void CheckSize(string[] rows, int width, int height, string layer)
        {
            if (rows == null) return;
            if (rows.Length != height || rows[0].Length != width)
                throw new LevelFormatException("'" + layer + "' must be " + width + "x" + height);
        }

        static CellSpec ParseBoardToken(char t, int x, int y)
        {
            var s = new CellSpec { Exists = true, Content = CellContent.Fixed, Color = GemColor.None };
            switch (t)
            {
                case '.': s.Content = CellContent.Random; return s;
                case '#': s.Exists = false; s.Content = CellContent.Empty; return s;
                case '_': s.Content = CellContent.Empty; return s;
                case '-': s.Kind = PieceKind.RocketH; return s;
                case '|': s.Kind = PieceKind.RocketV; return s;
                case '*': s.Kind = PieceKind.Bomb; return s;
                case '^': s.Kind = PieceKind.Homing; return s;
                case '@': s.Kind = PieceKind.ColorBomb; return s;
                case '$': s.Kind = PieceKind.Collectible; return s;
            }
            if (t >= '1' && t <= '9')
            {
                s.Kind = PieceKind.Box;
                s.Hp = t - '0';
                return s;
            }
            int color = GemColor.FromChar(t);
            if (color >= 0)
            {
                s.Kind = PieceKind.Gem;
                s.Color = color;
                return s;
            }
            throw new LevelFormatException("Unknown board token '" + t + "' at " + new GridPos(x, y));
        }

        static int ParseDigit(char t, string layer, int x, int y)
        {
            if (t == '.' || t == '0' || t == '#') return 0;
            if (t >= '1' && t <= '9') return t - '0';
            throw new LevelFormatException("Bad '" + layer + "' token '" + t + "' at " + new GridPos(x, y));
        }

        static bool ParseSpawner(char t, int x, int y)
        {
            if (t == 'S' || t == 's' || t == '1') return true;
            if (t == '.' || t == '0' || t == '#') return false;
            throw new LevelFormatException("Bad 'spawners' token '" + t + "' at " + new GridPos(x, y));
        }

        static GoalType ParseGoalType(string s)
        {
            switch (s.ToLowerInvariant())
            {
                case "color": return GoalType.Color;
                case "box": return GoalType.Box;
                case "floor": return GoalType.Floor;
                case "cover": return GoalType.Cover;
                case "collectible": return GoalType.Collectible;
                default: throw new LevelFormatException("Unknown goal type '" + s + "'");
            }
        }

        public static void Validate(LevelDefinition level)
        {
            if (level.Moves <= 0) throw new LevelFormatException("'moves' must be > 0");
            if (level.ColorCount < 3 || level.ColorCount > GemColor.Count) throw new LevelFormatException("'colors' must be 3..6");
            if (level.Goals.Count == 0) throw new LevelFormatException("At least one goal is required");
            bool anySpawner = false;
            int collectiblesOnBoard = 0;
            foreach (var c in level.Cells)
            {
                anySpawner |= c.Spawner;
                if (c.Content == CellContent.Fixed && c.Kind == PieceKind.Collectible) collectiblesOnBoard++;
                if (c.Content == CellContent.Fixed && c.Kind == PieceKind.Gem && c.Color >= level.ColorCount)
                    throw new LevelFormatException("Gem color " + GemColor.ToChar(c.Color) + " is not used by a " + level.ColorCount + "-color level");
            }
            if (!anySpawner) throw new LevelFormatException("Level needs at least one spawner");
            foreach (var g in level.Goals)
            {
                if (g.Type == GoalType.Color)
                {
                    if (g.Color < 0 || g.Color >= level.ColorCount) throw new LevelFormatException("Color goal needs a color used by the level");
                    if (g.Count <= 0) throw new LevelFormatException("Color goal needs a positive count");
                }
                if (g.Type == GoalType.Collectible && collectiblesOnBoard == 0 && (level.Collectibles == null || level.Collectibles.Total <= 0))
                    throw new LevelFormatException("Collectible goal needs collectibles on the board or 'collectibles.total'");
            }
        }

        // ---------------------------------------------------------------- writing

        public static string ToJson(LevelDefinition level)
        {
            var root = new Dictionary<string, object>
            {
                ["id"] = level.Id,
                ["name"] = level.Name ?? "",
                ["moves"] = level.Moves,
                ["colors"] = level.ColorCount,
                ["difficulty"] = level.Difficulty ?? "normal",
            };

            var goals = new List<object>();
            foreach (var g in level.Goals)
            {
                var d = new Dictionary<string, object> { ["type"] = g.Type.ToString().ToLowerInvariant() };
                if (g.Color >= 0) d["color"] = GemColor.ToChar(g.Color).ToString();
                if (g.Count >= 0) d["count"] = g.Count;
                goals.Add(d);
            }
            root["goals"] = goals;

            bool hasFloor = false, hasCover = false, customSpawners = false;
            for (int x = 0; x < level.Width; x++)
            {
                int top = -1;
                for (int y = level.Height - 1; y >= 0; y--)
                {
                    if (level.GetCell(x, y).Exists) { top = y; break; }
                }
                for (int y = 0; y < level.Height; y++)
                {
                    var c = level.GetCell(x, y);
                    hasFloor |= c.Floor > 0;
                    hasCover |= c.Cover > 0;
                    if (c.Spawner != (y == top)) customSpawners = true;
                }
            }

            root["board"] = Rows(level, BoardToken);
            if (hasFloor) root["floor"] = Rows(level, c => Digit(c.Floor));
            if (hasCover) root["cover"] = Rows(level, c => Digit(c.Cover));
            if (customSpawners) root["spawners"] = Rows(level, c => c.Spawner ? 'S' : '.');

            if (level.Collectibles != null)
            {
                root["collectibles"] = new Dictionary<string, object>
                {
                    ["total"] = level.Collectibles.Total,
                    ["maxOnBoard"] = level.Collectibles.MaxOnBoard,
                    ["spawnChance"] = level.Collectibles.SpawnChance,
                };
            }
            return MiniJson.Serialize(root, true);
        }

        static List<object> Rows(LevelDefinition level, Func<CellSpec, char> token)
        {
            var rows = new List<object>();
            var sb = new StringBuilder();
            for (int y = level.Height - 1; y >= 0; y--)
            {
                sb.Clear();
                for (int x = 0; x < level.Width; x++) sb.Append(token(level.GetCell(x, y)));
                rows.Add(sb.ToString());
            }
            return rows;
        }

        static char Digit(int v) => v <= 0 ? '.' : (char)('0' + Math.Min(9, v));

        public static char BoardToken(CellSpec c)
        {
            if (!c.Exists) return '#';
            if (c.Content == CellContent.Random) return '.';
            if (c.Content == CellContent.Empty) return '_';
            return PieceToken(c.Kind, c.Color, c.Hp);
        }

        public static char PieceToken(PieceKind kind, int color, int hp)
        {
            switch (kind)
            {
                case PieceKind.Gem: return GemColor.ToChar(color);
                case PieceKind.RocketH: return '-';
                case PieceKind.RocketV: return '|';
                case PieceKind.Bomb: return '*';
                case PieceKind.Homing: return '^';
                case PieceKind.ColorBomb: return '@';
                case PieceKind.Collectible: return '$';
                case PieceKind.Box: return (char)('0' + Math.Max(1, Math.Min(9, hp)));
                default: return '?';
            }
        }
    }
}
