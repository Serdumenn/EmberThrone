using System;
using System.Collections.Generic;

namespace EmberThrone.Core
{
    /// <summary>
    /// Deterministic PCG32 random generator. Produces identical sequences on every
    /// platform and runtime (Unity Mono/IL2CPP, .NET), which keeps replays, bot
    /// simulations and server-side validation in sync. Never use System.Random in Core.
    /// </summary>
    public sealed class Pcg32
    {
        const ulong Multiplier = 6364136223846793005UL;
        const ulong DefaultStream = 0xda3e39cb94b95bdbUL;

        ulong _state;
        readonly ulong _inc;

        public Pcg32(ulong seed) : this(seed, DefaultStream) { }

        public Pcg32(ulong seed, ulong stream)
        {
            _state = 0UL;
            _inc = (stream << 1) | 1UL;
            NextUInt();
            unchecked { _state += seed; }
            NextUInt();
        }

        Pcg32(ulong state, ulong inc, bool raw)
        {
            _state = state;
            _inc = inc;
        }

        public ulong State => _state;
        public ulong Increment => _inc;

        /// <summary>Restores a generator from <see cref="State"/> and <see cref="Increment"/> (save games).</summary>
        public static Pcg32 FromState(ulong state, ulong increment) => new Pcg32(state, increment, true);

        public Pcg32 Clone() => new Pcg32(_state, _inc, true);

        public uint NextUInt()
        {
            unchecked
            {
                ulong old = _state;
                _state = old * Multiplier + _inc;
                uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
                int rot = (int)(old >> 59);
                return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
            }
        }

        /// <summary>Uniform integer in [0, maxExclusive) without modulo bias.</summary>
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            uint bound = (uint)maxExclusive;
            uint threshold = unchecked(0u - bound) % bound;
            while (true)
            {
                uint r = NextUInt();
                if (r >= threshold) return (int)(r % bound);
            }
        }

        /// <summary>Uniform integer in [minInclusive, maxExclusive).</summary>
        public int Next(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return minInclusive + Next(maxExclusive - minInclusive);
        }

        /// <summary>Uniform double in [0, 1) with 53 bits of precision.</summary>
        public double NextDouble()
        {
            ulong a = NextUInt() >> 5;
            ulong b = NextUInt() >> 6;
            return (a * 67108864.0 + b) * (1.0 / 9007199254740992.0);
        }

        public bool Chance(double probability) => NextDouble() < probability;

        /// <summary>Fisher-Yates shuffle.</summary>
        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Next(i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
