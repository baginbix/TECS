using System;
using System.Runtime.CompilerServices;
using TECS;
using TECS.Commands;
using TECS.Query; 
using Xunit;

namespace UnitTestsECS
{
        // 1. Define Components and Tags
        public struct Position { public float X; }
        public struct Velocity { public float Dx; }
        public struct Health { public int Hp; }
        public struct PlayerTag { }
        public struct Frozen { }

        // 2. Define the Query Struct with the required attribute!
        [Query]
        public ref struct MovementQuery
        {
            public ref Position Pos;
            public ref Velocity Vel;
        }
        [Query]
        public ref struct NothingQuery
        {
            public ref Health Hp;
        }

        // 3. Define the Systems for testing
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

            public static void QueryNothing(Query<NothingQuery> query)
            {
                foreach (var item in query)
                {
                    item.Hp.Hp += 1;
                }
            }

            public static void QuerySingle(Query<MovementQuery> query)
            {
                var item = query.Single();
                item.Pos.X += item.Vel.Dx;
            }
        }
    public class QueryTests
    {

        [Fact]
        public void System_ProcessesAllValidEntities()
        {
            var ecs = new ECS();
            var entity = ecs.CreateEntity();
            ecs.InsertComponent(entity, new Position { X = 0 });
            ecs.InsertComponent(entity, new Velocity { Dx = 5 });

            TestSystems.CountMovementSystem(new Query<MovementQuery>(ecs));

            Assert.Equal(5, ecs.QueryComponent<Position>(entity).Unwrap().X);
        }

        [Fact]
        public void System_HandleQueryingNothing()
        {
            var ecs = new ECS();
            var entity = ecs.CreateEntity();

            var exceptions = Record.Exception(() => TestSystems.QueryNothing(new Query<NothingQuery>(ecs)));

            Assert.Null(exceptions);
        }

        [Fact]
        public void System_QuerySingleWithOneComponent_Success()
        {
            var ecs = new ECS();
            var entity = ecs.CreateEntity();
            ecs.InsertComponent(entity, new Position { X = 0 });
            ecs.InsertComponent(entity, new Velocity { Dx = 5 });

            TestSystems.QuerySingle(new Query<MovementQuery>(ecs));

            Assert.Equal(5, ecs.QueryComponent<Position>(entity).Unwrap().X);
        }
        [Fact]
        public void System_QuerySingleWithMultipleComponents_ThrowsException()
        {
            var ecs = new ECS();
            var entity1 = ecs.CreateEntity();
            ecs.InsertComponent(entity1, new Position { X = 0 });
            ecs.InsertComponent(entity1, new Velocity { Dx = 5 });

            var entity2 = ecs.CreateEntity();
            ecs.InsertComponent(entity2, new Position { X = 10 });
            ecs.InsertComponent(entity2, new Velocity { Dx = 15 });

            var exceptions = Record.Exception(() => TestSystems.QuerySingle(new Query<MovementQuery>(ecs)));

            Assert.NotNull(exceptions);
        }
    }
}