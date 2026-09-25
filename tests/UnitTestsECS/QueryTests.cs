using TECS;
using TECS.Commands;
using TECS.Query;
using TECS.Result;

namespace UnitTestsECS
{
    // 1. Define Components and Tags
    public struct Position
    {
        public float X;
    }

    public struct Velocity
    {
        public float Dx;
    }

    public struct Health
    {
        public int Hp;
    }

    public struct PlayerTag { }

    public struct Frozen { }

    [Query]
    public ref struct SpanQuery
    {
        public Span<Position> positions;
    }

    // 2. Define the Query Struct with the required attribute!
    [Query]
    public ref struct MovementQuery
    {
        public ref Position Pos;
        public ref Velocity Vel;
    }

    [Query]
    public ref struct PositionQuery
    {
        public ref Position Pos;
    }

    [Query]
    public ref struct NothingQuery
    {
        public ref Health Hp;
    }

    [Query]
    [With<PlayerTag>]
    public ref struct WithQuery
    {
        public ref Position Pos;
    }

    [Query]
    [Without<PlayerTag>]
    public ref struct WithoutQuery
    {
        public ref Position Pos;
    }

    [Query]
    [With<Frozen>]
    [Without<PlayerTag>]
    public ref struct WithWithoutQuery
    {
        public ref Data Data;
    }

    [Query]
    [Changed<Position>]
    public ref struct ChangedQuery
    {
        public ref Position pos;
    }

    [Query]
    [Changed<Position>]
    [Changed<Velocity>]
    public ref struct ChangedTwoComponentsQuery
    {
        public ref Position pos;
        public ref Velocity vel;
    }

    [Query]
    public ref struct OptionalQuery
    {
        public ref Position position;
        public Option<Velocity> optionalVel;
    }

    [Query]
    public ref struct OptionalMutQuery
    {
        public ref Position position;
        public OptionMut<Velocity> optionVel;
    }

    public record struct Data(int X);

    public static class TestSystems
    {
        [System]
        public static void CountMovementSystem(Query<MovementQuery> query)
        {
            foreach (var item in query)
            {
                item.Pos.X += item.Vel.Dx;
            }
        }

        [System]
        public static void QueryNothing(Query<NothingQuery> query)
        {
            foreach (var item in query)
            {
                item.Hp.Hp += 1;
            }
        }

        [System]
        public static void QuerySingle(Query<MovementQuery> query)
        {
            var item = query.Single();
        }
    }

    public class QueryTests
    {
        [Fact]
        public void System_HandleQueryingNothing()
        {
            var ecs = new ECS();
            var entity = ecs.CreateEntity();

            var query = new Query<NothingQuery>(ecs, 0);
            var nothing = query.Single();
            Assert.Equal(QuerySingleError.Empty, nothing.Error);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(100)]
        public void Query_ProcessExactEntityCount(int expectedCount)
        {
            var ecs = new ECS();

            for (int i = 0; i < expectedCount; i++)
            {
                var entity = ecs.CreateEntity();
                ecs.InsertComponent(entity, new Position { X = 5 });
            }

            var query = new Query<PositionQuery>(ecs, 0);
            int actualCount = 0;
            foreach (var _ in query)
            {
                actualCount++;
            }

            Assert.Equal(expectedCount, actualCount);
        }

        [Fact]
        public void System_QuerySingleMultipleComponents_ThrowsException()
        {
            var ecs = new ECS();
            var entity1 = ecs.CreateEntity();
            ecs.InsertComponent(entity1, new Position { X = 0 });
            ecs.InsertComponent(entity1, new Velocity { Dx = 5 });

            var entity2 = ecs.CreateEntity();
            ecs.InsertComponent(entity2, new Position { X = 10 });
            ecs.InsertComponent(entity2, new Velocity { Dx = 15 });

            var query = new Query<MovementQuery>(ecs, 0);
            var item = query.Single();

            Assert.Equal(QuerySingleError.Multiple, item.Error);
        }

