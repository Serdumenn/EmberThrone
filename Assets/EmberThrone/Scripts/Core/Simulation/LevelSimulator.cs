using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EmberThrone.Core.Simulation
{
    public interface IPlayerPolicy
    {
        string Name { get; }
        Move? Choose(GameSession session, Pcg32 rng);
    }

    public sealed class RandomPolicy : IPlayerPolicy
    {
        public string Name => "random";

        public Move? Choose(GameSession session, Pcg32 rng)
        {
            var moves = session.FindValidMoves();
            if (moves.Count == 0) return null;
            return moves[rng.Next(moves.Count)];
        }
    }

    public sealed class GreedyPolicy : IPlayerPolicy
    {
        public double Mistakes;

        public GreedyPolicy(double mistakes = 0.15)
        {
            Mistakes = mistakes;
        }

        public string Name => "greedy";

        public Move? Choose(GameSession session, Pcg32 rng)
        {
            var moves = session.FindValidMoves();
            if (moves.Count == 0) return null;
            if (rng.Chance(Mistakes)) return moves[rng.Next(moves.Count)];

            Move? best = null;
            double bestScore = double.MinValue;
            foreach (var m in moves)
            {
                var copy = session.Clone(rng.NextUInt());
                var outcome = copy.Apply(m);
                if (!outcome.Accepted) continue;
                double score = Evaluate(session, copy) + rng.NextDouble() * 0.01;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = m;
                }
            }
            return best ?? moves[rng.Next(moves.Count)];
        }

        static double Evaluate(GameSession before, GameSession after)
        {
            if (after.State == GameState.Won) return 1e6 + after.MovesLeft;
            double score = 0;
            for (int i = 0; i < after.Goals.Count; i++)
            {
                var g0 = before.Goals[i];
                var g1 = after.Goals[i];
                int progress = Math.Max(0, g0.Remaining) - Math.Max(0, g1.Remaining);
                score += progress * (g0.Target > 0 ? 100.0 / g0.Target : 1.0) * 10;
                if (!g0.IsComplete && g1.IsComplete) score += 150;
            }
            score += (after.Stats.SpecialsCreated - before.Stats.SpecialsCreated) * 25;
            score += CountSpecials(after.Board) * 5;
            return score;
        }

        static int CountSpecials(Board b)
        {
            int n = 0;
            for (int y = 0; y < b.Height; y++)
                for (int x = 0; x < b.Width; x++)
                {
                    var p = b.CellAt(x, y).Piece;
                    if (p != null && p.IsSpecial) n++;
                }
            return n;
        }
    }

    public sealed class SimulationReport
    {
        public int LevelId;
        public string LevelName;
        public string Policy;
        public int Runs;
        public int Wins;
        public double AvgMovesLeftOnWin;
        public double AvgGoalProgressOnLoss;
        public double AvgSpecialsCreated;
        public double AvgShuffles;
        public int MaxClearRounds;
        public readonly List<string> Errors = new List<string>();

        public double WinRate => Runs == 0 ? 0 : Wins / (double)Runs;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("Level ").Append(LevelId).Append(" '").Append(LevelName).Append("' [").Append(Policy).Append("] ");
            sb.Append("win ").Append((WinRate * 100).ToString("0.0", CultureInfo.InvariantCulture)).Append("% ");
            sb.Append("(").Append(Wins).Append('/').Append(Runs).Append(") ");
            sb.Append("movesLeft ").Append(AvgMovesLeftOnWin.ToString("0.0", CultureInfo.InvariantCulture)).Append(' ');
            sb.Append("lossProgress ").Append((AvgGoalProgressOnLoss * 100).ToString("0", CultureInfo.InvariantCulture)).Append("% ");
            sb.Append("specials ").Append(AvgSpecialsCreated.ToString("0.0", CultureInfo.InvariantCulture)).Append(' ');
            sb.Append("shuffles ").Append(AvgShuffles.ToString("0.00", CultureInfo.InvariantCulture));
            if (Errors.Count > 0) sb.Append(" ERRORS ").Append(Errors.Count);
            return sb.ToString();
        }
    }

    public static class LevelSimulator
    {
        public sealed class RunResult
        {
            public bool Won;
            public int MovesLeft;
            public double GoalProgress;
            public int SpecialsCreated;
            public int Shuffles;
            public int MaxClearRounds;
            public string Error;
        }

        public static RunResult PlayOnce(LevelDefinition level, int seed, IPlayerPolicy policy, GameRules rules = null, bool validate = false, int maxActions = 500)
        {
            var result = new RunResult();
            var session = new GameSession(level, seed, rules);
            var rng = new Pcg32(unchecked((ulong)seed * 0x9E3779B97F4A7C15UL + 1UL));
            int actions = 0;
            try
            {
                while (session.State == GameState.Playing && actions++ < maxActions)
                {
                    var move = policy.Choose(session, rng);
                    if (!move.HasValue) break;
                    var outcome = session.Apply(move.Value);
                    if (!outcome.Accepted)
                    {
                        result.Error = "Bot move rejected: " + move.Value;
                        break;
                    }
                    if (validate)
                    {
                        var errors = BoardDebug.Validate(session.Board, true);
                        if (errors.Count > 0)
                        {
                            result.Error = "After " + move.Value + ": " + string.Join("; ", errors) + "\n" + BoardDebug.ToText(session.Board);
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                result.Error = e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace;
            }

            result.Won = session.State == GameState.Won;
            result.MovesLeft = session.MovesLeft;
            double progress = 0;
            foreach (var g in session.Goals) progress += g.Progress;
            result.GoalProgress = session.Goals.Count == 0 ? 1 : progress / session.Goals.Count;
            result.SpecialsCreated = session.Stats.SpecialsCreated;
            result.Shuffles = session.Stats.Shuffles;
            result.MaxClearRounds = session.Stats.MaxClearRounds;
            return result;
        }

        public static SimulationReport Run(LevelDefinition level, int runs, int baseSeed, Func<IPlayerPolicy> policyFactory, GameRules rules = null, bool validate = false)
        {
            var results = new RunResult[runs];
            string policyName = null;
            for (int i = 0; i < runs; i++)
            {
                var policy = policyFactory();
                policyName = policy.Name;
                results[i] = PlayOnce(level, baseSeed + i, policy, rules, validate);
            }
            return Summarize(level, policyName ?? "?", results);
        }

        public static SimulationReport Summarize(LevelDefinition level, string policyName, IList<RunResult> results)
        {
            var report = new SimulationReport
            {
                LevelId = level.Id,
                LevelName = level.Name,
                Policy = policyName,
                Runs = results.Count,
            };
            double movesLeft = 0, lossProgress = 0, specials = 0, shuffles = 0;
            int losses = 0;
            foreach (var r in results)
            {
                if (r.Error != null) report.Errors.Add(r.Error);
                specials += r.SpecialsCreated;
                shuffles += r.Shuffles;
                if (r.MaxClearRounds > report.MaxClearRounds) report.MaxClearRounds = r.MaxClearRounds;
                if (r.Won)
                {
                    report.Wins++;
                    movesLeft += r.MovesLeft;
                }
                else
                {
                    losses++;
                    lossProgress += r.GoalProgress;
                }
            }
            report.AvgMovesLeftOnWin = report.Wins == 0 ? 0 : movesLeft / report.Wins;
            report.AvgGoalProgressOnLoss = losses == 0 ? 1 : lossProgress / losses;
            report.AvgSpecialsCreated = results.Count == 0 ? 0 : specials / results.Count;
            report.AvgShuffles = results.Count == 0 ? 0 : shuffles / results.Count;
            return report;
        }
    }
}
