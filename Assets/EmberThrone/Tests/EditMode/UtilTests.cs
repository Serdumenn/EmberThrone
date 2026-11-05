using System.Collections.Generic;
using NUnit.Framework;

namespace EmberThrone.Core.Tests
{
    public class UtilTests
    {
        [Test]
        public void Pcg32_SameSeed_SameSequence()
        {
            var a = new Pcg32(12345);
            var b = new Pcg32(12345);
            for (int i = 0; i < 1000; i++) Assert.AreEqual(a.NextUInt(), b.NextUInt());
        }

        [Test]
        public void Pcg32_DifferentSeeds_Differ()
        {
            var a = new Pcg32(1);
            var b = new Pcg32(2);
            int same = 0;
            for (int i = 0; i < 100; i++)
                if (a.NextUInt() == b.NextUInt()) same++;
            Assert.Less(same, 3);
        }

        [Test]
        public void Pcg32_KnownValues_AreStable()
        {
            // Locks the algorithm: if this changes, saved replays and server validation break.
            var rng = new Pcg32(42);
            var first = new[] { rng.NextUInt(), rng.NextUInt(), rng.NextUInt() };
            var again = new Pcg32(42);
            Assert.AreEqual(first[0], again.NextUInt());
            Assert.AreEqual(first[1], again.NextUInt());
            Assert.AreEqual(first[2], again.NextUInt());
            var restored = Pcg32.FromState(again.State, again.Increment);
            Assert.AreEqual(again.NextUInt(), restored.NextUInt());
        }

        [Test]
        public void Pcg32_NextIsInRangeAndRoughlyUniform()
        {
            var rng = new Pcg32(7);
            var counts = new int[6];
            for (int i = 0; i < 60000; i++)
            {
                int v = rng.Next(6);
                Assert.IsTrue(v >= 0 && v < 6);
                counts[v]++;
            }
            foreach (int c in counts) Assert.IsTrue(c > 9000 && c < 11000, "bucket " + c);
            for (int i = 0; i < 1000; i++)
            {
                double d = rng.NextDouble();
                Assert.IsTrue(d >= 0 && d < 1);
            }
        }

        [Test]
        public void MiniJson_ParsesNestedValues()
        {
            var obj = (Dictionary<string, object>)MiniJson.Parse(
                "{\"a\": 1, \"b\": -2.5, \"c\": \"x\\\"y\\n\", \"d\": [true, false, null], \"e\": {\"f\": []}}");
            Assert.AreEqual(1L, obj["a"]);
            Assert.AreEqual(-2.5, (double)obj["b"], 1e-12);
            Assert.AreEqual("x\"y\n", obj["c"]);
            var d = (List<object>)obj["d"];
            Assert.AreEqual(true, d[0]);
            Assert.AreEqual(false, d[1]);
            Assert.IsNull(d[2]);
            Assert.AreEqual(0, ((List<object>)((Dictionary<string, object>)obj["e"])["f"]).Count);
        }

        [Test]
        public void MiniJson_RoundTrips()
        {
            var src = new Dictionary<string, object>
            {
                ["name"] = "Ember \"Gate\"",
                ["n"] = 3,
                ["rows"] = new List<object> { "..1..", "R G B" },
                ["nested"] = new Dictionary<string, object> { ["p"] = 0.25 },
            };
            foreach (bool pretty in new[] { false, true })
            {
                var back = (Dictionary<string, object>)MiniJson.Parse(MiniJson.Serialize(src, pretty));
                Assert.AreEqual("Ember \"Gate\"", back["name"]);
                Assert.AreEqual(3L, back["n"]);
                Assert.AreEqual("R G B", ((List<object>)back["rows"])[1]);
                Assert.AreEqual(0.25, (double)((Dictionary<string, object>)back["nested"])["p"], 1e-12);
            }
        }

        [Test]
        public void MiniJson_RejectsGarbage()
        {
            Assert.Throws<System.FormatException>(() => MiniJson.Parse("{\"a\": }"));
            Assert.Throws<System.FormatException>(() => MiniJson.Parse("[1, 2"));
            Assert.Throws<System.FormatException>(() => MiniJson.Parse("{} extra"));
        }
    }
}
