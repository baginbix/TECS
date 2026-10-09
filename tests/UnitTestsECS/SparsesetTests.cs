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

        [Fact]
        public void Remove_One_Success()
        {
            var set = new SparseSet<int>(20);

            var entity = new Entity(1, 10);
            set.Add(entity, 42, 0);
            set.Remove(entity);
            Assert.Equal(0, set.GetDense().Count);
        }

        [Fact]
        public void Remove_Middle_Success()
        {
            var set = new SparseSet<int>(20);

            var entity = new Entity(1, 10);
            set.Add(entity, 42, 0);
            set.Add(new Entity(1, 2), 10, 0);
            set.Add(new Entity(1, 100), 20, 0);
            set.Remove(entity);
            Assert.Equal(0, set.GetDense().Count);
        }
    }
}
