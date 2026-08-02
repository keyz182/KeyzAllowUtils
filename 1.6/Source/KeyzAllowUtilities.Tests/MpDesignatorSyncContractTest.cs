using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Verse;
using Xunit;

namespace KeyzAllowUtilities.Tests
{
    // Multiplayer's generic designator sync (Multiplayer.Client.DesignatorPatches) finds sync
    // targets with GetMethod(name, DeclaredOnly|Instance|Public, null, argTypes, null) for exactly
    // these three signatures, on every Designator subtype. MpSelectSimilarUnpatch (in the
    // Compatibility/rwmt.Multiplayer assembly) removes that patch from Designator_SelectSimilar
    // specifically, using the same lookup, and refuses to act unless it resolves to a method
    // declared on Designator_SelectSimilar itself. These tests guard the precondition that
    // refusal rests on: a silent rename/removal here would either leave Select Similar broken
    // under Multiplayer again, or — if the override moved onto a shared base class — risk the
    // unpatch stripping Multiplayer sync from every other designator in the game instead of
    // correctly refusing.
    [TestSubject(typeof(Designator_SelectSimilar))]
    public class MpDesignatorSyncContractTest
    {
        private const BindingFlags DeclaredInstancePublic =
            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public;

        [Fact]
        public void SelectSimilar_DeclaresDesignateSingleCell_MatchingMultiplayersSyncLookup()
        {
            AssertDeclaresOwn(typeof(Designator_SelectSimilar), "DesignateSingleCell", typeof(IntVec3));
        }

        [Fact]
        public void SelectSimilar_DeclaresDesignateMultiCell_MatchingMultiplayersSyncLookup()
        {
            AssertDeclaresOwn(typeof(Designator_SelectSimilar), "DesignateMultiCell", typeof(IEnumerable<IntVec3>));
        }

        [Fact]
        public void SelectSimilar_DeclaresDesignateThing_MatchingMultiplayersSyncLookup()
        {
            AssertDeclaresOwn(typeof(Designator_SelectSimilar), "DesignateThing", typeof(Thing));
        }

        [Fact]
        public void FinishOff_DeclaresDesignateSingleCellAndThing_ButNotMultiCell()
        {
            // FinishOff *does* write Designations, so Multiplayer legitimately syncs and replays
            // it — its rect-drag path reaches DesignateThing via the inherited base
            // Designator.DesignateMultiCell -> DesignateSingleCell -> DesignateThing, which is
            // exactly the path that hit the previously-unguarded Event.current.shift read.
            AssertDeclaresOwn(typeof(Designator_FinishOff), "DesignateSingleCell", typeof(IntVec3));
            AssertDeclaresOwn(typeof(Designator_FinishOff), "DesignateThing", typeof(Thing));

            MethodInfo multiCell = typeof(Designator_FinishOff)
                .GetMethod("DesignateMultiCell", DeclaredInstancePublic, null, new[] { typeof(IEnumerable<IntVec3>) }, null);

            Assert.Null(multiCell);
        }

        private static void AssertDeclaresOwn(Type type, string methodName, Type argType)
        {
            MethodInfo method = type.GetMethod(methodName, DeclaredInstancePublic, null, new[] { argType }, null);

            Assert.NotNull(method);
            Assert.Equal(type, method.DeclaringType);
        }
    }
}
