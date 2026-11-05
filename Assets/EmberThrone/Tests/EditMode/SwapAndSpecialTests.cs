using System.Linq;
using NUnit.Framework;

namespace EmberThrone.Core.Tests
{
    public class SwapAndSpecialTests
    {
        [Test]
        public void ValidSwap_ClearsMatch_AndPiecesFall()
        {
            var s = T.Session(T.Even, T.Odd, "RRBRY", T.Odd, T.Even);
            var o = s.TrySwap(T.P(2, 2), T.P(3, 2));
            Assert.IsTrue(o.Accepted);
            Assert.AreEqual(19, s.MovesLeft);
            Assert.AreEqual(1, o.Count<MatchEvent>());
            Assert.AreEqual(3, o.EventsOf<PieceClearedEvent>().Count(e => e.Color == GemColor.Red));
            Assert.AreEqual(T.Rows("___GY", "YPOPO", "OGYBY", "OGYPO", "YPOGY"), T.Text(s));
            Assert.AreEqual(StepKind.Swap, o.Steps[0].Kind);
            Assert.AreEqual(StepKind.Clear, o.Steps[1].Kind);
            Assert.AreEqual(StepKind.Gravity, o.Steps[2].Kind);
        }

        [Test]
        public void SwapWithoutMatch_IsRevertedAndFree()
        {
            var s = T.Session(T.Even, T.Odd, T.Even, T.Odd, T.Even);
            ulong before = s.Board.ComputeHash();
            var o = s.TrySwap(T.P(0, 0), T.P(1, 0));
            Assert.IsFalse(o.Accepted);
            Assert.AreEqual(20, s.MovesLeft);
            Assert.AreEqual(before, s.Board.ComputeHash());
            var swap = o.EventsOf<SwapEvent>().Single();
            Assert.IsTrue(swap.Reverted);
        }

        [Test]
        public void IllegalSwaps_AreRejectedSilently()
        {
            var s = T.Session(T.Even, T.Odd, "RR1RY", T.Odd, T.Even);
            Assert.IsFalse(s.TrySwap(T.P(0, 0), T.P(2, 0)).Accepted, "not adjacent");
            Assert.IsFalse(s.TrySwap(T.P(2, 2), T.P(3, 2)).Accepted, "boxes cannot move");
            Assert.IsFalse(s.TrySwap(T.P(4, 4), T.P(5, 4)).Accepted, "out of bounds");
            Assert.AreEqual(20, s.MovesLeft);
        }

        [Test]
        public void FourVertical_CreatesRocketWhereDraggedPieceLanded()
        {
            var s = T.Session("OGYPO", T.Even, "OGRPO", "YPBRY", "OGRPO", "YPRGY");
            var o = s.TrySwap(T.P(3, 2), T.P(2, 2));
            Assert.IsTrue(o.Accepted);
            var created = o.EventsOf<SpecialCreatedEvent>().Single();
            Assert.AreEqual(PieceKind.RocketH, created.Kind);
            Assert.AreEqual(T.P(2, 2), created.Pos);
            Assert.AreEqual(PieceKind.RocketH, T.KindAt(s, 2, 0), "rocket fell to the bottom");
        }

        [Test]
        public void LShape_CreatesBomb()
        {
            var s = T.Session(T.Even, T.Odd, "RRBRY", "OGRPO", "YPRGY");
            var o = s.TrySwap(T.P(3, 2), T.P(2, 2));
            Assert.IsTrue(o.Accepted);
            Assert.AreEqual(PieceKind.Bomb, o.EventsOf<SpecialCreatedEvent>().Single().Kind);
            Assert.AreEqual(PieceKind.Bomb, T.KindAt(s, 2, 0));
        }

        [Test]
        public void FiveInARow_CreatesColorBomb()
        {
            var s = T.Session(T.Even, "OGRPO", "RRBRR", T.Odd, T.Even);
            var o = s.TrySwap(T.P(2, 3), T.P(2, 2));
            Assert.IsTrue(o.Accepted);
            var created = o.EventsOf<SpecialCreatedEvent>().Single();
            Assert.AreEqual(PieceKind.ColorBomb, created.Kind);
            Assert.AreEqual(T.P(2, 2), created.Pos);
        }

