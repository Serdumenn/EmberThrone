using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmberThrone.Core;
using EmberThrone.Core.Simulation;

static class Program
{
    static int Main(string[] args)
    {
        int runs = 200, seed = 1;
        string policyName = "greedy";
        double mistakes = 0.15;
        double? tune = null;
        bool validate = false, write = false, failOnError = false;
        int? traceSeed = null;
        var inputs = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--runs": runs = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                case "--policy": policyName = args[++i]; break;
                case "--mistakes": mistakes = double.Parse(args[++i], CultureInfo.InvariantCulture); break;
                case "--seed": seed = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                case "--validate": validate = true; break;
                case "--tune": tune = double.Parse(args[++i], CultureInfo.InvariantCulture); break;
                case "--write": write = true; break;
                case "--fail-on-error": failOnError = true; break;
                case "--trace": traceSeed = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                default: inputs.Add(args[i]); break;
            }
        }

        var files = new List<string>();
        foreach (var input in inputs)
        {
            if (Directory.Exists(input)) files.AddRange(Directory.GetFiles(input, "*.json").OrderBy(f => f, StringComparer.Ordinal));
            else if (File.Exists(input)) files.Add(input);
            else Console.Error.WriteLine("Not found: " + input);
        }
        if (files.Count == 0)
        {
            Console.Error.WriteLine("No level files given.");
            return 2;
        }

        Func<IPlayerPolicy> factory = policyName == "random"
            ? (Func<IPlayerPolicy>)(() => new RandomPolicy())
            : () => new GreedyPolicy(mistakes);

        if (traceSeed.HasValue)
        {
            foreach (var file in files) Trace(LevelParser.FromJson(File.ReadAllText(file)), traceSeed.Value, factory());
            return 0;
        }

        Console.WriteLine($"policy={policyName} mistakes={mistakes} runs={runs} seed={seed}");
        Console.WriteLine($"{"level",-6} {"name",-22} {"moves",5} {"win%",6} {"left",5} {"lossProg",8} {"spec",5} {"shuf",5} {"time",6}");
        int errorCount = 0;
        foreach (var file in files)
        {
            LevelDefinition level;
            try
            {
                level = LevelParser.FromJson(File.ReadAllText(file));
            }
            catch (LevelFormatException e)
            {
                Console.WriteLine($"{Path.GetFileName(file)}: INVALID - {e.Message}");
                errorCount++;
                continue;
            }

            var sw = Stopwatch.StartNew();
            var report = Simulate(level, runs, seed, factory, validate);
            sw.Stop();
            errorCount += report.Errors.Count;
            Console.WriteLine(Row(level, report, sw.Elapsed.TotalSeconds));
            foreach (var err in report.Errors.Take(3)) Console.WriteLine("    ERROR: " + err.Replace("\n", "\n    "));

            if (tune.HasValue)
            {
                int best = TuneMoves(level, tune.Value, runs, seed, factory, out var bestReport);
                Console.WriteLine($"       -> for {tune.Value:P0} win rate use moves={best} (measured {bestReport.WinRate:P1})");
                if (write && best != level.Moves)
                {
                    var tuned = level.Clone();
                    tuned.Moves = best;
                    File.WriteAllText(file, LevelParser.ToJson(tuned) + "\n");
                    Console.WriteLine("       -> written to " + file);
                }
            }
        }
        return failOnError && errorCount > 0 ? 1 : 0;
    }

    static void Trace(LevelDefinition level, int seed, IPlayerPolicy policy)
    {
        var session = new GameSession(level, seed);
        var rng = new Pcg32(unchecked((ulong)seed * 0x9E3779B97F4A7C15UL + 1UL));
        Console.WriteLine($"== Level {level.Id} '{level.Name}' seed {seed} ({policy.Name})");
        Console.WriteLine(BoardDebug.ToText(session.Board));
        int n = 0;
        while (session.State == GameState.Playing && n++ < 200)
        {
            var move = policy.Choose(session, rng);
            if (!move.HasValue) break;
            var o = session.Apply(move.Value);
            Console.WriteLine();
            Console.WriteLine($"#{n} {move.Value}: rounds={o.ClearRounds} matches={o.Count<MatchEvent>()} " +
                              $"cleared={o.Count<PieceClearedEvent>()} specials+={o.Count<SpecialCreatedEvent>()} " +
                              $"fired={o.Count<SpecialActivatedEvent>()} movesLeft={o.MovesLeft} goals=[{string.Join(", ", session.Goals)}]");
            Console.WriteLine(BoardDebug.ToText(session.Board));
        }
        Console.WriteLine("Result: " + session.State);
    }

    static SimulationReport Simulate(LevelDefinition level, int runs, int seed, Func<IPlayerPolicy> factory, bool validate)
    {
        var results = new LevelSimulator.RunResult[runs];
        Parallel.For(0, runs, i => results[i] = LevelSimulator.PlayOnce(level, seed + i, factory(), null, validate));
        return LevelSimulator.Summarize(level, factory().Name, results);
    }

    static int TuneMoves(LevelDefinition level, double target, int runs, int seed, Func<IPlayerPolicy> factory, out SimulationReport bestReport)
    {
        int lo = 3, hi = Math.Max(level.Moves * 3, 60);
        bestReport = null;
        int best = hi;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            var copy = level.Clone();
            copy.Moves = mid;
            var r = Simulate(copy, runs, seed, factory, false);
            if (r.WinRate >= target)
            {
                best = mid;
                bestReport = r;
                hi = mid - 1;
            }
            else lo = mid + 1;
        }
        if (bestReport == null)
        {
            var copy = level.Clone();
            copy.Moves = best;
            bestReport = Simulate(copy, runs, seed, factory, false);
        }
        return best;
    }

    static string Row(LevelDefinition level, SimulationReport r, double seconds)
    {
        string name = level.Name.Length > 22 ? level.Name.Substring(0, 22) : level.Name;
        return string.Format(CultureInfo.InvariantCulture,
            "{0,-6} {1,-22} {2,5} {3,6:0.0} {4,5:0.0} {5,8:0%} {6,5:0.0} {7,5:0.00} {8,5:0.0}s{9}",
            level.Id, name, level.Moves, r.WinRate * 100, r.AvgMovesLeftOnWin, r.AvgGoalProgressOnLoss,
            r.AvgSpecialsCreated, r.AvgShuffles, seconds, r.Errors.Count > 0 ? "  ERRORS=" + r.Errors.Count : "");
    }
}
