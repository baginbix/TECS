using TECS;
using TECS.Query;

namespace UnitTestsECS
{
    [Query]
    public ref struct QueryPos
    {
        public ref Position pos;
    }

    public class SparsesetTests
    {
        [Fact]
        public void SparseSet_ShouldHandlePageBoundaries()
        {
            var ecs = new ECS();

            // PAGE_SIZE is 4096. Let's spawn 5000 to force a page allocation!
            int spawnCount = 5000;

            for (int i = 0; i < spawnCount; i++)
            {
                Entity e = ecs.CreateEntity();
                ecs.InsertComponent(e, new Position { X = i });
            }

            // Assert all 5000 entities can be iterated successfully
            int count = 0;
            var query = new Query<QueryPos>(ecs, 0);
            foreach (var item in query)
            {
                count++;
            }

            Assert.Equal(spawnCount, count);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(100_000)]
        public void Insert_AllInserted_ExpectedCount(int expectedCount)
        {
            var set = new SparseSet<int>(expectedCount);

            for (int i = 0; i < expectedCount; i++)
            {
                set.Add(new Entity(i, 0), i, 0);
            }

            Assert.Equal(expectedCount, set.Size);
        }

     public struct EmptyTag { }
        // ------------------------------------------------------------------
        // Guards: NRE / IndexOutOfRange on missing pages)
        // ------------------------------------------------------------------

        [Fact]
        public void Remove_EmptySet_DoesNothing()
        {
            var set = new SparseSet<int>(16);

            set.Remove(new Entity(1, 0));

            Assert.Equal(0, set.Size);
        }

        [Fact]
        public void Remove_EntityInUnallocatedPage_DoesNotThrow()
        {
            var set = new SparseSet<int>(16);
            set.Add(new Entity(1, 0), 42, 0);

            // Id 100_000 lives in a page far beyond what was ever allocated.
            set.Remove(new Entity(100_000, 0));

            Assert.Equal(1, set.Size);
        }

        [Fact]
        public void Remove_UnusedEntryInAllocatedPage_DoesNothing()
        {
            var set = new SparseSet<int>(16);
            set.Add(new Entity(1, 0), 42, 0);

            // Page 0 exists (entity 1 allocated it), but entry for Id 2 is empty.
            set.Remove(new Entity(2, 0));

            Assert.Equal(1, set.Size);
        }

        [Fact]
        public void Remove_SameEntityTwice_SecondRemoveIsNoOp()
        {
            var set = new SparseSet<int>(16);
            var a = new Entity(1, 0);
            set.Add(a, 42, 0);

            set.Remove(a);
            set.Remove(a);

            Assert.Equal(0, set.Size);
            Assert.False(set.Contains(a));
        }

        // ------------------------------------------------------------------
        // Swap-remove correctness
        // ------------------------------------------------------------------

        [Fact]
        public void Remove_Middle_MovesLastIntoVacatedSlot()
        {
            var set = new SparseSet<int>(16);
            var a = new Entity(1, 0);
            var b = new Entity(2, 0);
            var c = new Entity(3, 0);
            set.Add(a, 10, 1);
            set.Add(b, 20, 2);
            set.Add(c, 30, 3);

            set.Remove(b);

            Assert.Equal(2, set.Size);
            Assert.False(set.Contains(b));
            Assert.True(set.Contains(a));
            Assert.True(set.Contains(c));
        }

        [Fact]
        public void Remove_Middle_MovedEntitySparseEntryPointsAtVacatedSlot()
        {
            var set = new SparseSet<int>(16);
            var a = new Entity(1, 0);
            var b = new Entity(2, 0);
            var c = new Entity(3, 0);
            set.Add(a, 10, 1);
            set.Add(b, 20, 2);
            set.Add(c, 30, 3);

            set.Remove(b); // C (last, dense index 2) must swap into dense index 1.

            // The moved entity must still be resolvable through the sparse page...
            Assert.False(set.GetReadonlyValue(c).IsNone);
            Assert.False(set.GetReadonlyValue(a).IsNone);
            // ...and the dense array must still hold exactly the survivors' data.
            Assert.Equal(new[] { 10, 30 }, set.GetDense().OrderBy(x => x).ToArray());
        }

        [Fact]
        public void Remove_LastElement_LeavesNoStaleMappings()
        {
            var set = new SparseSet<int>(16);
            var a = new Entity(1, 0);
            var b = new Entity(2, 0);
            var c = new Entity(3, 0);
            set.Add(a, 10, 1);
            set.Add(b, 20, 2);
            set.Add(c, 30, 3);

            set.Remove(c); // denseId == last index: the self-swap edge case

            Assert.Equal(2, set.Size);
            Assert.False(set.Contains(c));
            Assert.False(set.GetReadonlyValue(c).IsNone == false);
            Assert.True(set.Contains(a));
            Assert.True(set.Contains(b));
        }

        // ------------------------------------------------------------------
        // Lockstep invariant : ticks desynced after Remove
        // ------------------------------------------------------------------

        [Fact]
        public void Remove_MaintainsTicksDenseEntitiesInLockstep()
        {
            var set = new SparseSet<int>(16);
            for (int i = 1; i <= 5; i++)
                set.Add(new Entity(i, 0), i * 10, (uint)i);

            set.Remove(new Entity(2, 0));
            set.Remove(new Entity(4, 0));

            // The invariant that bugs 1 and 2 both violated:
            // dense, ticks, and denseEntities must describe the same elements.
            Assert.Equal(set.Size, set.GetEntities().Count);
            Assert.Equal(set.Size, set.GetLastTicks().Length);
        }