        [Test]
        public void TapRocket_ClearsRow()
        {
            var s = T.Session(T.Even, T.Odd, "YP-GY", T.Odd, T.Even);
            var o = s.TryTap(T.P(2, 2));
            Assert.IsTrue(o.Accepted);
            Assert.AreEqual(19, s.MovesLeft);
            var act = o.EventsOf<SpecialActivatedEvent>().Single();
            Assert.AreEqual(PieceKind.RocketH, act.Kind);
            Assert.AreEqual(5, act.Area.Length);
            Assert.AreEqual(T.P(2, 2), act.Area[0], "rocket hits outward from its own cell");
            Assert.AreEqual(4, o.EventsOf<PieceClearedEvent>().Count(e => e.Kind == PieceKind.Gem));
            Assert.AreEqual("_____", T.Text(s).Split('\n')[0], "top row empties (no spawners)");
        }

        [Test]
        public void TapOnGem_IsRejected()
        {
            var s = T.Session(T.Even, T.Odd, T.Even, T.Odd, T.Even);
            Assert.IsFalse(s.TryTap(T.P(1, 1)).Accepted);
            Assert.AreEqual(20, s.MovesLeft);
        }

        [Test]
        public void Bomb_Clears5x5()
        {
            var s = T.Session("YPOGYPO", "OGYPOGY", "YPOGYPO", "OGY*OGY", "YPOGYPO", "OGYPOGY", "YPOGYPO");
            var o = s.TryTap(T.P(3, 3));
            Assert.AreEqual(25, o.EventsOf<SpecialActivatedEvent>().Single().Area.Length);
            var blast = o.Steps.First(st => st.Kind == StepKind.Clear);
            Assert.AreEqual(24, blast.Events.OfType<PieceClearedEvent>().Count());
        }

        [Test]
        public void ColorBombSwappedWithGem_ClearsThatColor()
        {
            var s = T.Session(T.Even, T.Odd, "YP@GY", T.Odd, T.Even);
            var o = s.TrySwap(T.P(2, 2), T.P(3, 2));
            Assert.IsTrue(o.Accepted);
            var act = o.EventsOf<SpecialActivatedEvent>().Single();
            Assert.AreEqual(GemColor.Green, act.TargetColor);
            // Only look at the blast itself; later cascades may clear other colors.
            var blast = o.Steps.First(st => st.Kind == StepKind.Clear).Events.OfType<PieceClearedEvent>().ToList();
            Assert.AreEqual(5, blast.Count);
            Assert.IsTrue(blast.All(e => e.Color == GemColor.Green));
        }

        [Test]
        public void RocketPlusRocket_FiresCross()
        {
            var s = T.Session(T.Even, T.Odd, "Y-|GY", T.Odd, T.Even);
            var o = s.TrySwap(T.P(1, 2), T.P(2, 2));
            var act = o.EventsOf<SpecialActivatedEvent>().Single();
            Assert.AreEqual(ComboKind.RocketRocket, act.Combo);
            Assert.AreEqual(T.P(2, 2), act.Pos, "combo centers where the dragged piece landed");
            Assert.AreEqual(9, act.Area.Length);
            Assert.AreEqual(7, o.Count<PieceClearedEvent>());
            Assert.AreEqual(1, s.Stats.Combos);
        }

        [Test]
        public void RocketPlusBomb_FiresThreeRowsAndColumns()
        {
            var s = T.Session("YPOGYPO", "OGYPOGY", "YPOGYPO", "OGY*-GY", "YPOGYPO", "OGYPOGY", "YPOGYPO");
            var o = s.TrySwap(T.P(4, 3), T.P(3, 3));
            var act = o.EventsOf<SpecialActivatedEvent>().Single();
            Assert.AreEqual(ComboKind.RocketBomb, act.Combo);
            Assert.AreEqual(7 * 3 + 7 * 3 - 9, act.Area.Length);
        }

