using System;
using System.Collections.Generic;
using Xunit;
using TECS;
using TECS.Query;
using TECS.Resources;

namespace TECS.Tests
{
    // 1. Components & Resources
    public struct Position { public float X, Y; }
    public struct Velocity { public float X, Y; }
    public struct Health { public int Value; }
    public class TimeResource : IResource { public float DeltaTime; }

    // 2. Define Queries
    [Query]
    public ref struct MoveQuery
    {
        public ref Position Pos;
    }

    [Query]
    public ref struct DestroyTestQuery
    {
        public ref Position Pos;
        public ref Velocity Vel;
    }

    // 3. Define Static Systems
    public static class TestSystems
    {
        public static int ExecutionCount = 0;

        [System]
        public static void MoveSystem(Query<MoveQuery> query)
        {
            foreach (var item in query)
            {
                // You can mutate items directly here now!
            }
        }

        [System]
        public static void CountEntitiesSystem(Query<DestroyTestQuery> query)
        {
            foreach (var item in query)
            {
                ExecutionCount++;
            }
        }
    }

    public class ECSTests
    {
        [Fact]
        public void CreateEntity_ShouldReturnUniqueEntities()
        {
            var ecs = new ECS();
            Entity e1 = ecs.CreateEntity();
            Entity e2 = ecs.CreateEntity();
            Assert.NotEqual(e1.Id, e2.Id);
        }

        [Fact]
        public void DestroyEntity_ShouldRemoveComponentFromDenseArray()
        {
            var ecs = new ECS();
            Entity e1 = ecs.CreateEntity();
            Entity e2 = ecs.CreateEntity();

            ecs.InsertComponent<Position>(e1, new Position { X = 10, Y = 10 });
            ecs.InsertComponent<Position>(e2, new Position { X = 20, Y = 20 });

            // Act
            ecs.DestroyEntity(e1);

            // Assert
            var positions = ecs.GetComponentList<Position>();
            Assert.Single(positions);
            Assert.Equal(20, positions[0].X);
        }

        [Fact]
        public void DestroyEntity_ShouldRemoveEntityFromQueries()
        {
            var app = new App();
            TestSystems.ExecutionCount = 0;

            // Using internal app.ecs access
            Entity e1 = app.ecs.CreateEntity();
            Entity e2 = app.ecs.CreateEntity();
            Entity e3 = app.ecs.CreateEntity();

            app.ecs.InsertComponent<Position>(e1, new Position());
            app.ecs.InsertComponent<Velocity>(e1, new Velocity());

            app.ecs.InsertComponent<Position>(e2, new Position());
            app.ecs.InsertComponent<Velocity>(e2, new Velocity());

            app.ecs.InsertComponent<Position>(e3, new Position());
            app.ecs.InsertComponent<Velocity>(e3, new Velocity());

            // Act
            app.ecs.DestroyEntity(e2); // Destroy the middle one

            // Assert
            // Register and run the system once
            app.AddSystem(TestSystems.CountEntitiesSystem);
            app.Run();

            // Should only process e1 and e3
            Assert.Equal(2, TestSystems.ExecutionCount);
        }
    }
}