        // ------------------------------------------------------------------
        // Stale sparse entry (classic sparse-set bug)
        // ------------------------------------------------------------------

        [Fact]
        public void Remove_ThenReAdd_EntityIsUsableAgain()
        {
            var set = new SparseSet<int>(16);
            var e = new Entity(7, 0);
            set.Add(e, 42, 1);

            set.Remove(e);
            Assert.Equal(0, set.Size);

            set.Add(e, 99, 2);

            Assert.Equal(1, set.Size);
            Assert.True(set.Contains(e));
            Assert.False(set.GetReadonlyValue(e).IsNone);
            Assert.Equal(new[] { 99 }, set.GetDense().ToArray());
        }

        [Fact]
        public void Remove_AllEntities_SetCanBeRebuiltFromScratch()
        {
            var set = new SparseSet<int>(16);
            var entities = new List<Entity>();
            for (int i = 1; i <= 10; i++)
            {
                var e = new Entity(i, 0);
                entities.Add(e);
                set.Add(e, i * 100, 0);
            }

            foreach (var e in entities)
                set.Remove(e);

            Assert.Equal(0, set.Size);
            Assert.Equal(0, set.GetEntities().Count);
            Assert.Equal(0, set.GetLastTicks().Length);

            // Sparse must be fully cleared: re-adding must append, not overwrite.
            set.Add(new Entity(1, 0), 555, 3);
            Assert.Equal(1, set.Size);
            Assert.Equal(new[] { 555 }, set.GetDense().ToArray());
        }

        // ------------------------------------------------------------------
        // isTag code path
        // ------------------------------------------------------------------

        [Fact]
        public void Remove_TagComponent_MaintainsEntityList()
        {
            var set = new SparseSet<EmptyTag>(8);
            var a = new Entity(1, 0);
            var b = new Entity(2, 0);
            var c = new Entity(3, 0);
            set.Add(a, default, 0);
            set.Add(b, default, 0);
            set.Add(c, default, 0);

            set.Remove(b);

            // Tags have no dense/ticks storage; GetEntities() is the source of truth.
            Assert.Equal(2, set.GetEntities().Count);
            Assert.False(set.Contains(b));
            Assert.True(set.Contains(a));
            Assert.True(set.Contains(c));
        }

        // ------------------------------------------------------------------
        // Randomized invariant check (deterministic seed)
        // ------------------------------------------------------------------

        [Fact]
        public void Remove_MixedWorkload_AllInvariantsHold()
        {
            var set = new SparseSet<int>(64);
            var remaining = new HashSet<Entity>();
            var removed = new HashSet<Entity>();

            for (int i = 0; i < 100; i++)
            {
                var e = new Entity(i + 1, 0);
                set.Add(e, i * 3 + 1, (uint)i);
                remaining.Add(e);
            }

            // Deterministic removal pattern: seed makes this a stable regression test.
            var rng = new Random(42);
            for (int n = 0; n < 50; n++)
            {
                var e = remaining.ElementAt(rng.Next(remaining.Count));
                set.Remove(e);
                remaining.Remove(e);
                removed.Add(e);
            }

            // 1. Lockstep invariant
            Assert.Equal(remaining.Count, set.Size);
            Assert.Equal(remaining.Count, set.GetEntities().Count);
            Assert.Equal(remaining.Count, set.GetLastTicks().Length);

            // 2. Membership matches bookkeeping exactly
            foreach (var e in remaining)
                Assert.True(set.Contains(e), $"Entity {e.Id} should still be in the set");
            foreach (var e in removed)
                Assert.False(set.Contains(e), $"Entity {e.Id} should have been removed");

            // 3. Data survived the swap-removes intact
            var expected = remaining.Select(e => (int)(e.Id * 3 - 2)).OrderBy(x => x).ToArray();
            Assert.Equal(expected, set.GetDense().OrderBy(x => x).ToArray());
        }

        // ------------------------------------------------------------------
        // This test FAILS until Add's overwrite path is fixed:
        //     ticks[entity.Id] = currentTick;
        // must become
        //     ticks[sparse[pageIndex][pageOffset]] = currentTick;
        // ------------------------------------------------------------------

        [Fact]
        public void Add_Overwrite_UpdatesTickAtDensePosition()
        {
            var set = new SparseSet<int>(16);
            // Ids deliberately do not match dense positions:
            // dense index 0 = entity Id 2, index 1 = entity Id 0, index 2 = entity Id 1.
            var first = new Entity(2, 0);
            var second = new Entity(0, 0);
            var third = new Entity(1, 0);
            set.Add(first, 1, 1);
            set.Add(second, 2, 2);
            set.Add(third, 3, 3);

            // Overwrite the component of the entity at dense position 0.
            set.Add(first, 99, 100);

            Assert.Equal(3, set.Size); // overwrite, not append
            Assert.Equal(99, set.GetDense()[0]);

            // The new tick must land at dense position 0 — not at sparse[Id].
            Assert.Equal(100u, set.GetLastTicks()[0].CurrentTick);
            // And it must not have stomped anyone else's tick.
            Assert.Equal(2u, set.GetLastTicks()[1].CurrentTick);
            Assert.Equal(3u, set.GetLastTicks()[2].CurrentTick);
        }

    }
}