        [Test]
        public void ColorBombPlusColorBomb_HitsWholeBoard()
        {
            var s = T.Session(T.Even, T.Odd, "Y@@GY", T.Odd, T.Even);
            var o = s.TrySwap(T.P(1, 2), T.P(2, 2));
            var act = o.EventsOf<SpecialActivatedEvent>().Single();
            Assert.AreEqual(ComboKind.ColorColor, act.Combo);
            Assert.AreEqual(25, act.Area.Length);
            Assert.AreEqual(23, o.Count<PieceClearedEvent>());
            Assert.AreEqual(T.Rows("_____", "_____", "_____", "_____", "_____"), T.Text(s));
        }

        [Test]
        public void ColorBombPlusRocket_ConvertsMostCommonColorAndFiresAll()
        {
            var s = T.Session(T.Even, T.Odd, "Y@-GY", T.Odd, T.Even);
            var o = s.TrySwap(T.P(1, 2), T.P(2, 2));
            var act = o.EventsOf<SpecialActivatedEvent>().First();
            Assert.AreEqual(ComboKind.ColorRocket, act.Combo);
            int converted = o.Count<PieceConvertedEvent>();
            Assert.Greater(converted, 3);
            Assert.AreEqual(converted + 1, o.Count<SpecialActivatedEvent>());
        }

        [Test]
        public void ChainReaction_RocketTriggersBomb()
        {
            var s = T.Session("YPOGYPO", "OGYPOGY", "YPOGYPO", "-GY*OGY", "YPOGYPO", "OGYPOGY", "YPOGYPO");
            var o = s.TryTap(T.P(0, 3));
            Assert.AreEqual(2, o.Count<SpecialActivatedEvent>());
            Assert.AreEqual(2, s.Stats.SpecialsActivated);
            var clearSteps = o.Steps.Where(st => st.Kind == StepKind.Clear).ToList();
            Assert.AreEqual(PieceKind.RocketH, clearSteps[0].Events.OfType<SpecialActivatedEvent>().Single().Kind);
            Assert.AreEqual(PieceKind.Bomb, clearSteps[1].Events.OfType<SpecialActivatedEvent>().Single().Kind, "the bomb fires in the next wave");
            Assert.AreEqual(StepKind.Clear, o.Steps[1].Kind, "waves are consecutive, before gravity");
        }

        [Test]
        public void Homing_StrikesGoalBox()
        {
            var level = T.Level(new[] { "OGYP1", T.Even, T.Odd, T.Even, "^GYPO" }, goals: new GoalDefinition(GoalType.Box));
            var s = T.Session(level);
            var o = s.TryTap(T.P(0, 0));
            var strike = o.EventsOf<HomingStrikeEvent>().Single();
            Assert.AreEqual(T.P(4, 4), strike.Target);
            Assert.AreEqual(GameState.Won, s.State);
        }

        [Test]
        public void HomingPlusHoming_LaunchesThreeStrikes()
        {
            var s = T.Session(T.Even, T.Odd, "Y^^GY", T.Odd, T.Even);
            var o = s.TrySwap(T.P(1, 2), T.P(2, 2));
            Assert.AreEqual(ComboKind.HomingHoming, o.EventsOf<SpecialActivatedEvent>().Single().Combo);
            var targets = o.EventsOf<HomingStrikeEvent>().Select(e => e.Target).ToList();
            Assert.AreEqual(3, targets.Count);
            Assert.AreEqual(3, targets.Distinct().Count(), "each strike picks a different cell");
        }

        [Test]
        public void SpecialSwappedWithGem_Fires()
        {
            var s = T.Session(T.Even, T.Odd, "YP-GY", T.Odd, T.Even);
            var o = s.TrySwap(T.P(2, 2), T.P(2, 3));
            Assert.IsTrue(o.Accepted);
            var act = o.EventsOf<SpecialActivatedEvent>().Single();
            Assert.AreEqual(T.P(2, 3), act.Pos, "fires from where it was moved to");
        }
    }
}
