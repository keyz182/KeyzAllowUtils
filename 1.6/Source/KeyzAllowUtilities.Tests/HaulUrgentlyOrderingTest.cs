using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Verse;
using Xunit;

namespace KeyzAllowUtilities.Tests
{
    // WorkGiver_HaulUrgently.PotentialWorkThingsGlobal must be a pure, deterministic function of
    // (pawn position, designation state) — the previous Rand-based sampling made a different
    // number of Rand calls depending on a stale, unsaved search pool, which desynced Multiplayer
    // sessions when more than one item was designated. These tests cover the two internal
    // primitives that replaced it: the total order and the bounded top-K insertion.
    [TestSubject(typeof(WorkGiver_HaulUrgently))]
    public class HaulUrgentlyOrderingTest
    {
        private static readonly IntVec3 Origin = new(0, 0, 0);

        // ── CompareCandidates: nearer/farther ─────────────────────────────────

        [Fact]
        public void CompareCandidates_NearerCandidate_SortsFirst()
        {
            int result = WorkGiver_HaulUrgently.CompareCandidates(
                Origin, new IntVec3(1, 0, 0), 1, new IntVec3(5, 0, 0), 2);

            Assert.True(result < 0);
        }

        [Fact]
        public void CompareCandidates_FartherCandidate_SortsLast()
        {
            int result = WorkGiver_HaulUrgently.CompareCandidates(
                Origin, new IntVec3(5, 0, 0), 1, new IntVec3(1, 0, 0), 2);

            Assert.True(result > 0);
        }

        // ── Tie-break: equal distance resolves by thingIDNumber ───────────────

        [Fact]
        public void CompareCandidates_EqualDistance_TieBreaksByLowerId()
        {
            // (3,0,4) and (4,0,3) are both distance 25 (squared) from the origin.
            IntVec3 a = new(3, 0, 4);
            IntVec3 b = new(4, 0, 3);
            Assert.Equal(a.LengthHorizontalSquared, b.LengthHorizontalSquared);

            Assert.True(WorkGiver_HaulUrgently.CompareCandidates(Origin, a, 10, b, 20) < 0);
            Assert.True(WorkGiver_HaulUrgently.CompareCandidates(Origin, b, 20, a, 10) > 0);
        }

        [Fact]
        public void CompareCandidates_SamePositionAndId_ReturnsZero()
        {
            IntVec3 pos = new(7, 0, 2);
            Assert.Equal(0, WorkGiver_HaulUrgently.CompareCandidates(Origin, pos, 42, pos, 42));
        }

        // ── Total order: antisymmetry and no accidental zero for distinct entries ─

        [Fact]
        public void CompareCandidates_IsAntisymmetric_OverAGridOfPositionsAndIds()
        {
            var points = new List<(IntVec3 pos, int id)>();
            for (int x = -3; x <= 3; x++)
            for (int z = -3; z <= 3; z++)
                points.Add((new IntVec3(x, 0, z), (x + 3) * 7 + (z + 3)));

            foreach (var p in points)
            foreach (var q in points)
            {
                int forward = WorkGiver_HaulUrgently.CompareCandidates(Origin, p.pos, p.id, q.pos, q.id);
                int backward = WorkGiver_HaulUrgently.CompareCandidates(Origin, q.pos, q.id, p.pos, p.id);
                Assert.Equal(-Math.Sign(forward), Math.Sign(backward));

                if (p.pos != q.pos || p.id != q.id)
                {
                    Assert.NotEqual(0, forward);
                }
            }
        }

        [Fact]
        public void CompareCandidates_IsExact_ForALargeMap()
        {
            // A 500x500 map's largest possible squared distance (500*500*2) is well within
            // int range — LengthHorizontalSquared never overflows for any RimWorld map size.
            IntVec3 far = new(500, 0, 500);
            int expected = 500 * 500 + 500 * 500;
            Assert.Equal(expected, (far - Origin).LengthHorizontalSquared);
        }

        // ── InsertCappedByKey: bounded top-K, Thing-independent ───────────────

        private static int IntCompare(int a, int b) => a.CompareTo(b);

        [Fact]
        public void InsertCappedByKey_MatchesOrderByTake_BelowCap()
        {
            int[] input = [5, 3, 8, 1, 9, 2];
            List<int> buffer = [];
            foreach (int x in input) WorkGiver_HaulUrgently.InsertCappedByKey(buffer, x, 10, IntCompare);

            Assert.Equal(input.OrderBy(x => x).ToList(), buffer);
        }

        [Fact]
        public void InsertCappedByKey_MatchesOrderByTake_AtCap()
        {
            int[] input = [5, 3, 8, 1, 9, 2];
            List<int> buffer = [];
            foreach (int x in input) WorkGiver_HaulUrgently.InsertCappedByKey(buffer, x, input.Length, IntCompare);

            Assert.Equal(input.OrderBy(x => x).ToList(), buffer);
        }

        [Fact]
        public void InsertCappedByKey_MatchesOrderByTake_AboveCap()
        {
            int[] input = [5, 3, 8, 1, 9, 2, 7, 4, 6, 0];
            const int cap = 4;
            List<int> buffer = [];
            foreach (int x in input) WorkGiver_HaulUrgently.InsertCappedByKey(buffer, x, cap, IntCompare);

            Assert.Equal(input.OrderBy(x => x).Take(cap).ToList(), buffer);
        }

        [Fact]
        public void InsertCappedByKey_NeverExceedsCap()
        {
            const int cap = 5;
            List<int> buffer = [];
            for (int i = 0; i < 50; i++)
            {
                WorkGiver_HaulUrgently.InsertCappedByKey(buffer, i, cap, IntCompare);
                Assert.True(buffer.Count <= cap);
            }
        }

        [Fact]
        public void InsertCappedByKey_OutputIsInvariantUnderInputPermutation()
        {
            // This is the desync invariant: whatever order candidates arrive in (which, upstream,
            // depends only on designation-manager iteration order and is itself not guaranteed
            // stable), the final sorted+capped result must be identical every time.
            int[] baseInput = [15, 3, 42, 7, 23, 8, 1, 99, 5, 12, 30, 2, 17, 9, 25];
            const int cap = 6;
            List<int> expected = baseInput.OrderBy(x => x).Take(cap).ToList();

            var rng = new Random(12345);
            for (int trial = 0; trial < 100; trial++)
            {
                int[] shuffled = (int[])baseInput.Clone();
                for (int i = shuffled.Length - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
                }

                List<int> buffer = [];
                foreach (int x in shuffled) WorkGiver_HaulUrgently.InsertCappedByKey(buffer, x, cap, IntCompare);

                Assert.Equal(expected, buffer);
            }
        }

        [Fact]
        public void InsertCappedByKey_RepeatedCallsOnSameInput_AreIdentical()
        {
            int[] input = [5, 3, 8, 1, 9, 2];
            const int cap = 4;

            List<int> first = [];
            foreach (int x in input) WorkGiver_HaulUrgently.InsertCappedByKey(first, x, cap, IntCompare);

            List<int> second = [];
            foreach (int x in input) WorkGiver_HaulUrgently.InsertCappedByKey(second, x, cap, IntCompare);

            Assert.Equal(first, second);
        }
    }
}