        [Fact]
        public void System_QueryWithTag_Success()
        {
            var ecs = new ECS();
            var entity = ecs.CreateEntity();
            ecs.InsertComponent(entity, new Position { X = 5 });
            ecs.InsertComponent(entity, new PlayerTag());

            entity = ecs.CreateEntity();
            ecs.InsertComponent(entity, new Position { X = 0 });

            var query = new Query<WithQuery>(ecs, 0);
            int count = 0;
            foreach (var q in query)
                count++;
            Assert.Equal(1, count);
        }

        [Fact]
        public void System_QueryWithoutTag_Success()
        {
            var ecs = new ECS();
            var entity = ecs.CreateEntity();
            ecs.InsertComponent(entity, new Position { X = 5 });
            ecs.InsertComponent(entity, new PlayerTag());

            entity = ecs.CreateEntity();
            ecs.InsertComponent(entity, new Position { X = 0 });
            entity = ecs.CreateEntity();
            ecs.InsertComponent(entity, new Position { X = 0 });

            var query = new Query<WithoutQuery>(ecs, 0);
            int count = 0;
            foreach (var q in query)
                count++;
            Assert.Equal(2, count);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        //This one checkes so that paged data is correct
        [InlineData(5000)]
        public void Query_WithFrozenAndWithoutPlayerTagComponent_ValidateDataIntegrity(
            int expectedCount
        )
        {
            var ecs = new ECS();
            const int dataValue = 42;

            for (int i = 0; i < expectedCount; i++)
            {
                // With: PlayerTag
                // Without: Frozen
                var entity = ecs.CreateEntity();
                ecs.InsertComponent(entity, new Data { X = 5 });
                ecs.InsertComponent(entity, new PlayerTag());

                // With: Frozen
                // Without: PlayerTag
                entity = ecs.CreateEntity();
                ecs.InsertComponent(entity, new Data { X = dataValue });
                ecs.InsertComponent(entity, new Frozen());

                // With: PlayerTag, Frozen
                // Without:
                entity = ecs.CreateEntity();
                ecs.InsertComponent(entity, new Data { X = 0 });
                ecs.InsertComponent(entity, new Frozen());
                ecs.InsertComponent(entity, new PlayerTag());
            }

            var query = new Query<WithWithoutQuery>(ecs, 0);
            int actualCount = 0;
            foreach (var q in query)
            {
                actualCount++;
                Assert.Equal(dataValue, q.Data.X);
            }
            Assert.Equal(expectedCount, actualCount);
        }

        [Fact]
        public void Query_ChangedPosition_OnlyRunWhenChanged()
        {
            ECS world = new();
            CommandBuffer cmd = new();
            cmd.SpawnEntity().With(new Position { X = 42 });
            cmd.SpawnEntity().With(new Position { X = 420 });
            cmd.Flush(world);
            var positions = world.GetSparseSet<Position>();

            positions.GetLastTicks()[0] = 20;
            var query = new Query<ChangedQuery>(world, 10);
            int actualFound = 0;
            foreach (var q in query)
            {
                Assert.Equal(42, q.pos.X);
                actualFound++;
            }

            Assert.Equal(1, actualFound);
        }

        [Fact]
        public void Query_ChangedPositionAndVelocity_OnlyRunWhenChanged()
        {
            ECS world = new();
            CommandBuffer cmd = new();
            cmd.SpawnEntity().With(new Position { X = 42 }).With(new Velocity());
            cmd.SpawnEntity().With(new Position { X = 420 }).With(new Velocity());
            cmd.Flush(world);
            var positions = world.GetSparseSet<Position>();

            var velocities = world.GetSparseSet<Velocity>();
            positions.GetLastTicks()[0] = 20;
            velocities.GetLastTicks()[0] = 20;
            var query = new Query<ChangedTwoComponentsQuery>(world, 10);
            int actualFound = 0;
            foreach (var q in query)
            {
                Assert.Equal(42, q.pos.X);
                actualFound++;
            }

            Assert.Equal(1, actualFound);
        }

        [Fact]
        public void Query_ChangedPositionRandom_OnlyRunWhenChanged()
        {
            ECS world = new();
            CommandBuffer cmd = new();
            Random rand = new(42);
            int expectedCount = 0;
            for (int i = 0; i < 1000; i++)
            {
                if (rand.NextDouble() > 0.5)
                {
                    cmd.SpawnEntity().With(new Position { X = 42 }).With(new Velocity());
                    expectedCount++;
                }
                else
                {
                    cmd.SpawnEntity().With(new Position { X = 420 }).With(new Velocity());
                }
            }

            cmd.Flush(world);
            var positions = world.GetSparseSet<Position>();

            var velocities = world.GetSparseSet<Velocity>();
            //First half have been changed
            for (int i = 0; i < positions.GetEntities().Count; i++)
            {
                if (positions.GetDense()[i].X == 42)
                    positions.GetLastTicks()[i] = 20;
            }

            var query = new Query<ChangedQuery>(world, 10);
            int actualFound = 0;
            foreach (var q in query)
            {
                Assert.Equal(42, q.pos.X);
                actualFound++;
            }

            Assert.Equal(expectedCount, actualFound);
        }

        [Fact]
        public void Query_ASpan_GetSpanOfComponents()
        {
            var ecs = new ECS();

            for (int i = 0; i < 1; i++)
            {
                var entity = ecs.CreateEntity();
                ecs.InsertComponent(entity, new Position { X = 5 });
            }
            var q = new Query<SpanQuery>(ecs, 0);
            Assert.Equal(1, q.Single().positions.Length);
        }

        [Fact]
        public void Query_OptionalComponent_HaveOptionalVelocity()
        {
            var world = new ECS();

            var entity = world.CreateEntity();
            world.InsertComponent(entity, new Position { X = 2 });

            entity = world.CreateEntity();
            world.InsertComponent(entity, new Position { X = 42 });
            world.InsertComponent(entity, new Velocity { Dx = 42 });

            var q = new Query<OptionalQuery>(world, 0);
            var actualCount = 0;
            foreach (var query in q)
            {
                actualCount++;
                if (query.optionalVel.IsSome)
                    Assert.Equal(42, query.optionalVel.Unwrap().Dx);
            }

            Assert.Equal(2, actualCount);
        }

        [Fact]
        public void Query_OptionalComponent_HaveOptionalMutVelocity()
        {
            var world = new ECS();

            var entity = world.CreateEntity();
            world.InsertComponent(entity, new Position { X = 2 });

            entity = world.CreateEntity();
            world.InsertComponent(entity, new Position { X = 42 });
            world.InsertComponent(entity, new Velocity { Dx = 42 });

            var q = new Query<OptionalMutQuery>(world, 0);
            var actualCount = 0;
            foreach (var query in q)
            {
                actualCount++;
                if (query.optionVel.IsSome)
                    Assert.Equal(42, query.optionVel.Unwrap().Dx);
            }

            Assert.Equal(2, actualCount);
        }

        [Fact]
        public void Query_Get_Success()
        {
            var world = new ECS();

            var entity = world.CreateEntity();
            world.InsertComponent(entity, new Position { X = 2 });

            entity = world.CreateEntity();
            world.InsertComponent(entity, new Position { X = 42 });
            world.InsertComponent(entity, new Velocity { Dx = 42 });

            var q = new Query<MovementQuery>(world, 0);
            var comps = q.Get(entity);

            Assert.True(comps.IsSome);
        }

        [Fact]
        public void Query_Get_Failure()
        {
            var world = new ECS();

            var entity = world.CreateEntity();
            world.InsertComponent(entity, new Position { X = 2 });

            var entity2 = world.CreateEntity();
            world.InsertComponent(entity2, new Position { X = 42 });
            world.InsertComponent(entity2, new Velocity { Dx = 42 });

            var q = new Query<MovementQuery>(world, 0);
            var comps = q.Get(entity);

            Assert.True(comps.IsNone);
        }
    }
}
